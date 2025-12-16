using MedicationAssist.TelegramBot.Configuration;
using MedicationAssist.TelegramBot.Handlers;
using MedicationAssist.TelegramBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace MedicationAssist.TelegramBot.Extensions;

/// <summary>
/// Расширения для регистрации сервисов Telegram бота
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBot(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Конфигурация настроек бота
        services.Configure<TelegramBotSettings>(
            configuration.GetSection(TelegramBotSettings.SectionName));

        // Регистрация TelegramBotClient
        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<TelegramBotSettings>>().Value;
            if (string.IsNullOrEmpty(settings.Token))
            {
                throw new InvalidOperationException(
                    "Telegram Bot Token is not configured. " +
                    "Set the TELEGRAM_BOT_TOKEN environment variable or TelegramBot__Token");
            }
            return new TelegramBotClient(settings.Token);
        });

        // Регистрация сервисов
        services.AddSingleton<UserSessionService>();
        services.AddSingleton<ReminderService>();
        
        // Регистрация обработчиков
        services.AddScoped<CommandHandler>();
        services.AddScoped<AuthHandler>();
        services.AddScoped<MedicationHandler>();
        services.AddScoped<IntakeHandler>();
        services.AddScoped<ReminderHandler>();
        services.AddScoped<CallbackQueryHandler>();
        
        // Регистрация основного сервиса бота
        services.AddHostedService<TelegramBotService>();
        services.AddHostedService(sp => sp.GetRequiredService<ReminderService>());

        return services;
    }
}

