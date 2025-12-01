using MedicationAssist.Domain.Repositories;
using MedicationAssist.Infrastructure.Data;
using MedicationAssist.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedicationAssist.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Регистрация DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Регистрация репозиториев
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMedicationRepository, MedicationRepository>();
        services.AddScoped<IMedicationIntakeRepository, MedicationIntakeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}

