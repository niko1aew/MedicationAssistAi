FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проектов
COPY ["MedicationAssist.API/MedicationAssist.API.csproj", "MedicationAssist.API/"]
COPY ["MedicationAssist.Application/MedicationAssist.Application.csproj", "MedicationAssist.Application/"]
COPY ["MedicationAssist.Domain/MedicationAssist.Domain.csproj", "MedicationAssist.Domain/"]
COPY ["MedicationAssist.Infrastructure/MedicationAssist.Infrastructure.csproj", "MedicationAssist.Infrastructure/"]

# Восстанавливаем зависимости
RUN dotnet restore "MedicationAssist.API/MedicationAssist.API.csproj"

# Копируем весь код
COPY . .

# Собираем проект
WORKDIR "/src/MedicationAssist.API"
RUN dotnet build "MedicationAssist.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MedicationAssist.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Создаем директорию для логов
RUN mkdir -p /app/logs

ENTRYPOINT ["dotnet", "MedicationAssist.API.dll"]

