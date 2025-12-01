# MedicationAssist - Обзор проекта

## Краткое описание

MedicationAssist - это информационная система для контроля приема лекарств, разработанная с использованием современных технологий и архитектурных принципов Domain-Driven Design (DDD) и Rich Domain Model.

## Технологический стек

### Backend
- **.NET 9.0** - последняя версия фреймворка
- **ASP.NET Core Web API** - REST API
- **Entity Framework Core 9.0** - ORM
- **PostgreSQL 17** - реляционная база данных
- **Npgsql.EntityFrameworkCore.PostgreSQL** - провайдер PostgreSQL для EF Core

### Логирование
- **Serilog** - структурированное логирование
- **Serilog.Sinks.File** - запись логов в файлы с ротацией
- **Serilog.AspNetCore** - интеграция с ASP.NET Core

### Тестирование
- **xUnit** - фреймворк для unit-тестирования
- **FluentAssertions** - выразительные утверждения в тестах
- **Moq** - библиотека для создания mock-объектов

### Контейнеризация
- **Docker** - контейнеризация приложения
- **Docker Compose** - оркестрация контейнеров

## Архитектура проекта

Проект организован по принципам **Clean Architecture** и **DDD**:

### 1. Domain Layer (MedicationAssist.Domain)
**Ядро системы** - содержит бизнес-логику и не зависит от внешних библиотек.

#### Основные компоненты:
- **Common/** - базовые классы
  - `Entity.cs` - базовый класс для всех сущностей
  - `ValueObject.cs` - базовый класс для объектов-значений
  - `DomainException.cs` - доменное исключение

- **Entities/** - доменные сущности (Rich Domain Model)
  - `User.cs` - пользователь системы с методами управления лекарствами и приемами
  - `Medication.cs` - лекарство с валидацией
  - `MedicationIntake.cs` - запись о приеме лекарства

- **Repositories/** - интерфейсы репозиториев
  - `IUserRepository.cs`
  - `IMedicationRepository.cs`
  - `IMedicationIntakeRepository.cs`
  - `IUnitOfWork.cs` - шаблон Unit of Work для управления транзакциями

**Бизнес-правила в Domain:**
- Валидация данных на уровне сущностей
- Проверка бизнес-правил (например, невозможность удалить лекарство с привязанными приемами)
- Инкапсуляция логики изменения состояния

### 2. Application Layer (MedicationAssist.Application)
**Слой приложения** - содержит бизнес-логику приложения и координирует работу.

#### Основные компоненты:
- **Common/** 
  - `Result.cs` - паттерн Result для обработки ошибок без исключений

- **DTOs/** - объекты передачи данных
  - `UserDto.cs`, `CreateUserDto.cs`, `UpdateUserDto.cs`
  - `MedicationDto.cs`, `CreateMedicationDto.cs`, `UpdateMedicationDto.cs`
  - `MedicationIntakeDto.cs`, `CreateMedicationIntakeDto.cs`, `UpdateMedicationIntakeDto.cs`
  - `MedicationIntakeFilterDto.cs` - фильтры для поиска

- **Services/** - сервисы приложения
  - `UserService.cs` - управление пользователями
  - `MedicationService.cs` - управление лекарствами
  - `MedicationIntakeService.cs` - управление приемами лекарств

- `DependencyInjection.cs` - регистрация сервисов в DI-контейнере

### 3. Infrastructure Layer (MedicationAssist.Infrastructure)
**Инфраструктурный слой** - содержит реализацию доступа к данным и внешним сервисам.

#### Основные компоненты:
- **Data/**
  - `ApplicationDbContext.cs` - контекст Entity Framework Core
  - **Configurations/** - конфигурации сущностей EF Core
    - `UserConfiguration.cs`
    - `MedicationConfiguration.cs`
    - `MedicationIntakeConfiguration.cs`

- **Repositories/** - реализации репозиториев
  - `UserRepository.cs`
  - `MedicationRepository.cs`
  - `MedicationIntakeRepository.cs`
  - `UnitOfWork.cs`

- **Migrations/** - миграции базы данных
  - `InitialCreate` - начальная миграция

- `DependencyInjection.cs` - регистрация инфраструктурных сервисов

### 4. API Layer (MedicationAssist.API)
**Слой представления** - REST API контроллеры.

#### Основные компоненты:
- **Controllers/**
  - `UsersController.cs` - управление пользователями
  - `MedicationsController.cs` - управление лекарствами (вложенный маршрут под пользователем)
  - `MedicationIntakesController.cs` - управление приемами (вложенный маршрут под пользователем)

- `Program.cs` - точка входа, настройка приложения
- `appsettings.json` - конфигурация приложения
- `Dockerfile` - конфигурация Docker-образа

### 5. Tests Layer (MedicationAssist.Tests.Unit)
**Слой тестов** - unit-тесты для Domain и Application слоев.

#### Основные компоненты:
- **Domain/** - тесты доменной логики
  - `UserTests.cs` - 22 теста для User
  - `MedicationTests.cs` - 10 тестов для Medication
  - `MedicationIntakeTests.cs` - 6 тестов для MedicationIntake

- **Application/** - тесты сервисов
  - `UserServiceTests.cs` - 6 тестов для UserService
  - `MedicationServiceTests.cs` - 6 тестов для MedicationService

**Всего: 46 unit-тестов, покрывающих основную бизнес-логику**

## Ключевые возможности

### 1. Управление пользователями
- Создание нового пользователя с валидацией email
- Обновление данных пользователя
- Удаление пользователя
- Получение списка всех пользователей
- Поиск пользователя по ID или email

### 2. Управление лекарствами
- Создание лекарства для конкретного пользователя
- Указание названия, описания и дозировки
- Редактирование информации о лекарстве
- Удаление лекарства (с проверкой на наличие связанных приемов)
- Получение списка лекарств пользователя

### 3. Учет приема лекарств
- Регистрация факта приема лекарства
- Автоматическая установка текущей даты/времени при отсутствии явного указания
- Добавление примечаний к приему
- Фильтрация истории приема по:
  - Диапазону дат (от/до)
  - Конкретному лекарству
- Редактирование записи о приеме
- Удаление записи

### 4. Валидация и бизнес-правила
- Проверка уникальности email пользователя
- Валидация формата email
- Ограничение длины полей
- Проверка существования связанных сущностей
- Защита от удаления лекарства с привязанными приемами
- Ограничение времени приема (не более чем через день)

## API Endpoints

### Users API
```
GET    /api/users                    - Список всех пользователей
GET    /api/users/{id}               - Получить пользователя по ID
GET    /api/users/by-email/{email}   - Получить пользователя по email
POST   /api/users                    - Создать пользователя
PUT    /api/users/{id}               - Обновить пользователя
DELETE /api/users/{id}               - Удалить пользователя
```

### Medications API
```
GET    /api/users/{userId}/medications           - Список лекарств пользователя
GET    /api/users/{userId}/medications/{id}      - Получить лекарство
POST   /api/users/{userId}/medications           - Создать лекарство
PUT    /api/users/{userId}/medications/{id}      - Обновить лекарство
DELETE /api/users/{userId}/medications/{id}      - Удалить лекарство
```

### Medication Intakes API
```
GET    /api/users/{userId}/intakes               - Список приемов пользователя
                                                    Query: fromDate, toDate, medicationId
GET    /api/users/{userId}/intakes/{id}          - Получить прием
POST   /api/users/{userId}/intakes               - Зарегистрировать прием
PUT    /api/users/{userId}/intakes/{id}          - Обновить прием
DELETE /api/users/{userId}/intakes/{id}          - Удалить прием
```

## База данных

### Схема базы данных

#### Таблица Users
- `Id` (GUID, PK)
- `Name` (VARCHAR(200), NOT NULL)
- `Email` (VARCHAR(200), NOT NULL, UNIQUE)
- `CreatedAt` (TIMESTAMP, NOT NULL)
- `UpdatedAt` (TIMESTAMP, NULL)

#### Таблица Medications
- `Id` (GUID, PK)
- `UserId` (GUID, FK → Users.Id, ON DELETE CASCADE)
- `Name` (VARCHAR(200), NOT NULL)
- `Description` (VARCHAR(1000), NULL)
- `Dosage` (VARCHAR(100), NULL)
- `CreatedAt` (TIMESTAMP, NOT NULL)
- `UpdatedAt` (TIMESTAMP, NULL)
- **Index:** (UserId, Name)

#### Таблица MedicationIntakes
- `Id` (GUID, PK)
- `UserId` (GUID, FK → Users.Id, ON DELETE CASCADE)
- `MedicationId` (GUID, FK → Medications.Id)
- `IntakeTime` (TIMESTAMP, NOT NULL)
- `Notes` (VARCHAR(500), NULL)
- `CreatedAt` (TIMESTAMP, NOT NULL)
- `UpdatedAt` (TIMESTAMP, NULL)
- **Index:** (UserId, IntakeTime)
- **Index:** (MedicationId)

## Логирование

### Конфигурация Serilog
- **Консоль** - вывод логов в консоль (для Development)
- **Файлы** - запись в файлы с ротацией
  - Путь: `logs/medication-assist-YYYYMMDD.log`
  - Ротация: ежедневно
  - Хранение: 30 дней
  - Формат: структурированный JSON

### Уровни логирования
- **Information** - основные операции (создание, обновление, удаление)
- **Warning** - ошибки валидации, попытки некорректных операций
- **Error** - исключения и системные ошибки
- **Fatal** - критические ошибки приложения

## Docker

### docker-compose.yml
Содержит два сервиса:
1. **postgres** - PostgreSQL 17
   - Порт: 5432
   - База: medicationassist
   - Health check для корректного запуска API

2. **api** - API приложение
   - Порт: 5000 (хост) → 8080 (контейнер)
   - Зависит от postgres
   - Auto-restart при падении
   - Монтирование папки logs для доступа к логам

### Запуск
```bash
docker-compose up --build
```

## Тестирование

### Unit Tests Coverage
- **Domain Layer**: 38 тестов
  - User: создание, валидация, управление лекарствами и приемами
  - Medication: создание, валидация, обновление
  - MedicationIntake: создание, валидация времени и примечаний

- **Application Layer**: 8 тестов
  - UserService: CRUD операции с mock репозиториев
  - MedicationService: CRUD операции с проверкой связей

### Запуск тестов
```bash
dotnet test
```

**Результат**: 46/46 тестов проходят успешно ✅

## Принципы разработки

### SOLID
- **S**ingle Responsibility - каждый класс имеет одну ответственность
- **O**pen/Closed - открыт для расширения, закрыт для модификации
- **L**iskov Substitution - использование интерфейсов и абстракций
- **I**nterface Segregation - небольшие специализированные интерфейсы
- **D**ependency Inversion - зависимость от абстракций, а не конкретных реализаций

### DDD Patterns
- **Entities** - объекты с идентичностью (User, Medication, MedicationIntake)
- **Value Objects** - возможность расширения (например, для Email, Dosage)
- **Aggregates** - User как агрегат, управляющий Medications и MedicationIntakes
- **Repositories** - абстракция доступа к данным
- **Domain Services** - бизнес-логика, не привязанная к конкретной сущности
- **Domain Exceptions** - специальные исключения для доменных ошибок

### Best Practices
- **Immutable DTOs** - использование record для DTOs
- **Result Pattern** - обработка ошибок без исключений на уровне API
- **Unit of Work** - атомарные транзакции
- **Dependency Injection** - инверсия управления
- **Async/Await** - асинхронное программирование для I/O операций

## Возможности для расширения

1. **Аутентификация и авторизация** - JWT токены, роли пользователей
2. **Напоминания** - система уведомлений о предстоящем приеме
3. **Расписание приема** - регулярные приемы (ежедневно, раз в неделю и т.д.)
4. **История изменений** - аудит всех операций
5. **API документация** - Swagger/OpenAPI
6. **Интеграция с внешними системами** - аптеки, медицинские сервисы
7. **Мобильное приложение** - iOS/Android клиенты
8. **Отчеты** - статистика приема лекарств
9. **Экспорт данных** - PDF, Excel отчеты
10. **Локализация** - поддержка множества языков

## Состояние проекта

✅ **Полностью готов к использованию**

- Все основные функции реализованы
- 46 unit-тестов покрывают бизнес-логику
- Настроено логирование
- Готова Docker-конфигурация
- Создана документация
- Применены миграции базы данных

## Лицензия

MIT License

## Авторы

Проект создан для демонстрации применения современных архитектурных практик в .NET приложениях.

