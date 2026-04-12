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
if (startupMode is not ("web" or "db-task"))
{
    throw new InvalidOperationException("APP_STARTUP_MODE must be either 'web' or 'db-task'.");
}

var legacyRunDbTasksOnceAndExit = builder.Configuration.GetValue<bool>("RUN_DB_TASKS_ONCE_AND_EXIT");
var legacyRunDbTasksInDevelopment = app.Environment.IsDevelopment() &&
                                    builder.Configuration.GetValue<bool>("RUN_DB_TASKS_IN_DEVELOPMENT");

if (!app.Environment.IsDevelopment() && (legacyRunDbTasksOnceAndExit || builder.Configuration.GetValue<bool>("RUN_DB_TASKS_IN_DEVELOPMENT")))
{
    Log.Warning("Legacy DB task flags are ignored outside Development. Use APP_STARTUP_MODE=db-task for one-off migration/seeding tasks.");
}

var runDbTasksOnceAndExit = startupMode == "db-task" || (app.Environment.IsDevelopment() && legacyRunDbTasksOnceAndExit);
var runDbTasksInDevelopment = startupMode == "db-task" || legacyRunDbTasksInDevelopment;

if (runDbTasksOnceAndExit || runDbTasksInDevelopment)
{
    if (startupMode == "db-task")
    {
        Log.Information("Running startup database tasks because APP_STARTUP_MODE=db-task.");
    }
    else if (runDbTasksInDevelopment)
    {
        Log.Information("Running startup database tasks in development mode because RUN_DB_TASKS_IN_DEVELOPMENT=true.");
    }

    if (runDbTasksOnceAndExit)
    {
        Log.Information("Running one-off database tasks because RUN_DB_TASKS_ONCE_AND_EXIT=true.");
    }

    var adminUsername = builder.Configuration["ADMIN_USERNAME"];
    var adminEmail = builder.Configuration["ADMIN_EMAIL"];
    var adminPassword = builder.Configuration["ADMIN_PASSWORD"];

    if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException("ADMIN_USERNAME, ADMIN_EMAIL, and ADMIN_PASSWORD are required when running database tasks.");
    }

    var requiredAdminUsername = adminUsername!;
    var requiredAdminEmail = adminEmail!;
    var requiredAdminPassword = adminPassword!;

    using var scope = app.Services.CreateScope();
    var hasher = new PasswordHasher<User>();
    var db = scope.ServiceProvider.GetRequiredService<GearUpDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    db.Database.Migrate();
    await AdminSeeder.SeedAdminAsync(db, hasher, requiredAdminUsername, requiredAdminEmail, requiredAdminPassword);
    await seeder.SeedAsync();

    if (runDbTasksOnceAndExit)
    {
        Log.Information("Completed one-off database tasks. Exiting without starting web host.");
        Log.CloseAndFlush();
        return;
    }
}
else
{
    Log.Information("Skipping startup database migration and seeding tasks. Use RUN_DB_TASKS_IN_DEVELOPMENT=true (development only) or RUN_DB_TASKS_ONCE_AND_EXIT=true (one-off).");
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
