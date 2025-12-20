using MedicationAssist.Application;
using MedicationAssist.Infrastructure;
using MedicationAssist.TelegramBot.Extensions;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting MedicationAssist Telegram bot...");

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Add environment variables with custom mapping
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["TelegramBot:Token"] = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN"),
        ["TelegramBot:WebhookUrl"] = Environment.GetEnvironmentVariable("TELEGRAM_WEBHOOK_URL"),
        ["TelegramBot:BotUsername"] = Environment.GetEnvironmentVariable("TELEGRAM_BOT_USERNAME"),
        ["TelegramBot:WebsiteUrl"] = Environment.GetEnvironmentVariable("WEBSITE_URL"),
        ["ConnectionStrings:DefaultConnection"] = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING"),
        ["JwtSettings:Secret"] = Environment.GetEnvironmentVariable("JWT_SECRET_KEY"),
    }.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value));

    // Serilog configuration
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/telegram-bot-.log", rollingInterval: RollingInterval.Day));

    // Application layers registration
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Telegram bot registration
    builder.Services.AddTelegramBot(builder.Configuration);

    var host = builder.Build();

    Log.Information("Telegram bot initialized successfully");

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Critical error while starting Telegram bot");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

