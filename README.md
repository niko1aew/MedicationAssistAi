# MedicationAssist - Система контроля приема лекарств

Информационная система для контроля приема лекарств с напоминаниями о предстоящей необходимости приема.

## Описание

MedicationAssist - это REST API приложение, разработанное с использованием современных технологий и архитектурных практик:

- **.NET 9.0** - последняя версия платформы .NET
- **PostgreSQL** - надежная реляционная база данных
- **DDD (Domain-Driven Design)** - архитектурный подход
- **Rich Domain Model** - богатая доменная модель с бизнес-логикой
- **Entity Framework Core** - ORM для работы с базой данных
- **Serilog** - структурированное логирование
- **Docker** - контейнеризация приложения
- **xUnit, FluentAssertions, Moq** - тестирование

## Функциональность

### Управление пользователями
- Создание, обновление, удаление пользователей
- Получение информации о пользователе

### Управление лекарствами
- Создание списка лекарств для каждого пользователя
- Добавление информации о лекарстве (название, описание, дозировка)
- Редактирование и удаление лекарств

### Учет приема лекарств
- Регистрация факта приема лекарства
- Автоматическое проставление текущей даты/времени при отсутствии указания
- Просмотр истории приема с возможностью фильтрации:
  - По датам (от/до)
  - По конкретному лекарству
- Редактирование и удаление записей о приеме

## Структура проекта

```
MedicationAssist/
├── MedicationAssist.Domain/          # Доменный слой (сущности, интерфейсы)
│   ├── Common/                       # Базовые классы и исключения
│   ├── Entities/                     # Доменные сущности
│   └── Repositories/                 # Интерфейсы репозиториев
├── MedicationAssist.Application/     # Слой приложения (use cases, DTOs)
│   ├── Common/                       # Общие классы (Result)
│   ├── DTOs/                         # Data Transfer Objects
│   └── Services/                     # Сервисы приложения
├── MedicationAssist.Infrastructure/  # Инфраструктурный слой
│   ├── Data/                         # DbContext и конфигурации EF
│   └── Repositories/                 # Реализации репозиториев
├── MedicationAssist.API/             # Web API
│   └── Controllers/                  # REST контроллеры
└── MedicationAssist.Tests.Unit/      # Unit тесты
    ├── Domain/                       # Тесты доменной логики
    └── Application/                  # Тесты сервисов
```

## Требования

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (для запуска в контейнерах)
- PostgreSQL 17+ (если запуск без Docker)

## Быстрый старт с Docker

1. Клонируйте репозиторий:
```bash
git clone <repository-url>
cd MedicationAssist
```

2. Запустите приложение с помощью Docker Compose:
```bash
docker-compose up --build
```

3. API будет доступен по адресу: `http://localhost:5000`

4. PostgreSQL будет доступен на порту `5432`

## Запуск без Docker

1. Установите и настройте PostgreSQL

2. Обновите строку подключения в `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=medicationassist;Username=postgres;Password=your_password"
  }
}
```

3. Примените миграции базы данных:
```bash
cd MedicationAssist.API
dotnet ef database update --project ../MedicationAssist.Infrastructure
```

4. Запустите приложение:
```bash
dotnet run --project MedicationAssist.API
```

## API Endpoints

### Users (Пользователи)

- `GET /api/users` - Получить всех пользователей
- `GET /api/users/{id}` - Получить пользователя по ID
- `GET /api/users/by-email/{email}` - Получить пользователя по email
- `POST /api/users` - Создать пользователя
- `PUT /api/users/{id}` - Обновить пользователя
- `DELETE /api/users/{id}` - Удалить пользователя

### Medications (Лекарства)

- `GET /api/users/{userId}/medications` - Получить все лекарства пользователя
- `GET /api/users/{userId}/medications/{id}` - Получить лекарство по ID
- `POST /api/users/{userId}/medications` - Создать лекарство
- `PUT /api/users/{userId}/medications/{id}` - Обновить лекарство
- `DELETE /api/users/{userId}/medications/{id}` - Удалить лекарство

### Medication Intakes (Приемы лекарств)

- `GET /api/users/{userId}/intakes` - Получить все приемы пользователя
  - Query параметры: `fromDate`, `toDate`, `medicationId`
- `GET /api/users/{userId}/intakes/{id}` - Получить прием по ID
- `POST /api/users/{userId}/intakes` - Зарегистрировать прием
- `PUT /api/users/{userId}/intakes/{id}` - Обновить прием
- `DELETE /api/users/{userId}/intakes/{id}` - Удалить прием

## Примеры запросов

### Создание пользователя
```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Иван Иванов",
    "email": "ivan@example.com"
  }'
```

### Создание лекарства
```bash
curl -X POST http://localhost:5000/api/users/{userId}/medications \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Аспирин",
    "description": "Обезболивающее средство",
    "dosage": "500mg"
  }'
```

### Регистрация приема лекарства
```bash
curl -X POST http://localhost:5000/api/users/{userId}/intakes \
  -H "Content-Type: application/json" \
  -d '{
    "medicationId": "{medicationId}",
    "intakeTime": "2024-12-01T10:00:00Z",
    "notes": "Принято после завтрака"
  }'
```

## Тестирование

Запуск unit тестов:
```bash
dotnet test
```

Запуск с покрытием кода:
```bash
dotnet test /p:CollectCoverage=true
```

## Логирование

Логи записываются в:
- Консоль (при запуске)
- Файл `logs/medication-assist-YYYYMMDD.log`

Логи автоматически ротируются ежедневно, хранятся последние 30 дней.

## Архитектура

Проект следует принципам:
- **Clean Architecture** - разделение на слои с четкими границами
- **DDD** - доменная модель в центре архитектуры
- **Rich Domain Model** - бизнес-логика в доменных сущностях
- **SOLID** - принципы объектно-ориентированного проектирования
- **Repository Pattern** - абстракция доступа к данным
- **Unit of Work** - управление транзакциями

## Разработка

### Создание миграций
```bash
cd MedicationAssist.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../MedicationAssist.API
```

### Применение миграций
```bash
dotnet ef database update --startup-project ../MedicationAssist.API
```

## Лицензия

MIT License

## Контакты

Для вопросов и предложений создайте issue в репозитории проекта.

