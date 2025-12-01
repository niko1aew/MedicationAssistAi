using MedicationAssist.Application;
using MedicationAssist.Infrastructure;
using MedicationAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
    builder.Services.AddOpenApi();

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
        app.MapOpenApi();
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseCors("AllowAll");

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
