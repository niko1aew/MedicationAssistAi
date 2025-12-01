using MedicationAssist.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MedicationAssist.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Регистрация сервисов приложения
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMedicationService, MedicationService>();
        services.AddScoped<IMedicationIntakeService, MedicationIntakeService>();

        return services;
    }
}

