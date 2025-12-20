using System.Text;
using System.Threading.RateLimiting;
using MedicationAssist.Application;
using MedicationAssist.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// Check if running migrations only
var isMigrationMode = Environment.GetEnvironmentVariable("MIGRATION_MODE") == "true";

if (isMigrationMode)
{
    // Minimal setup for migrations
    var migrationBuilder = WebApplication.CreateBuilder(args);
    migrationBuilder.Services.AddInfrastructure(migrationBuilder.Configuration);
    var migrationApp = migrationBuilder.Build();

    Console.WriteLine("Migration mode: Application built successfully for EF Core tools");
    // Don't run the app, just exit successfully
    // EF Core tools will use this application to apply migrations
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Add environment variables with custom mapping
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["TelegramBot:BotUsername"] = Environment.GetEnvironmentVariable("TELEGRAM_BOT_USERNAME"),
    ["TelegramBot:WebsiteUrl"] = Environment.GetEnvironmentVariable("TELEGRAM_WEBSITE_URL"),
}.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value));

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/medication-assist-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting MedicationAssist application");

    // Add services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Configure Swagger/OpenAPI with JWT auth
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MedicationAssist API",
            Version = "v1",
            Description = "API for managing medications and intake schedule"
        });

        // JWT Bearer auth configuration
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT token.\n\nExample: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // JWT Authentication configuration
    var jwtSecret = builder.Configuration["JwtSettings:Secret"];
    if (string.IsNullOrEmpty(jwtSecret))
    {
        throw new InvalidOperationException(
            "JWT Secret not found in configuration. " +
            "Set environment variable JwtSettings__Secret or configure appsettings.json");
    }
    var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
    var jwtAudience = builder.Configuration["JwtSettings:Audience"];

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero // Убираем задержку валидации токена
        };
    });

    builder.Services.AddAuthorization();

    // Rate Limiting configuration
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                }));

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // Application layers registration
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // CORS configuration
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // HTTP pipeline configuration
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "MedicationAssist API v1.0");
            options.RoutePrefix = "swagger"; // URL: /swagger
            options.DocumentTitle = "MedicationAssist API Documentation";
        });
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseRateLimiter();

    app.UseCors("AllowAll");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();

    Log.Information("MedicationAssist application started successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "MedicationAssist application terminated with a fatal error");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
