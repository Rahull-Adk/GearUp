using System.Text.Json;
using DotNetEnv;
using GearUp.Domain.Entities.Users;
using GearUp.Infrastructure.Persistence;
using GearUp.Infrastructure.Seed;
using GearUp.Infrastructure.SignalR;
using GearUp.Presentation.Extensions;
using GearUp.Presentation.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;


try
{
    var root = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
    Env.Load(Path.Combine(root!, ".env"));
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

string adminUsername = builder.Configuration["ADMIN_USERNAME"]!;
string adminEmail = builder.Configuration["ADMIN_EMAIL"]!;
string adminPassword = builder.Configuration["ADMIN_PASSWORD"]!;

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var hasher = new PasswordHasher<User>();
    var db = scope.ServiceProvider.GetRequiredService<GearUpDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    db.Database.Migrate();
    await AdminSeeder.SeedAdminAsync(db, hasher, adminUsername, adminEmail, adminPassword);
    await seeder.SeedAsync();
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
else
{
    app.UseRateLimiter();
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
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.UseSerilogRequestLogging();

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();
app.MapHub<PostHub>("/hubs/post");
app.MapHub<NotificationHub>("/hubs/notification");

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


