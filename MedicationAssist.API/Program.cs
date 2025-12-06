using System.Text;
using System.Threading.RateLimiting;
using MedicationAssist.Application;
using MedicationAssist.Infrastructure;
using MedicationAssist.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
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
    Log.Information("Запуск приложения MedicationAssist");

    // Добавление сервисов
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    
    // Настройка Swagger/OpenAPI с JWT авторизацией
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MedicationAssist API",
            Version = "v1",
            Description = "API для управления лекарствами и расписанием приёма"
        });

        // Настройка JWT Bearer авторизации
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Введите JWT токен.\n\nПример: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
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

    // Настройка JWT Authentication
    var jwtSecret = builder.Configuration["JwtSettings:Secret"] 
        ?? throw new InvalidOperationException("JWT Secret не найден в конфигурации");
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

    // Настройка Rate Limiting
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

    // Регистрация слоев приложения
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Настройка CORS
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

    // Автоматическое применение миграций при запуске (опционально)
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            dbContext.Database.Migrate();
            Log.Information("Миграции базы данных применены успешно");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Не удалось применить миграции. База данных может быть недоступна");
        }
    }

    // Настройка HTTP pipeline
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

    Log.Information("Приложение успешно запущено");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение завершилось с ошибкой");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
