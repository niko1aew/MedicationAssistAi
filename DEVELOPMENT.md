# Руководство по разработке MedicationAssist

## Быстрый старт для разработчика

### Предварительные требования

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- IDE: JetBrains Rider или Visual Studio 2022
- Git

### Первый запуск

1. **Клонируйте репозиторий:**
```powershell
git clone <repository-url>
cd AiMedicationAssist
```

2. **Запустите базу данных в Docker:**
```powershell
docker-compose -f docker-compose.db.yml up -d
```

3. **Дождитесь готовности БД (проверка healthcheck):**
```powershell
docker-compose -f docker-compose.db.yml ps
# Статус должен быть "healthy"
```

4. **Примените миграции:**
```powershell
cd MedicationAssist.API
dotnet ef database update --project ../MedicationAssist.Infrastructure
```

5. **Запустите API:**
   - В Rider: нажмите F5 или кнопку Run
   - Или из командной строки:
     ```powershell
     dotnet run --project MedicationAssist.API
     ```

6. **Проверьте работу API:**
   - **Swagger UI (Рекомендуется):** `http://localhost:5000/swagger`
   - Откройте браузер: `http://localhost:5000/api/users`
   - Или используйте файл `MedicationAssist.API/MedicationAssist.API.http`

### Работа с Swagger UI

Swagger UI - это интерактивная документация API, доступная в режиме Development:

**URL:** `http://localhost:5000/swagger`

**Возможности:**
- 📖 Просмотр всех доступных эндпоинтов
- 🧪 Тестирование API запросов прямо из браузера
- 📋 Просмотр схем данных (Request/Response DTOs)
- 📝 Примеры JSON для всех операций
- ✅ Коды ответов и описание ошибок

**Как использовать:**
1. Откройте `http://localhost:5000/swagger` в браузере
2. Разверните нужный эндпоинт (например, `GET /api/users`)
3. Нажмите кнопку "Try it out"
4. Заполните параметры (если требуется)
5. Нажмите "Execute"
6. Просмотрите Response

**Примечание:** Swagger UI доступен только в Development окружении.

## Ежедневный workflow

### Запуск окружения

```powershell
# Запуск БД (если еще не запущена)
docker-compose -f docker-compose.db.yml up -d

# Запуск API из IDE или:
dotnet run --project MedicationAssist.API
```

### Остановка окружения

```powershell
# Остановка БД (данные сохранятся)
docker-compose -f docker-compose.db.yml down

# Остановка БД с удалением данных
docker-compose -f docker-compose.db.yml down -v
```

## Работа с базой данных

### Подключение к БД

**Параметры подключения:**
- **Host:** localhost
- **Port:** 5432
- **Database:** medicationassist
- **Username:** postgres
- **Password:** postgres

**Строка подключения:**
```
Host=localhost;Port=5432;Database=medicationassist;Username=postgres;Password=postgres
```

### Работа с миграциями

**Создание новой миграции:**
```powershell
cd MedicationAssist.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../MedicationAssist.API
```

**Применение миграций:**
```powershell
cd MedicationAssist.API
dotnet ef database update --project ../MedicationAssist.Infrastructure
```

**Откат миграции:**
```powershell
cd MedicationAssist.API
dotnet ef database update <PreviousMigrationName> --project ../MedicationAssist.Infrastructure
```

**Удаление последней миграции (если не применена):**
```powershell
cd MedicationAssist.Infrastructure
dotnet ef migrations remove --startup-project ../MedicationAssist.API
```

**Просмотр списка миграций:**
```powershell
cd MedicationAssist.API
dotnet ef migrations list --project ../MedicationAssist.Infrastructure
```

**Генерация SQL скрипта миграции:**
```powershell
cd MedicationAssist.API
dotnet ef migrations script --project ../MedicationAssist.Infrastructure --output migration.sql
```

### Подключение к БД через psql

```powershell
# Интерактивный режим
docker exec -it medicationassist-postgres-dev psql -U postgres -d medicationassist

# Выполнение команды
docker exec -it medicationassist-postgres-dev psql -U postgres -d medicationassist -c "SELECT * FROM \"Users\";"
```

**Полезные SQL команды:**
```sql
-- Список таблиц
\dt

-- Описание таблицы
\d "Users"

-- Выход
\q
```

### Бэкап и восстановление

**Создание бэкапа:**
```powershell
docker exec -it medicationassist-postgres-dev pg_dump -U postgres medicationassist > backup_$(Get-Date -Format "yyyyMMdd_HHmmss").sql
```

**Восстановление из бэкапа:**
```powershell
Get-Content backup.sql | docker exec -i medicationassist-postgres-dev psql -U postgres medicationassist
```

**Полное удаление и пересоздание БД:**
```powershell
# Остановка и удаление данных
docker-compose -f docker-compose.db.yml down -v

# Запуск заново
docker-compose -f docker-compose.db.yml up -d

# Применение миграций
cd MedicationAssist.API
dotnet ef database update --project ../MedicationAssist.Infrastructure
```

## Тестирование

### Запуск тестов

```powershell
# Все тесты
dotnet test

# Тесты с детальным выводом
dotnet test --logger "console;verbosity=detailed"

# Тесты с покрытием кода
dotnet test /p:CollectCoverage=true /p:CoverageReportsFormat=opencover

# Конкретный тестовый проект
dotnet test MedicationAssist.Tests.Unit
```

### Запуск конкретного теста

```powershell
dotnet test --filter "FullyQualifiedName~MedicationAssist.Tests.Unit.Domain.UserTests"
```

## Работа с API

### HTTP файлы

Используйте файл `MedicationAssist.API/MedicationAssist.API.http` для тестирования API:
- В Rider: откройте файл и нажмите на зеленые стрелки рядом с запросами
- В VS Code: установите расширение REST Client

### Примеры запросов

**Создание пользователя:**
```powershell
$body = @{
    name = "Иван Иванов"
    email = "ivan@example.com"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/users" -Method Post -Body $body -ContentType "application/json"
```

**Получение всех пользователей:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/users" -Method Get
```

## Отладка

### Отладка в Rider

1. Установите breakpoint в коде (клик слева от номера строки)
2. Запустите в режиме Debug (Shift+F9)
3. Выполните запрос к API
4. Debugger остановится на breakpoint

### Отладка в Visual Studio

1. Установите breakpoint (F9)
2. Запустите с отладкой (F5)
3. Выполните запрос к API

### Просмотр логов

**Консоль приложения:**
Логи выводятся в консоль при запуске API

**Файлы логов:**
```powershell
# Просмотр последних логов
Get-Content MedicationAssist.API/logs/medication-assist-$(Get-Date -Format "yyyyMMdd").log -Tail 50 -Wait

# Открыть папку с логами
explorer MedicationAssist.API/logs
```

**Логи базы данных:**
```powershell
docker logs medicationassist-postgres-dev -f
```

## Структура проекта и соглашения

### Слои архитектуры

```
Domain ← Application ← Infrastructure
                ↓
              API
```

**MedicationAssist.Domain:**
- Доменные сущности (Entities)
- Интерфейсы репозиториев (Repositories)
- Доменные исключения (Common)
- Базовые классы (Entity, ValueObject)

**MedicationAssist.Application:**
- Сервисы приложения (Services)
- DTO (Data Transfer Objects)
- Интерфейсы сервисов
- Result объекты

**MedicationAssist.Infrastructure:**
- Реализации репозиториев
- DbContext и конфигурации EF Core
- Миграции

**MedicationAssist.API:**
- REST контроллеры
- Конфигурация приложения
- Middleware

### Соглашения по коду

**Именование:**
- Классы: PascalCase
- Методы: PascalCase
- Свойства: PascalCase
- Локальные переменные: camelCase
- Приватные поля: _camelCase

**Создание сущности:**
```csharp
// Используйте статический метод Create
var userResult = User.Create(name, email);
if (userResult.IsFailure)
{
    return BadRequest(userResult.Error);
}
var user = userResult.Value;
```

**Возврат результатов:**
```csharp
// Используйте Result<T>
public Result<User> CreateUser(string name, string email)
{
    // validation...
    return Result<User>.Success(user);
    // или
    return Result<User>.Failure("Error message");
}
```

## Полезные команды

### .NET CLI

```powershell
# Восстановление зависимостей
dotnet restore

# Сборка проекта
dotnet build

# Сборка в Release
dotnet build -c Release

# Очистка артефактов сборки
dotnet clean

# Форматирование кода
dotnet format

# Список установленных пакетов
dotnet list package
```

### Docker

```powershell
# Статус контейнеров
docker ps

# Все контейнеры (включая остановленные)
docker ps -a

# Логи контейнера
docker logs medicationassist-postgres-dev

# Статистика использования ресурсов
docker stats medicationassist-postgres-dev

# Удаление всех остановленных контейнеров
docker container prune

# Удаление неиспользуемых volumes
docker volume prune
```

## Troubleshooting

### Порт 5432 уже занят

```powershell
# Проверить, какой процесс использует порт
netstat -ano | findstr :5432

# Остановить другой PostgreSQL
Stop-Service postgresql-x64-17

# Или изменить порт в docker-compose.db.yml
ports:
  - "5433:5432"  # Внешний порт 5433
```

### Ошибка подключения к БД

```powershell
# Проверить статус контейнера
docker-compose -f docker-compose.db.yml ps

# Проверить логи
docker-compose -f docker-compose.db.yml logs postgres

# Перезапустить контейнер
docker-compose -f docker-compose.db.yml restart
```

### Ошибки миграций

```powershell
# Удалить БД и пересоздать
docker-compose -f docker-compose.db.yml down -v
docker-compose -f docker-compose.db.yml up -d

# Подождать готовности
Start-Sleep -Seconds 5

# Применить миграции заново
cd MedicationAssist.API
dotnet ef database update --project ../MedicationAssist.Infrastructure
```

### API не запускается

```powershell
# Проверить порты
netstat -ano | findstr :5000

# Убедиться что БД запущена
docker-compose -f docker-compose.db.yml ps

# Проверить строку подключения в appsettings.json
```

## Полезные ресурсы

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Docker Documentation](https://docs.docker.com/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)

## Контакты и поддержка

Для вопросов и предложений создайте issue в репозитории проекта.

