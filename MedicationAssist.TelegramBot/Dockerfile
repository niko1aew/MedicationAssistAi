FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проектов
COPY ["MedicationAssist.TelegramBot/MedicationAssist.TelegramBot.csproj", "MedicationAssist.TelegramBot/"]
COPY ["MedicationAssist.Application/MedicationAssist.Application.csproj", "MedicationAssist.Application/"]
COPY ["MedicationAssist.Domain/MedicationAssist.Domain.csproj", "MedicationAssist.Domain/"]
COPY ["MedicationAssist.Infrastructure/MedicationAssist.Infrastructure.csproj", "MedicationAssist.Infrastructure/"]

# Восстанавливаем зависимости
RUN dotnet restore "MedicationAssist.TelegramBot/MedicationAssist.TelegramBot.csproj"

# Копируем исходный код
COPY . .

# Собираем проект
WORKDIR "/src/MedicationAssist.TelegramBot"
RUN dotnet build "MedicationAssist.TelegramBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MedicationAssist.TelegramBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Создаём директорию для логов
RUN mkdir -p /app/logs

ENTRYPOINT ["dotnet", "MedicationAssist.TelegramBot.dll"]

