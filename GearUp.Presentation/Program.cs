using System.Text.Json;
using DotNetEnv;
using GearUp.Domain.Entities.Users;
using GearUp.Infrastructure.Persistence;
using GearUp.Infrastructure.Seed;
using GearUp.Infrastructure.SignalR;
using GearUp.Presentation.Extensions;
using GearUp.Presentation.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Serilog;


try
{
    var currentDirectory = Directory.GetCurrentDirectory();
    var candidatePaths = new[]
    {
        Path.Combine(currentDirectory, ".env"),
        Path.Combine(Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory, ".env")
    };

    var envFilePath = candidatePaths.FirstOrDefault(File.Exists);
    if (!string.IsNullOrWhiteSpace(envFilePath))
    {
        Env.Load(envFilePath);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to load .env file: {ex.Message}");
}


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});


builder.Services.AddOpenApi();
builder.Services.AddServices(builder.Configuration);

var app = builder.Build();

var startupMode = (builder.Configuration["APP_STARTUP_MODE"] ?? "web").Trim().ToLowerInvariant();
if (startupMode is not ("web" or "db-migrate" or "db-seed" or "db-task"))
{
    throw new InvalidOperationException("APP_STARTUP_MODE must be one of: 'web', 'db-migrate', 'db-seed', or 'db-task'.");
}

var isLegacyDbTaskMode = startupMode == "db-task";
var shouldRunDbTasksByMode = startupMode is "db-migrate" or "db-seed" or "db-task";
var shouldRunMigrationsByMode = startupMode is "db-migrate" or "db-seed" or "db-task";
var shouldRunSeedingByMode = startupMode is "db-seed" or "db-task";

var legacyRunDbTasksOnceAndExit = builder.Configuration.GetValue<bool>("RUN_DB_TASKS_ONCE_AND_EXIT");
var legacyRunDbTasksInDevelopment = app.Environment.IsDevelopment() &&
                                    builder.Configuration.GetValue<bool>("RUN_DB_TASKS_IN_DEVELOPMENT");

if (!app.Environment.IsDevelopment() && (legacyRunDbTasksOnceAndExit || builder.Configuration.GetValue<bool>("RUN_DB_TASKS_IN_DEVELOPMENT")))
{
    Log.Warning("Legacy DB task flags are ignored outside Development. Use APP_STARTUP_MODE=db-migrate for migrations only, or APP_STARTUP_MODE=db-seed for migration+seeding tasks.");
}

var runDbTasksInDevelopment = app.Environment.IsDevelopment() && legacyRunDbTasksInDevelopment;
var runMigrations = shouldRunMigrationsByMode || runDbTasksInDevelopment;
var runSeedData = shouldRunSeedingByMode || runDbTasksInDevelopment;
var runDbTasks = runMigrations || runSeedData;
var runDbTasksOnceAndExit = shouldRunDbTasksByMode || (runDbTasksInDevelopment && legacyRunDbTasksOnceAndExit);

var configuredSeedScope = builder.Configuration["DB_SEED_SCOPE"]?.Trim().ToLowerInvariant();
var seedScope = string.IsNullOrWhiteSpace(configuredSeedScope)
    ? (app.Environment.IsProduction() ? "admin" : "full")
    : configuredSeedScope;

if (seedScope is not ("admin" or "full"))
{
    throw new InvalidOperationException("DB_SEED_SCOPE must be either 'admin' or 'full'.");
}

if (runDbTasks)
{
    var dbTaskStage = "initialization";
    try
    {
        Log.Information(
            "Startup DB task config: mode={StartupMode}, runMigrations={RunMigrations}, runSeedData={RunSeedData}, runOnceAndExit={RunOnceAndExit}, seedScope={SeedScope}",
            startupMode,
            runMigrations,
            runSeedData,
            runDbTasksOnceAndExit,
            seedScope);

        if (shouldRunDbTasksByMode)
        {
            if (startupMode == "db-migrate")
            {
                Log.Information("Running startup database migration because APP_STARTUP_MODE=db-migrate.");
            }
            else if (startupMode == "db-seed")
            {
                Log.Information("Running startup database migration and seed because APP_STARTUP_MODE=db-seed.");
            }
            else if (isLegacyDbTaskMode)
            {
                Log.Information("Running startup database migration and seed because APP_STARTUP_MODE=db-task (legacy alias).");
            }
        }
        else if (runDbTasksInDevelopment)
        {
            Log.Information("Running startup database tasks in development mode because RUN_DB_TASKS_IN_DEVELOPMENT=true.");
        }

        if (runDbTasksOnceAndExit && !shouldRunDbTasksByMode)
        {
            Log.Information("Running one-off database tasks because RUN_DB_TASKS_ONCE_AND_EXIT=true.");
        }

        dbTaskStage = "create-scope";
        Log.Information("DB task stage: {DbTaskStage}", dbTaskStage);
        using var scope = app.Services.CreateScope();

        dbTaskStage = "resolve-db-context";
        Log.Information("DB task stage: {DbTaskStage}", dbTaskStage);
        var db = scope.ServiceProvider.GetRequiredService<GearUpDbContext>();
        var hasher = new PasswordHasher<User>();
        DbSeeder? seeder = null;

        if (runMigrations)
        {
            dbTaskStage = "migration-connectivity-check";
            Log.Information("DB task stage: {DbTaskStage}", dbTaskStage);
            var canConnect = await db.Database.CanConnectAsync();
            if (!canConnect)
            {
                throw new InvalidOperationException("Database connectivity check failed before migrations. Verify ConnectionStrings__DefaultConnection and database availability.");
            }
            Log.Information("DB task stage completed: {DbTaskStage}", dbTaskStage);

            dbTaskStage = "migration-discover-pending";
            Log.Information("DB task stage: {DbTaskStage}", dbTaskStage);
            var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToArray();
            Log.Information(
                "Pending migrations discovered: count={PendingCount}, migrations={PendingMigrations}",
                pendingMigrations.Length,
                pendingMigrations.Length == 0 ? "<none>" : string.Join(",", pendingMigrations));
            Log.Information("DB task stage completed: {DbTaskStage}", dbTaskStage);

            if (pendingMigrations.Length > 0)
            {
                var migrator = db.Database.GetService<IMigrator>();
                for (var i = 0; i < pendingMigrations.Length; i++)
                {
                    var targetMigration = pendingMigrations[i];
                    dbTaskStage = $"migration-apply:{targetMigration}";
                    Log.Information(
                        "DB task stage: {DbTaskStage} ({CurrentIndex}/{TotalCount})",
                        dbTaskStage,
                        i + 1,
                        pendingMigrations.Length);

                    await migrator.MigrateAsync(targetMigration);

                    Log.Information(
                        "DB task stage completed: {DbTaskStage} ({CurrentIndex}/{TotalCount})",
                        dbTaskStage,
                        i + 1,
                        pendingMigrations.Length);
                }
            }
            else
            {
                Log.Information("No pending migrations detected. Database is up to date.");
            }
        }

        if (runSeedData)
        {
            dbTaskStage = "validate-seed-admin-settings";
            Log.Information("DB task stage: {DbTaskStage}", dbTaskStage);

            var adminUsername = builder.Configuration["ADMIN_USERNAME"];
            var adminEmail = builder.Configuration["ADMIN_EMAIL"];
            var adminPassword = builder.Configuration["ADMIN_PASSWORD"];

            if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException("ADMIN_USERNAME, ADMIN_EMAIL, and ADMIN_PASSWORD are required when running seed tasks.");
            }

            dbTaskStage = "seed-admin";
            Log.Information("DB task stage: {DbTaskStage}", dbTaskStage);
            await AdminSeeder.SeedAdminAsync(db, hasher, adminUsername, adminEmail, adminPassword);

            if (seedScope == "full")
            {
                dbTaskStage = "resolve-full-seeder";
                Log.Information("DB task stage: {DbTaskStage}", dbTaskStage);
                seeder ??= scope.ServiceProvider.GetRequiredService<DbSeeder>();

                dbTaskStage = "seed-full-dataset";
                Log.Information("DB task stage: {DbTaskStage}", dbTaskStage);
                await seeder.SeedAsync();
                Log.Information("DB task stage completed: {DbTaskStage}", dbTaskStage);
            }
            else
            {
                Log.Information("Skipping non-admin seed data because DB_SEED_SCOPE=admin.");
            }
        }

        if (runDbTasksOnceAndExit)
        {
            Log.Information("Completed one-off database tasks. Exiting without starting web host.");
            Log.CloseAndFlush();
            return;
        }
    }
    catch (Exception ex)
    {
        Log.Fatal(
            ex,
            "Startup DB tasks failed at stage '{DbTaskStage}'. mode={StartupMode}, runMigrations={RunMigrations}, runSeedData={RunSeedData}, seedScope={SeedScope}. Review DB connectivity, migration compatibility, and required startup environment variables.",
            dbTaskStage,
            startupMode,
            runMigrations,
            runSeedData,
            seedScope);
        Log.CloseAndFlush();
        throw new InvalidOperationException(
            $"Startup database task failed at stage '{dbTaskStage}' (APP_STARTUP_MODE={startupMode}). See logs for details.",
            ex);
    }
}
else
{
    Log.Information("Skipping startup database migration and seeding tasks. Use APP_STARTUP_MODE=db-migrate for migration-only, APP_STARTUP_MODE=db-seed for migration+seeding, or RUN_DB_TASKS_IN_DEVELOPMENT=true (development only).");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            results = report.Entries.Select(e => new
            {
                name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.UseSerilogRequestLogging();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Trust proxy headers (required on hosted platforms like Render).
    KnownNetworks = { },
    KnownProxies = { }
});

app.UseRateLimiter();

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("Fixed");
app.MapHub<PostHub>("/hubs/post");
app.MapHub<NotificationHub>("/hubs/notification");
app.MapHub<ChatHub>("/hubs/chat");

try
{
    Log.Information("Starting up the application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
