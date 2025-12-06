# Техническая спецификация проекта MedicationAssist

## 1. Общая информация о проекте

### 1.1 Название проекта
**MedicationAssist** - Информационная система контроля приема лекарственных препаратов

### 1.2 Версия документа
**Версия:** 1.3  
**Дата:** 06 декабря 2025  
**Статус:** Утверждено

### 1.3 Цель проекта
Разработка информационной системы для контроля приема лекарств с функционалом напоминаний о предстоящей необходимости приема препаратов. Система предназначена для индивидуального использования пациентами с возможностью ведения персонального графика приема медикаментов.

### 1.4 Целевая аудитория
- Пациенты, принимающие лекарственные препараты регулярно
- Люди с хроническими заболеваниями
- Пожилые люди, нуждающиеся в контроле приема лекарств
- Пользователи, принимающие несколько препаратов одновременно

### 1.5 Ключевые возможности системы
1. **Безопасная аутентификация** с использованием JWT токенов
2. **Управление персональным списком** лекарственных препаратов
3. **Регистрация факта приема** лекарств с автоматической фиксацией времени
4. **Просмотр истории приемов** с возможностью фильтрации
5. **Валидация данных** на всех уровнях системы
6. **Многопользовательский режим** работы с защитой данных
7. **Защита от перегрузки** через Rate Limiting

---

## 2. Функциональные требования

### 2.1 Аутентификация и авторизация

#### FR-AUTH-001: Регистрация пользователя
**Описание:** Система должна позволять регистрацию новых пользователей с безопасным хэшированием паролей.

**Эндпоинт:** `POST /api/auth/register`

**Предусловия:** Нет

**Входные данные:**
```json
{
  "name": "string (required, max 200)",
  "email": "string (required, max 200)",
  "password": "string (required, min 6, max 100)"
}
```

**Основной сценарий:**
1. Клиент отправляет POST запрос с данными регистрации
2. Система проверяет уникальность email
3. Система валидирует пароль (минимум 6 символов)
4. Система хэширует пароль с использованием BCrypt
5. Система создает пользователя с ролью User
6. Система генерирует JWT токен
7. Система возвращает токен и данные пользователя (код 201)

**Альтернативные сценарии:**
- Email уже используется → ошибка 400 "Пользователь с таким email уже существует"
- Пароль менее 6 символов → ошибка 400 "Пароль должен содержать минимум 6 символов"
- Невалидные данные → ошибка 400 с описанием

**Возвращаемые данные:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "guid",
    "name": "string",
    "email": "string",
    "role": "User",
    "createdAt": "datetime",
    "updatedAt": null
  }
}
```

#### FR-AUTH-002: Вход в систему
**Описание:** Система должна позволять аутентификацию пользователей и выдачу JWT токенов.

**Эндпоинт:** `POST /api/auth/login`

**Предусловия:** Пользователь зарегистрирован

**Входные данные:**
```json
{
  "email": "string (required)",
  "password": "string (required)"
}
```

**Основной сценарий:**
1. Клиент отправляет POST запрос с email и паролем
2. Система находит пользователя по email
3. Система проверяет пароль через BCrypt
4. Система генерирует JWT токен с Claims (ID, Email, Name, Role)
5. Система возвращает токен и данные пользователя (код 200)

**Альтернативные сценарии:**
- Пользователь не найден → ошибка 400 "Неверный email или пароль"
- Неверный пароль → ошибка 400 "Неверный email или пароль"

**Возвращаемые данные:** объект AuthResponseDto (как в FR-AUTH-001)

#### FR-AUTH-003: Защита эндпоинтов
**Описание:** Все эндпоинты управления данными требуют JWT токен в заголовке Authorization.

**Формат заголовка:**
```
Authorization: Bearer {jwt_token}
```

**Защищенные контроллеры:**
- `UsersController` - все эндпоинты
- `MedicationsController` - все эндпоинты
- `MedicationIntakesController` - все эндпоинты

**Ответ при отсутствии токена:**
- Код 401 Unauthorized

**Ответ при невалидном токене:**
- Код 401 Unauthorized

---

### 2.2 Управление пользователями

#### FR-USER-001: Создание пользователя
**Описание:** Система должна позволять создавать нового пользователя с указанием имени и email.

**Предусловия:** Нет

**Основной сценарий:**
1. Клиент отправляет POST запрос `/api/users` с данными пользователя
2. Система проверяет уникальность email
3. Система валидирует данные:
   - Имя не пустое, длина ≤ 200 символов
   - Email корректного формата (содержит @), длина ≤ 200 символов
4. Система создает пользователя с уникальным ID
5. Система возвращает данные созданного пользователя (код 201)

**Альтернативные сценарии:**
- Email уже используется → ошибка 400 "Пользователь с таким email уже существует"
- Невалидные данные → ошибка 400 с описанием проблемы

**Постусловия:** Пользователь создан в системе (УСТАРЕЛО: используйте `/api/auth/register`)

**Примечание:** Данный эндпоинт устарел. Для создания пользователя используйте `/api/auth/register`, который обеспечивает безопасную регистрацию с хэшированием пароля.

#### FR-USER-002: Получение информации о пользователе
**Описание:** Система должна позволять получить информацию о пользователе по ID или email.

**Эндпоинты:**
- `GET /api/users/{id}` - получение по ID
- `GET /api/users/by-email/{email}` - получение по email

**Предусловия:** Пользователь существует в системе

**Основной сценарий:**
1. Клиент отправляет GET запрос с ID или email
2. Система ищет пользователя
3. Система возвращает данные пользователя (код 200)

**Альтернативные сценарии:**
- Пользователь не найден → ошибка 404 "Пользователь не найден"

**Возвращаемые данные:**
```json
{
  "id": "guid",
  "name": "string",
  "email": "string",
  "role": "User",
  "createdAt": "datetime",
  "updatedAt": "datetime?"
}
```

#### FR-USER-003: Получение списка всех пользователей
**Описание:** Система должна возвращать список всех зарегистрированных пользователей.

**Эндпоинт:** `GET /api/users`

**Предусловия:** Нет

**Основной сценарий:**
1. Клиент отправляет GET запрос
2. Система возвращает массив всех пользователей (код 200)

**Постусловия:** Список может быть пустым

#### FR-USER-004: Обновление данных пользователя
**Описание:** Система должна позволять обновлять имя и email пользователя.

**Эндпоинт:** `PUT /api/users/{id}`

**Предусловия:** Пользователь существует

**Основной сценарий:**
1. Клиент отправляет PUT запрос с новыми данными
2. Система проверяет существование пользователя
3. Если email изменился, проверяет его уникальность
4. Система валидирует данные
5. Система обновляет данные пользователя
6. Система устанавливает `UpdatedAt` в текущее время
7. Система возвращает обновленные данные (код 200)

**Альтернативные сценарии:**
- Пользователь не найден → ошибка 404
- Новый email уже занят → ошибка 400
- Невалидные данные → ошибка 400

#### FR-USER-005: Удаление пользователя
**Описание:** Система должна позволять удалять пользователя и все связанные данные.

**Эндпоинт:** `DELETE /api/users/{id}`

**Предусловия:** Пользователь существует

**Основной сценарий:**
1. Клиент отправляет DELETE запрос
2. Система проверяет существование пользователя
3. Система удаляет пользователя и все связанные данные (CASCADE)
4. Система возвращает код 204 (No Content)

**Альтернативные сценарии:**
- Пользователь не найден → ошибка 404

**Постусловия:** Пользователь и все его данные (лекарства, приемы) удалены

---

### 2.3 Управление лекарствами

#### FR-MED-001: Создание лекарства
**Описание:** Система должна позволять пользователю добавлять лекарство в свой список.

**Эндпоинт:** `POST /api/users/{userId}/medications`

**Предусловия:** Пользователь существует

**Входные данные:**
```json
{
  "name": "string (required, max 200)",
  "description": "string? (optional, max 1000)",
  "dosage": "string? (optional, max 100)"
}
```

**Основной сценарий:**
1. Клиент отправляет POST запрос с данными лекарства
2. Система проверяет существование пользователя
3. Система валидирует данные лекарства
4. Система создает лекарство с привязкой к пользователю
5. Система возвращает созданное лекарство (код 201)

**Альтернативные сценарии:**
- Пользователь не найден → ошибка 400 "Пользователь не найден"
- Невалидные данные → ошибка 400 с описанием
- Дублирование названия у пользователя → ошибка 400

**Постусловия:** Лекарство добавлено в список пользователя

#### FR-MED-002: Получение списка лекарств пользователя
**Описание:** Система должна возвращать список всех лекарств конкретного пользователя.

**Эндпоинт:** `GET /api/users/{userId}/medications`

**Предусловия:** Пользователь существует

**Основной сценарий:**
1. Клиент отправляет GET запрос
2. Система проверяет существование пользователя
3. Система возвращает отсортированный по имени список лекарств (код 200)

**Альтернативные сценарии:**
- Пользователь не найден → ошибка 400

**Возвращаемые данные:**
```json
[
  {
    "id": "guid",
    "userId": "guid",
    "name": "string",
    "description": "string?",
    "dosage": "string?",
    "createdAt": "datetime",
    "updatedAt": "datetime?"
  }
]
```

#### FR-MED-003: Получение информации о лекарстве
**Описание:** Система должна возвращать детальную информацию о конкретном лекарстве.

**Эндпоинт:** `GET /api/users/{userId}/medications/{id}`

**Предусловия:** 
- Пользователь существует
- Лекарство существует
- Лекарство принадлежит пользователю

**Основной сценарий:**
1. Клиент отправляет GET запрос
2. Система находит лекарство
3. Система проверяет принадлежность лекарства пользователю
4. Система возвращает данные лекарства (код 200)

**Альтернативные сценарии:**
- Лекарство не найдено → ошибка 404
- Лекарство принадлежит другому пользователю → ошибка 403 (Forbidden)

#### FR-MED-004: Обновление информации о лекарстве
**Описание:** Система должна позволять обновлять информацию о лекарстве.

**Эндпоинт:** `PUT /api/users/{userId}/medications/{id}`

**Предусловия:** 
- Пользователь и лекарство существуют
- Лекарство принадлежит пользователю

**Входные данные:**
```json
{
  "name": "string (required, max 200)",
  "description": "string? (optional, max 1000)",
  "dosage": "string? (optional, max 100)"
}
```

**Основной сценарий:**
1. Клиент отправляет PUT запрос с новыми данными
2. Система проверяет принадлежность лекарства пользователю
3. Система валидирует данные
4. Система обновляет информацию
5. Система устанавливает `UpdatedAt`
6. Система возвращает обновленные данные (код 200)

**Альтернативные сценарии:**
- Лекарство не найдено или не принадлежит пользователю → ошибка 404/403
- Невалидные данные → ошибка 400

#### FR-MED-005: Удаление лекарства
**Описание:** Система должна позволять удалять лекарство из списка пользователя.

**Эндпоинт:** `DELETE /api/users/{userId}/medications/{id}`

**Предусловия:** 
- Лекарство существует
- Лекарство принадлежит пользователю

**Основной сценарий:**
1. Клиент отправляет DELETE запрос
2. Система проверяет принадлежность лекарства пользователю
3. Система удаляет лекарство
4. Система возвращает код 204

**Альтернативные сценарии:**
- Лекарство не найдено или не принадлежит пользователю → ошибка 404/403

**Постусловия:** Лекарство удалено

---

### 2.4 Управление приемами лекарств

#### FR-INTAKE-001: Регистрация приема лекарства
**Описание:** Система должна позволять регистрировать факт приема лекарства.

**Эндпоинт:** `POST /api/users/{userId}/intakes`

**Предусловия:** 
- Пользователь существует
- Лекарство существует и принадлежит пользователю

**Входные данные:**
```json
{
  "medicationId": "guid (required)",
  "intakeTime": "datetime? (optional, defaults to current UTC time)",
  "notes": "string? (optional, max 500)"
}
```

**Основной сценарий:**
1. Клиент отправляет POST запрос
2. Система проверяет существование пользователя и лекарства
3. Система проверяет принадлежность лекарства пользователю
4. Если `intakeTime` не указано, система устанавливает текущее время UTC
5. Система валидирует данные
6. Система создает запись о приеме
7. Система возвращает созданную запись (код 201)

**Альтернативные сценарии:**
- Пользователь не найден → ошибка 400
- Лекарство не найдено → ошибка 400
- Лекарство принадлежит другому пользователю → ошибка 400
- `intakeTime` более чем через день → ошибка 400
- Невалидные примечания (>500 символов) → ошибка 400

**Возвращаемые данные:**
```json
{
  "id": "guid",
  "userId": "guid",
  "medicationId": "guid",
  "medicationName": "string",
  "intakeTime": "datetime",
  "notes": "string?",
  "createdAt": "datetime",
  "updatedAt": "datetime?"
}
```

#### FR-INTAKE-002: Получение истории приемов пользователя
**Описание:** Система должна возвращать историю приемов лекарств с возможностью фильтрации.

**Эндпоинт:** `GET /api/users/{userId}/intakes`

**Query параметры (все опциональные):**
- `fromDate` - начало периода (datetime)
- `toDate` - конец периода (datetime)
- `medicationId` - фильтр по конкретному лекарству (guid)

**Предусловия:** Пользователь существует

**Основной сценарий:**
1. Клиент отправляет GET запрос с опциональными фильтрами
2. Система проверяет существование пользователя
3. Система применяет фильтры (если указаны)
4. Система сортирует результаты по времени приема (DESC)
5. Система возвращает список приемов (код 200)

**Примеры запросов:**
```
GET /api/users/{userId}/intakes
GET /api/users/{userId}/intakes?fromDate=2024-12-01&toDate=2024-12-31
GET /api/users/{userId}/intakes?medicationId={guid}
GET /api/users/{userId}/intakes?fromDate=2024-12-01&medicationId={guid}
```

**Альтернативные сценарии:**
- Пользователь не найден → ошибка 400

**Возвращаемые данные:** Массив объектов приемов

#### FR-INTAKE-003: Получение информации о конкретном приеме
**Описание:** Система должна возвращать детальную информацию о конкретном приеме.

**Эндпоинт:** `GET /api/users/{userId}/intakes/{id}`

**Предусловия:** 
- Пользователь существует
- Запись о приеме существует
- Запись принадлежит пользователю

**Основной сценарий:**
1. Клиент отправляет GET запрос
2. Система находит запись
3. Система проверяет принадлежность записи пользователю
4. Система возвращает данные (код 200)

**Альтернативные сценарии:**
- Запись не найдена → ошибка 404
- Запись принадлежит другому пользователю → ошибка 403

#### FR-INTAKE-004: Обновление записи о приеме
**Описание:** Система должна позволять обновлять время приема и примечания.

**Эндпоинт:** `PUT /api/users/{userId}/intakes/{id}`

**Предусловия:** 
- Запись существует
- Запись принадлежит пользователю

**Входные данные:**
```json
{
  "intakeTime": "datetime (required)",
  "notes": "string? (optional, max 500)"
}
```

**Основной сценарий:**
1. Клиент отправляет PUT запрос
2. Система проверяет принадлежность записи пользователю
3. Система валидирует данные
4. Система обновляет запись
5. Система устанавливает `UpdatedAt`
6. Система возвращает обновленные данные (код 200)

**Альтернативные сценарии:**
- Запись не найдена или не принадлежит пользователю → ошибка 404/403
- Невалидные данные → ошибка 400

#### FR-INTAKE-005: Удаление записи о приеме
**Описание:** Система должна позволять удалять записи о приемах.

**Эндпоинт:** `DELETE /api/users/{userId}/intakes/{id}`

**Предусловия:** 
- Запись существует
- Запись принадлежит пользователю

**Основной сценарий:**
1. Клиент отправляет DELETE запрос
2. Система проверяет принадлежность записи пользователю
3. Система удаляет запись
4. Система возвращает код 204

**Альтернативные сценарии:**
- Запись не найдена или не принадлежит пользователю → ошибка 404/403

---

## 3. Нефункциональные требования

### 3.1 Производительность

**NFR-PERF-001:** Время отклика API
- Простые запросы (GET по ID): ≤ 100 мс (95-й перцентиль)
- Сложные запросы (фильтрация, JOIN): ≤ 500 мс (95-й перцентиль)
- Запросы на изменение данных (POST, PUT, DELETE): ≤ 200 мс (95-й перцентиль)

**NFR-PERF-002:** Масштабируемость
- Система должна поддерживать до 10,000 одновременных пользователей
- Горизонтальное масштабирование через добавление экземпляров API

**NFR-PERF-003:** Размер базы данных
- Оптимизация для работы с до 1 млн записей о приемах лекарств
- Индексация ключевых полей для быстрого поиска

### 3.2 Надежность

**NFR-REL-001:** Доступность
- Целевая доступность: 99.5% (uptime)
- Допустимое время простоя: ~3.65 дней в год

**NFR-REL-002:** Целостность данных
- ACID транзакции для всех операций изменения данных
- Каскадное удаление связанных данных
- Валидация на уровне домена и базы данных

**NFR-REL-003:** Резервное копирование
- Ежедневные автоматические резервные копии БД
- Возможность восстановления за последние 30 дней

### 3.3 Безопасность

**NFR-SEC-001:** Защита данных
- ✅ Хранение паролей в зашифрованном виде (BCrypt)
- ✅ Валидация всех входных данных
- HTTPS для всех соединений (рекомендуется для Production)

**NFR-SEC-002:** Аутентификация и авторизация
- ✅ JWT токены для аутентификации (HS256)
- ✅ Проверка принадлежности данных пользователю
- ✅ Защита всех эндпоинтов через [Authorize]
- ✅ Роли пользователей (User, Admin)
- Защита от CSRF атак (планируется)

**NFR-SEC-003:** Аудит
- ✅ Логирование всех операций изменения данных
- ✅ Сохранение временных меток действий
- Сохранение IP-адресов запросов (планируется)

**NFR-SEC-004:** Защита от перегрузки
- ✅ Rate Limiting (100 запросов/минуту с очередью до 5)

### 3.4 Удобство использования

**NFR-USE-001:** API
- RESTful API с понятной структурой эндпоинтов
- Согласованная обработка ошибок
- Информативные сообщения об ошибках

**NFR-USE-002:** Документация
- ✅ Полная API документация через Swagger UI (доступна на `/swagger`)
- OpenAPI 3.0 спецификация
- Интерактивное тестирование API эндпоинтов
- Примеры использования
- Руководство по развертыванию

### 3.5 Поддерживаемость

**NFR-MAIN-001:** Код
- Покрытие unit-тестами ≥ 80% для Domain и Application слоев
- Следование принципам SOLID и Clean Architecture
- Использование паттернов проектирования

**NFR-MAIN-002:** Логирование
- Структурированное логирование (Serilog)
- Уровни логирования: Debug, Info, Warning, Error, Fatal
- Ротация лог-файлов (ежедневно, хранение 30 дней)

**NFR-MAIN-003:** Мониторинг
- Health check эндпоинты
- Метрики производительности
- Алерты при критических ошибках

### 3.6 Совместимость

**NFR-COMP-001:** Платформы
- Кросс-платформенность (.NET 9.0)
- Поддержка Windows, Linux, macOS

**NFR-COMP-002:** База данных
- PostgreSQL 12+
- Возможность миграции на другие реляционные СУБД

**NFR-COMP-003:** Контейнеризация
- Docker контейнеры
- Docker Compose для оркестрации
- Kubernetes-ready архитектура

---

## 4. Архитектура системы

### 4.1 Архитектурный стиль
Система построена на основе **Clean Architecture** с применением принципов **Domain-Driven Design (DDD)** и паттерна **Rich Domain Model**.

### 4.2 Диаграмма слоев

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│       (MedicationAssist.API)           │
│  Controllers, Middleware, Filters       │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│        Application Layer                │
│    (MedicationAssist.Application)      │
│  Services, DTOs, Use Cases, Mappers     │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│          Domain Layer                   │
│      (MedicationAssist.Domain)         │
│  Entities, Value Objects, Interfaces    │
│  Business Rules, Domain Services        │
└──────────────▲──────────────────────────┘
               │
┌──────────────┴──────────────────────────┐
│      Infrastructure Layer               │
│   (MedicationAssist.Infrastructure)    │
│  Repositories, DbContext, Migrations    │
│  External Services, Persistence         │
└─────────────────────────────────────────┘
```

### 4.3 Зависимости между слоями

**Правила зависимостей:**
- Domain не зависит ни от чего (ядро системы)
- Application зависит только от Domain
- Infrastructure зависит от Domain и Application
- API зависит от Application и Infrastructure

**Принцип инверсии зависимостей:**
- Domain определяет интерфейсы репозиториев
- Infrastructure реализует эти интерфейсы
- Внедрение зависимостей происходит в API слое

### 4.4 Основные компоненты

#### 4.4.1 Domain Layer

**Entities (Сущности):**
```
User
├── Id: Guid
├── Name: string
├── Email: string
├── PasswordHash: string
├── Role: UserRole (enum: User, Admin)
├── CreatedAt: DateTime
├── UpdatedAt: DateTime?
├── Medications: IReadOnlyCollection<Medication>
├── MedicationIntakes: IReadOnlyCollection<MedicationIntake>
└── Methods:
    ├── AddMedication(name, description?, dosage?)
    ├── RemoveMedication(medicationId)
    ├── RecordMedicationIntake(medicationId, intakeTime?, notes?)
    ├── RemoveMedicationIntake(intakeId)
    ├── SetName(name)
    ├── SetEmail(email)
    ├── SetPasswordHash(passwordHash)
    └── SetRole(role)

Medication
├── Id: Guid
├── UserId: Guid
├── Name: string
├── Description: string?
├── Dosage: string?
├── CreatedAt: DateTime
├── UpdatedAt: DateTime?
└── Methods:
    ├── SetName(name)
    ├── SetDescription(description)
    └── SetDosage(dosage)

MedicationIntake
├── Id: Guid
├── UserId: Guid
├── MedicationId: Guid
├── IntakeTime: DateTime
├── Notes: string?
├── CreatedAt: DateTime
├── UpdatedAt: DateTime?
└── Methods:
    ├── SetIntakeTime(intakeTime)
    └── SetNotes(notes)
```

**Base Classes:**
- `Entity` - базовый класс для всех сущностей
- `ValueObject` - базовый класс для объектов-значений
- `DomainException` - исключения домена

**Repository Interfaces:**
- `IUserRepository`
- `IMedicationRepository`
- `IMedicationIntakeRepository`
- `IUnitOfWork`

#### 4.4.2 Application Layer

**Services:**
- `UserService` - управление пользователями
- `MedicationService` - управление лекарствами
- `MedicationIntakeService` - управление приемами
- `AuthService` - аутентификация и регистрация
- `IPasswordHasher` - интерфейс хэширования паролей
- `IJwtTokenService` - интерфейс генерации JWT токенов

**DTOs (Data Transfer Objects):**
- Request DTOs: `CreateUserDto`, `UpdateUserDto`, `RegisterDto`, `LoginDto`
- Response DTOs: `UserDto`, `MedicationDto`, `MedicationIntakeDto`, `AuthResponseDto`
- Filter DTOs: `MedicationIntakeFilterDto`

**Common:**
- `Result<T>` - паттерн для обработки ошибок
- `Result` - для операций без возвращаемого значения

#### 4.4.3 Infrastructure Layer

**Data Access:**
- `ApplicationDbContext` - контекст EF Core
- `UserRepository` - реализация IUserRepository
- `MedicationRepository` - реализация IMedicationRepository
- `MedicationIntakeRepository` - реализация IMedicationIntakeRepository
- `UnitOfWork` - реализация IUnitOfWork

**Entity Configurations:**
- `UserConfiguration` - конфигурация сущности User
- `MedicationConfiguration` - конфигурация сущности Medication
- `MedicationIntakeConfiguration` - конфигурация сущности MedicationIntake

**Security:**
- `PasswordHasher` - реализация хэширования паролей (BCrypt)
- `JwtTokenService` - реализация генерации JWT токенов
- `JwtSettings` - конфигурация JWT

**Migrations:**
- EF Core миграции для версионирования схемы БД
- `InitialCreate` - начальная структура БД
- `AddUserAuthentication` - добавление полей PasswordHash и Role

#### 4.4.4 API Layer

**Controllers:**
- `AuthController` - эндпоинты аутентификации (register, login)
- `UsersController` - эндпоинты управления пользователями (защищено [Authorize])
- `MedicationsController` - эндпоинты управления лекарствами (защищено [Authorize])
- `MedicationIntakesController` - эндпоинты управления приемами (защищено [Authorize])

**Configuration:**
- `Program.cs` - точка входа и конфигурация приложения
- `appsettings.json` - настройки приложения
- Dependency Injection - регистрация сервисов

### 4.5 Паттерны проектирования

**Используемые паттерны:**

1. **Repository Pattern** - абстракция доступа к данным
2. **Unit of Work** - управление транзакциями
3. **Dependency Injection** - инверсия управления
4. **Result Pattern** - обработка ошибок без исключений
5. **Rich Domain Model** - бизнес-логика в сущностях
6. **DTO Pattern** - разделение внутренней и внешней моделей данных
7. **Factory Pattern** - создание сложных объектов (косвенно через конструкторы)
8. **Strategy Pattern** - различные стратегии хэширования (IPasswordHasher)
9. **Service Layer Pattern** - бизнес-логика в сервисах (AuthService, UserService)
10. **Options Pattern** - конфигурация через IOptions<T> (JwtSettings)

### 4.6 Технологический стек

**Backend:**
- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core 9.0
- Npgsql.EntityFrameworkCore.PostgreSQL 9.0.2

**База данных:**
- PostgreSQL 17

**API Документация:**
- Swashbuckle.AspNetCore 6.9.0

**Безопасность:**
- Microsoft.AspNetCore.Authentication.JwtBearer 9.0.0
- System.IdentityModel.Tokens.Jwt 8.15.0
- BCrypt.Net-Next 4.0.3
- Microsoft.IdentityModel.Tokens 8.15.0

**Логирование:**
- Serilog 9.0.0
- Serilog.Sinks.File 7.0.0
- Serilog.AspNetCore 10.0.0

**Тестирование:**
- xUnit 2.9.2
- FluentAssertions 8.8.0
- Moq 4.20.72

**Контейнеризация:**
- Docker
- Docker Compose

**Дополнительные библиотеки:**
- Microsoft.Extensions.Logging.Abstractions 10.0.0
- Microsoft.Extensions.DependencyInjection 10.0.0

---

## 5. Модель данных

### 5.1 Концептуальная модель

```
User (1) ──────< (∞) Medication
 │
 │
 └──────< (∞) MedicationIntake >─────── (1) Medication
```

**Отношения:**
- User → Medications: один-ко-многим (1:N)
- User → MedicationIntakes: один-ко-многим (1:N)
- Medication → MedicationIntakes: один-ко-многим (1:N)

### 5.2 Физическая модель (PostgreSQL)

#### Таблица: Users

```sql
CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Email" VARCHAR(200) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(500) NOT NULL,
    "Role" VARCHAR(50) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP NULL
);

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
```

**Ограничения:**
- PK: Id
- UNIQUE: Email
- NOT NULL: Id, Name, Email, PasswordHash, Role, CreatedAt
- DEFAULT: Role = 'User'

#### Таблица: Medications

```sql
CREATE TABLE "Medications" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(1000) NULL,
    "Dosage" VARCHAR(100) NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP NULL,
    CONSTRAINT "FK_Medications_Users_UserId" 
        FOREIGN KEY ("UserId") 
        REFERENCES "Users" ("Id") 
        ON DELETE CASCADE
);

CREATE INDEX "IX_Medications_UserId_Name" 
    ON "Medications" ("UserId", "Name");
```

**Ограничения:**
- PK: Id
- FK: UserId → Users.Id (CASCADE DELETE)
- NOT NULL: Id, UserId, Name, CreatedAt
- INDEX: (UserId, Name) для быстрого поиска

#### Таблица: MedicationIntakes

```sql
CREATE TABLE "MedicationIntakes" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "MedicationId" UUID NOT NULL,
    "IntakeTime" TIMESTAMP NOT NULL,
    "Notes" VARCHAR(500) NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP NULL,
    CONSTRAINT "FK_MedicationIntakes_Users_UserId" 
        FOREIGN KEY ("UserId") 
        REFERENCES "Users" ("Id") 
        ON DELETE CASCADE,
    CONSTRAINT "FK_MedicationIntakes_Medications_MedicationId" 
        FOREIGN KEY ("MedicationId") 
        REFERENCES "Medications" ("Id") 
        ON DELETE RESTRICT
);

CREATE INDEX "IX_MedicationIntakes_UserId_IntakeTime" 
    ON "MedicationIntakes" ("UserId", "IntakeTime");
    
CREATE INDEX "IX_MedicationIntakes_MedicationId" 
    ON "MedicationIntakes" ("MedicationId");
```

**Ограничения:**
- PK: Id
- FK: UserId → Users.Id (CASCADE DELETE)
- FK: MedicationId → Medications.Id (RESTRICT DELETE)
- NOT NULL: Id, UserId, MedicationId, IntakeTime, CreatedAt
- INDEX: (UserId, IntakeTime) для фильтрации по дате
- INDEX: MedicationId для фильтрации по лекарству

### 5.3 Правила целостности данных

**Каскадное удаление:**
- При удалении User удаляются все его Medications и MedicationIntakes
- При удалении Medication НЕ удаляются связанные MedicationIntakes (RESTRICT)

**Уникальность:**
- Email должен быть уникальным в системе
- Название лекарства должно быть уникальным в рамках пользователя (логическое ограничение)

**Обязательность:**
- Все ID обязательны
- Name, Email, PasswordHash и Role у User обязательны
- Name у Medication обязательно
- IntakeTime у MedicationIntake обязательно

---

## 6. API Спецификация

### 6.1 Swagger UI / OpenAPI

**Swagger UI доступен в режиме Development:**
- **URL:** `http://localhost:5000/swagger`
- **OpenAPI спецификация:** `http://localhost:5000/swagger/v1/swagger.json`

**Возможности Swagger UI:**
- Интерактивная документация всех API эндпоинтов
- Тестирование API запросов прямо из браузера
- ✅ **JWT аутентификация** - кнопка "Authorize" для ввода Bearer токена
- Просмотр моделей данных (DTO)
- Описание параметров запросов и ответов
- Примеры JSON для Request/Response

**Информация об API:**
- **Title:** MedicationAssist API
- **Version:** v1.0
- **Description:** API для управления приемом лекарственных препаратов
- **Security:** JWT Bearer Authentication

**Как использовать аутентификацию в Swagger:**
1. Зарегистрируйтесь через POST `/api/auth/register`
2. Скопируйте полученный `token` из ответа
3. Нажмите кнопку **"Authorize"** в правом верхнем углу Swagger UI
4. Введите `Bearer {ваш_токен}` в поле Value
5. Нажмите "Authorize" и "Close"
6. Теперь все запросы будут отправляться с JWT токеном

**Примечание:** Swagger UI доступен только в Development окружении для безопасности. Для Production рекомендуется использовать отдельную документацию или защищенный Swagger с аутентификацией.

### 6.2 Общие принципы API

**Базовый URL:** `http://localhost:5000/api` (Development)

**Формат данных:**
- Request: `application/json`
- Response: `application/json`

**HTTP методы:**
- GET - получение данных
- POST - создание ресурса
- PUT - полное обновление ресурса
- DELETE - удаление ресурса

**Коды ответов:**
- 200 OK - успешный GET/PUT запрос
- 201 Created - успешное создание ресурса
- 204 No Content - успешное удаление
- 400 Bad Request - ошибка валидации или бизнес-логики
- 403 Forbidden - нет прав доступа к ресурсу
- 404 Not Found - ресурс не найден
- 500 Internal Server Error - внутренняя ошибка сервера

**Формат ошибок:**
```json
{
  "error": "Описание ошибки на русском языке"
}
```

**Аутентификация:**
Все эндпоинты (кроме `/api/auth/*`) требуют JWT токен в заголовке:
```
Authorization: Bearer {your_jwt_token}
```

### 6.3 Эндпоинты Authentication

#### POST /api/auth/register
Регистрация нового пользователя.

**Request Body:**
```json
{
  "name": "Иван Иванов",
  "email": "ivan@example.com",
  "password": "securepassword123"
}
```

**Валидация:**
- `name`: обязательное, макс 200 символов
- `email`: обязательное, корректный формат, макс 200 символов
- `password`: обязательное, минимум 6 символов, макс 100 символов

**Response 201:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Иван Иванов",
    "email": "ivan@example.com",
    "role": "User",
    "createdAt": "2024-12-02T10:00:00Z",
    "updatedAt": null
  }
}
```

**Response 400:**
```json
{
  "error": "Пользователь с таким email уже существует"
}
```

#### POST /api/auth/login
Вход в систему.

**Request Body:**
```json
{
  "email": "ivan@example.com",
  "password": "securepassword123"
}
```

**Response 200:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Иван Иванов",
    "email": "ivan@example.com",
    "role": "User",
    "createdAt": "2024-12-02T10:00:00Z",
    "updatedAt": null
  }
}
```

**Response 400:**
```json
{
  "error": "Неверный email или пароль"
}
```

**Детали JWT токена:**
- **Алгоритм:** HS256 (HMAC SHA256)
- **Время жизни:** 60 минут (Production), 1440 минут (Development)
- **Claims:**
  - `sub` - ID пользователя
  - `email` - Email пользователя
  - `name` - Имя пользователя
  - `role` - Роль пользователя (User/Admin)
  - `jti` - уникальный идентификатор токена

### 6.4 Эндпоинты Users

**⚠️ Требуется аутентификация:** Все эндпоинты требуют JWT токен в заголовке Authorization.

#### GET /api/users
Получить список всех пользователей.

**Response 200:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Иван Иванов",
    "email": "ivan@example.com",
    "role": "User",
    "createdAt": "2024-12-01T10:00:00Z",
    "updatedAt": null
  }
]
```

#### GET /api/users/{id}
Получить пользователя по ID.

**Parameters:**
- `id` (path, required): GUID пользователя

**Response 200:** объект UserDto

**Response 404:**
```json
{
  "error": "Пользователь не найден"
}
```

#### GET /api/users/by-email/{email}
Получить пользователя по email.

**Parameters:**
- `email` (path, required): Email пользователя

**Response 200:** объект UserDto

**Response 404:** объект с ошибкой

#### POST /api/users
Создать нового пользователя.

**Request Body:**
```json
{
  "name": "Иван Иванов",
  "email": "ivan@example.com"
}
```

**Response 201:** объект UserDto

**Response 400:**
```json
{
  "error": "Пользователь с таким email уже существует"
}
```

#### PUT /api/users/{id}
Обновить данные пользователя.

**Parameters:**
- `id` (path, required): GUID пользователя

**Request Body:**
```json
{
  "name": "Иван Петров",
  "email": "ivan.petrov@example.com"
}
```

**Response 200:** объект UserDto

**Response 400/404:** объект с ошибкой

#### DELETE /api/users/{id}
Удалить пользователя.

**Parameters:**
- `id` (path, required): GUID пользователя

**Response 204:** No Content

**Response 404:** объект с ошибкой

### 6.5 Эндпоинты Medications

**⚠️ Требуется аутентификация:** Все эндпоинты требуют JWT токен в заголовке Authorization.

#### GET /api/users/{userId}/medications
Получить все лекарства пользователя.

**Parameters:**
- `userId` (path, required): GUID пользователя

**Response 200:**
```json
[
  {
    "id": "guid",
    "userId": "guid",
    "name": "Аспирин",
    "description": "Обезболивающее средство",
    "dosage": "500mg",
    "createdAt": "2024-12-01T10:00:00Z",
    "updatedAt": null
  }
]
```

#### GET /api/users/{userId}/medications/{id}
Получить конкретное лекарство.

**Parameters:**
- `userId` (path, required): GUID пользователя
- `id` (path, required): GUID лекарства

**Response 200:** объект MedicationDto

**Response 403:** если лекарство принадлежит другому пользователю

**Response 404:** если лекарство не найдено

#### POST /api/users/{userId}/medications
Создать новое лекарство.

**Parameters:**
- `userId` (path, required): GUID пользователя

**Request Body:**
```json
{
  "name": "Аспирин",
  "description": "Обезболивающее средство",
  "dosage": "500mg"
}
```

**Response 201:** объект MedicationDto

**Response 400:** ошибка валидации или пользователь не найден

#### PUT /api/users/{userId}/medications/{id}
Обновить лекарство.

**Parameters:**
- `userId` (path, required): GUID пользователя
- `id` (path, required): GUID лекарства

**Request Body:**
```json
{
  "name": "Аспирин Форте",
  "description": "Обновленное описание",
  "dosage": "1000mg"
}
```

**Response 200:** объект MedicationDto

**Response 400/403/404:** объект с ошибкой

#### DELETE /api/users/{userId}/medications/{id}
Удалить лекарство.

**Parameters:**
- `userId` (path, required): GUID пользователя
- `id` (path, required): GUID лекарства

**Response 204:** No Content

**Response 403/404:** объект с ошибкой

### 6.6 Эндпоинты Medication Intakes

**⚠️ Требуется аутентификация:** Все эндпоинты требуют JWT токен в заголовке Authorization.

#### GET /api/users/{userId}/intakes
Получить историю приемов с фильтрацией.

**Parameters:**
- `userId` (path, required): GUID пользователя
- `fromDate` (query, optional): Начало периода (ISO 8601)
- `toDate` (query, optional): Конец периода (ISO 8601)
- `medicationId` (query, optional): GUID лекарства для фильтрации

**Examples:**
```
GET /api/users/{userId}/intakes
GET /api/users/{userId}/intakes?fromDate=2024-12-01T00:00:00Z
GET /api/users/{userId}/intakes?fromDate=2024-12-01T00:00:00Z&toDate=2024-12-31T23:59:59Z
GET /api/users/{userId}/intakes?medicationId={guid}
```

**Response 200:**
```json
[
  {
    "id": "guid",
    "userId": "guid",
    "medicationId": "guid",
    "medicationName": "Аспирин",
    "intakeTime": "2024-12-01T08:00:00Z",
    "notes": "Принято после завтрака",
    "createdAt": "2024-12-01T08:00:05Z",
    "updatedAt": null
  }
]
```

#### GET /api/users/{userId}/intakes/{id}
Получить конкретную запись о приеме.

**Parameters:**
- `userId` (path, required): GUID пользователя
- `id` (path, required): GUID записи

**Response 200:** объект MedicationIntakeDto

**Response 403/404:** объект с ошибкой

#### POST /api/users/{userId}/intakes
Зарегистрировать прием лекарства.

**Parameters:**
- `userId` (path, required): GUID пользователя

**Request Body:**
```json
{
  "medicationId": "guid",
  "intakeTime": "2024-12-01T08:00:00Z",
  "notes": "Принято после завтрака"
}
```

**Notes:**
- `intakeTime` опционально, если не указано - используется текущее время UTC
- `notes` опционально, максимум 500 символов

**Response 201:** объект MedicationIntakeDto

**Response 400:** ошибка валидации

#### PUT /api/users/{userId}/intakes/{id}
Обновить запись о приеме.

**Parameters:**
- `userId` (path, required): GUID пользователя
- `id` (path, required): GUID записи

**Request Body:**
```json
{
  "intakeTime": "2024-12-01T09:00:00Z",
  "notes": "Обновленное примечание"
}
```

**Response 200:** объект MedicationIntakeDto

**Response 400/403/404:** объект с ошибкой

#### DELETE /api/users/{userId}/intakes/{id}
Удалить запись о приеме.

**Parameters:**
- `userId` (path, required): GUID пользователя
- `id` (path, required): GUID записи

**Response 204:** No Content

**Response 403/404:** объект с ошибкой

---

## 7. Бизнес-правила и валидация

### 7.1 Правила валидации User

| Поле | Правило | Сообщение об ошибке |
|------|---------|---------------------|
| Name | Не пустое | "Имя пользователя не может быть пустым" |
| Name | Длина ≤ 200 | "Имя пользователя не может превышать 200 символов" |
| Email | Не пустое | "Email не может быть пустым" |
| Email | Содержит @ | "Некорректный формат email" |
| Email | Длина ≤ 200 | "Email не может превышать 200 символов" |
| Email | Уникальный | "Пользователь с таким email уже существует" |
| Password | Длина ≥ 6 (при регистрации) | "Пароль должен содержать минимум 6 символов" |
| Password | Длина ≤ 100 (при регистрации) | "Пароль не может превышать 100 символов" |
| PasswordHash | Не пустое | "Хэш пароля не может быть пустым" |
| PasswordHash | Длина ≤ 500 | "Хэш пароля не может превышать 500 символов" |
| Role | Валидное значение enum | "Недопустимая роль пользователя" |

### 7.2 Правила валидации Medication

| Поле | Правило | Сообщение об ошибке |
|------|---------|---------------------|
| Name | Не пустое | "Название лекарства не может быть пустым" |
| Name | Длина ≤ 200 | "Название лекарства не может превышать 200 символов" |
| Description | Длина ≤ 1000 (если указано) | "Описание лекарства не может превышать 1000 символов" |
| Dosage | Длина ≤ 100 (если указано) | "Дозировка не может превышать 100 символов" |
| UserId | Пользователь существует | "Пользователь не найден" |

**Дополнительные правила:**
- Название лекарства должно быть уникальным в рамках пользователя (case-insensitive)
- При удалении лекарства проверяется наличие связанных приемов

### 7.3 Правила валидации MedicationIntake

| Поле | Правило | Сообщение об ошибке |
|------|---------|---------------------|
| IntakeTime | Не более чем через день от текущего времени | "Время приема не может быть более чем через день" |
| Notes | Длина ≤ 500 (если указано) | "Примечания не могут превышать 500 символов" |
| UserId | Пользователь существует | "Пользователь не найден" |
| MedicationId | Лекарство существует | "Лекарство не найдено" |
| MedicationId | Лекарство принадлежит пользователю | "Лекарство принадлежит другому пользователю" |

**Дополнительные правила:**
- Если IntakeTime не указано, используется DateTime.UtcNow
- IntakeTime хранится и обрабатывается в UTC

### 7.4 Бизнес-правила

**BR-001:** Удаление пользователя
- При удалении пользователя каскадно удаляются все его лекарства и приемы

**BR-002:** Удаление лекарства
- Лекарство можно удалить только если нет связанных записей о приемах
- Если есть приемы, возвращается ошибка "Невозможно удалить лекарство, так как существуют записи о его приеме"

**BR-003:** Изменение email
- При изменении email проверяется, что новый email не занят другим пользователем

**BR-004:** Автоматические поля
- CreatedAt устанавливается автоматически при создании (DateTime.UtcNow)
- UpdatedAt устанавливается при любом изменении сущности
- Id генерируется как Guid.NewGuid() при создании

**BR-005:** Фильтрация приемов
- Фильтры применяются по принципу AND (все указанные фильтры должны совпадать)
- Результаты сортируются по IntakeTime DESC (новые сверху)

**BR-006:** Принадлежность данных
- Пользователь может видеть и изменять только свои лекарства и приемы
- Попытка доступа к чужим данным возвращает 403 Forbidden

---

## 8. Сценарии использования (Use Cases)

### 8.1 UC-001: Регистрация нового пользователя

**Актор:** Новый пользователь

**Предусловия:** Нет

**Основной сценарий:**
1. Пользователь предоставляет имя, email и пароль
2. Система проверяет уникальность email
3. Система валидирует данные (пароль минимум 6 символов)
4. Система хэширует пароль с использованием BCrypt
5. Система создает пользователя с уникальным ID и ролью User
6. Система генерирует JWT токен
7. Система возвращает токен и данные пользователя

**Альтернативные сценарии:**
- 2a. Email уже используется → ошибка "Пользователь с таким email уже существует"
- 3a. Пароль менее 6 символов → ошибка
- 3b. Данные невалидны → ошибка

**Постусловия:** 
- Пользователь зарегистрирован в системе
- Пользователь получил JWT токен для доступа к API

### 8.1.1 UC-001A: Вход в систему

**Актор:** Зарегистрированный пользователь

**Предусловия:** Пользователь зарегистрирован

**Основной сценарий:**
1. Пользователь предоставляет email и пароль
2. Система находит пользователя по email
3. Система проверяет пароль через BCrypt
4. Система генерирует JWT токен с Claims (ID, Email, Name, Role)
5. Система возвращает токен и данные пользователя

**Альтернативные сценарии:**
- 2a. Пользователь не найден → ошибка "Неверный email или пароль"
- 3a. Неверный пароль → ошибка "Неверный email или пароль"

**Постусловия:** Пользователь получил JWT токен для доступа к API

### 8.2 UC-002: Добавление лекарства в список

**Актор:** Зарегистрированный пользователь

**Предусловия:** Пользователь зарегистрирован

**Основной сценарий:**
1. Пользователь выбирает "Добавить лекарство"
2. Пользователь вводит название, описание и дозировку
3. Система валидирует данные
4. Система добавляет лекарство в список пользователя
5. Система отображает обновленный список

**Альтернативные сценарии:**
- 3a. Лекарство с таким названием уже есть → ошибка
- 3b. Название пустое или слишком длинное → ошибка

**Постусловия:** Лекарство добавлено в список пользователя

### 8.3 UC-003: Регистрация приема лекарства

**Актор:** Пользователь

**Предусловия:** 
- Пользователь зарегистрирован
- В списке есть хотя бы одно лекарство

**Основной сценарий:**
1. Пользователь выбирает "Зарегистрировать прием"
2. Пользователь выбирает лекарство из списка
3. Пользователь может указать время приема (опционально)
4. Пользователь может добавить примечание (опционально)
5. Система устанавливает текущее время, если время не указано
6. Система валидирует данные
7. Система сохраняет запись о приеме
8. Система подтверждает регистрацию приема

**Альтернативные сценарии:**
- 3a. Указано время более чем через день → ошибка
- 6a. Лекарство не найдено → ошибка

**Постусловия:** Прием лекарства зарегистрирован

### 8.4 UC-004: Просмотр истории приемов

**Актор:** Пользователь

**Предусловия:** Пользователь зарегистрирован

**Основной сценарий:**
1. Пользователь выбирает "История приемов"
2. Система отображает все приемы, отсортированные по времени
3. Пользователь может применить фильтры:
   - По диапазону дат
   - По конкретному лекарству
4. Система применяет фильтры и обновляет список

**Альтернативные сценарии:**
- 2a. История пустая → отображается сообщение "Нет записей"

**Постусловия:** История приемов отображена

### 8.5 UC-005: Редактирование записи о приеме

**Актор:** Пользователь

**Предусловия:** 
- Пользователь зарегистрирован
- Существует запись о приеме

**Основной сценарий:**
1. Пользователь выбирает запись в истории
2. Пользователь выбирает "Редактировать"
3. Пользователь изменяет время приема или примечание
4. Система валидирует данные
5. Система обновляет запись
6. Система отображает обновленную информацию

**Альтернативные сценарии:**
- 4a. Новое время невалидно → ошибка

**Постусловия:** Запись о приеме обновлена

### 8.6 UC-006: Удаление лекарства

**Актор:** Пользователь

**Предусловия:** 
- Пользователь зарегистрирован
- Лекарство существует в списке

**Основной сценарий:**
1. Пользователь выбирает лекарство
2. Пользователь выбирает "Удалить"
3. Система проверяет наличие записей о приемах
4. Система удаляет лекарство
5. Система обновляет список

**Альтернативные сценарии:**
- 3a. Есть записи о приемах → ошибка "Невозможно удалить лекарство"

**Постусловия:** Лекарство удалено из списка

---

## 9. Тестирование

### 9.1 Стратегия тестирования

**Уровни тестирования:**
1. **Unit Tests** - тестирование изолированных компонентов
2. **Integration Tests** - тестирование взаимодействия компонентов (планируется)
3. **API Tests** - тестирование REST API (планируется)
4. **Performance Tests** - нагрузочное тестирование (планируется)

### 9.2 Unit Tests

**Покрытие:** Domain и Application слои

**Текущие тесты:** 46 тестов (все обновлены для поддержки аутентификации)

**Domain Layer тесты (38 тестов):**

*UserTests.cs (22 теста):*
- Создание пользователя с валидными данными (включая passwordHash и role)
- Валидация имени (пустое, null, пробелы)
- Валидация email (пустое, null, неверный формат)
- Валидация passwordHash (пустое, длина)
- Валидация role
- Обновление имени
- Обновление email
- Обновление passwordHash и role
- Добавление лекарства
- Удаление лекарства
- Предотвращение дублирования названий лекарств
- Регистрация приема лекарства
- Использование текущего времени по умолчанию
- Удаление записи о приеме
- Ошибки при операциях с несуществующими данными

**Примечание:** Все тесты были обновлены для передачи параметров `passwordHash` и `role` в конструктор `User`.

*MedicationTests.cs (10 тестов):*
- Создание лекарства с валидными данными
- Валидация названия (пустое, null, пробелы)
- Обновление названия, описания, дозировки
- Валидация длины описания (max 1000)
- Валидация длины дозировки (max 100)

*MedicationIntakeTests.cs (6 тестов):*
- Создание записи о приеме
- Обновление времени приема
- Валидация времени (не более чем через день)
- Обновление примечаний
- Валидация длины примечаний (max 500)
- Поддержка null для примечаний

**Application Layer тесты (8 тестов):**

*UserServiceTests.cs (6 тестов):*
- Получение пользователя по ID (успех и ошибка)
- Создание пользователя (успех и дубликат email)
- Обновление пользователя
- Удаление пользователя (успех и ошибка)

*MedicationServiceTests.cs (6 тестов):*
- Получение лекарства по ID
- Получение списка лекарств пользователя
- Создание лекарства (успех и пользователь не найден)
- Обновление лекарства
- Удаление лекарства

### 9.3 Инструменты тестирования

**Фреймворки:**
- **xUnit** - основной фреймворк для unit-тестов
- **FluentAssertions** - выразительные утверждения
- **Moq** - создание mock-объектов для зависимостей

**Пример теста:**
```csharp
[Fact]
public void User_Constructor_Should_Create_Valid_User()
{
    // Arrange
    var name = "Иван Иванов";
    var email = "ivan@example.com";
    var passwordHash = "hashed_password_123";
    var role = UserRole.User;

    // Act
    var user = new User(name, email, passwordHash, role);

    // Assert
    user.Should().NotBeNull();
    user.Id.Should().NotBeEmpty();
    user.Name.Should().Be(name);
    user.Email.Should().Be(email);
    user.PasswordHash.Should().Be(passwordHash);
    user.Role.Should().Be(role);
    user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
}
```

### 9.4 Запуск тестов

**Команда:**
```bash
dotnet test
```

**Результат:**
```
Passed!  - Failed: 0, Passed: 46, Skipped: 0, Total: 46
```

---

## 10. Развертывание

### 10.1 Требования к окружению

**Минимальные требования:**
- CPU: 2 ядра
- RAM: 2 GB
- Disk: 10 GB
- OS: Windows 10+, Linux (Ubuntu 20.04+), macOS 11+

**Рекомендуемые требования:**
- CPU: 4 ядра
- RAM: 4 GB
- Disk: 20 GB

### 10.2 Зависимости

**Runtime:**
- .NET 9.0 Runtime
- PostgreSQL 12+

**Development:**
- .NET 9.0 SDK
- Docker Desktop (для Docker-развертывания)

### 10.3 Развертывание через Docker

**Файлы:**
- `Dockerfile` - образ API приложения
- `docker-compose.yml` - оркестрация сервисов

**Шаги:**
1. Клонировать репозиторий
2. Перейти в корневую директорию проекта
3. Выполнить команду: `docker-compose up --build`
4. API доступен на `http://localhost:5000`
5. **Swagger UI доступен на** `http://localhost:5000/swagger`
6. PostgreSQL доступен на `localhost:5432`

**Сервисы в docker-compose:**
- **postgres** - PostgreSQL 17 с health check
- **api** - REST API приложение

**Volumes:**
- `postgres_data` - хранение данных PostgreSQL
- `./logs` - логи приложения

### 10.4 Развертывание без Docker

**Шаги:**
1. Установить PostgreSQL 12+
2. Создать базу данных `medicationassist`
3. Обновить строку подключения в `appsettings.json`
4. **Настроить JWT секреты** в `appsettings.json` (секция `JwtSettings`)
5. Выполнить миграции: `dotnet ef database update --project MedicationAssist.Infrastructure --startup-project MedicationAssist.API`
6. Запустить приложение: `dotnet run --project MedicationAssist.API`
7. Открыть Swagger UI: `http://localhost:5000/swagger` (в Development режиме)
8. **Зарегистрироваться** через `/api/auth/register` для получения JWT токена

### 10.5 Переменные окружения

| Переменная | Описание | Значение по умолчанию |
|------------|----------|----------------------|
| ASPNETCORE_ENVIRONMENT | Окружение (Development/Production) | Development |
| ASPNETCORE_HTTP_PORTS | HTTP порты | 8080 |
| ConnectionStrings__DefaultConnection | Строка подключения к БД | См. appsettings.json |
| JwtSettings__Key | Секретный ключ для JWT (мин. 32 символа) | См. appsettings.json |
| JwtSettings__Issuer | Издатель JWT токена | MedicationAssist |
| JwtSettings__Audience | Аудитория JWT токена | MedicationAssistUsers |
| JwtSettings__ExpiresInMinutes | Время жизни токена в минутах | 60 |

### 10.6 Конфигурация

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=medicationassist;Username=postgres;Password=postgres"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    }
  },
  "JwtSettings": {
    "Key": "SuperSecretKeyThatIsAtLeast32CharactersLongForHS256",
    "Issuer": "MedicationAssist",
    "Audience": "MedicationAssistUsers",
    "ExpiresInMinutes": 60
  },
  "AllowedHosts": "*"
}
```

**⚠️ Важно для Production:**
- Генерируйте криптографически стойкий секретный ключ (минимум 32 символа)
- Храните ключ в безопасном хранилище (Azure Key Vault, AWS Secrets Manager)
- Никогда не публикуйте секретные ключи в репозитории

---

## 11. Мониторинг и логирование

### 11.1 Логирование

**Библиотека:** Serilog

**Sink'и:**
- Console - вывод в консоль
- File - запись в файлы с ротацией

**Конфигурация файлов:**
- Путь: `logs/medication-assist-YYYYMMDD.log`
- Ротация: ежедневно
- Retention: 30 дней
- Формат: `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}`

**Уровни логирования:**

| Уровень | Использование | Примеры |
|---------|---------------|---------|
| Debug | Детальная отладочная информация | Значения переменных, промежуточные состояния |
| Information | Основные события приложения | Создание пользователя, регистрация приема |
| Warning | Нештатные ситуации без ошибок | Попытка создать дубликат, валидационные ошибки |
| Error | Ошибки выполнения | Исключения, ошибки БД |
| Fatal | Критические ошибки | Невозможность запуска приложения |

**Примеры логов:**
```
2024-12-01 10:00:00.123 +00:00 [INF] Запуск приложения MedicationAssist
2024-12-01 10:00:05.456 +00:00 [INF] Миграции базы данных применены успешно
2024-12-01 10:05:12.789 +00:00 [INF] Создан новый пользователь 3fa85f64-5717-4562-b3fc-2c963f66afa6 с email ivan@example.com
2024-12-01 10:10:23.012 +00:00 [WRN] Попытка создать пользователя с существующим email ivan@example.com
```

### 11.2 Метрики (планируется)

**Планируемые метрики:**
- Количество запросов в секунду
- Время отклика API (p50, p95, p99)
- Количество ошибок
- Использование ресурсов (CPU, RAM)
- Количество активных подключений к БД

### 11.3 Health Checks (планируется)

**Эндпоинт:** `/health` (планируется)

**Проверки:**
- API доступность
- Подключение к PostgreSQL
- Доступное место на диске

**Текущая проверка работоспособности:**
- Swagger UI доступен для тестирования всех эндпоинтов: `/swagger`

---

## 12. Безопасность

### 12.1 Реализованные меры

**Аутентификация и авторизация:**
- ✅ JWT токены для аутентификации (HS256)
- ✅ Время жизни токенов: 60 минут (Production), 1440 минут (Development)
- ✅ Claims: UserId, Email, Name, Role
- ✅ Защита всех контроллеров через [Authorize]
- ✅ Публичные эндпоинты: `/api/auth/register` и `/api/auth/login`

**Хэширование паролей:**
- ✅ BCrypt.Net-Next 4.0.3
- ✅ Автоматическая генерация соли
- ✅ Защита от rainbow table атак

**Управление ролями:**
- ✅ Enum UserRole: User, Admin
- ✅ Роль по умолчанию: User
- ✅ Роль хранится в JWT Claims

**Rate Limiting:**
- ✅ Fixed Window Limiter
- ✅ Лимит: 100 запросов в минуту
- ✅ Очередь: 5 запросов (QueueProcessingOrder.OldestFirst)
- ✅ Автоматический HTTP 503 при превышении

**Валидация входных данных:**
- ✅ Проверка на уровне Domain (через методы сущностей)
- ✅ Проверка на уровне Application (в сервисах)
- ✅ Проверка типов данных на уровне API (ModelBinding)
- ✅ Валидация паролей (минимум 6 символов)

**CORS:**
- ✅ Настроена политика AllowAll для Development
- ⚠️ Для Production рекомендуется ограничить origins

**Логирование:**
- ✅ Все операции логируются с указанием email пользователя
- ✅ Логирование попыток входа (успешных и неуспешных)
- ✅ Ошибки детально записываются для анализа

### 12.2 Планируемые меры

**Расширенная аутентификация:**
- OAuth 2.0 / OpenID Connect (интеграция с Google, Microsoft)
- Refresh tokens (обновление JWT без повторного ввода пароля)
- Multi-factor authentication (2FA)
- Password reset через email

**Расширенная авторизация:**
- Policy-based authorization (более гибкие правила доступа)
- Resource-based authorization (проверка принадлежности ресурса)
- Permission-based access (детализированные права доступа)

**Улучшенное шифрование:**
- HTTPS (TLS 1.3+) обязательно для Production
- Шифрование чувствительных данных в БД (например, medical notes)
- Certificate pinning для мобильных приложений

**Дополнительная защита API:**
- Request throttling (динамическое ограничение)
- API Keys для внешних клиентов
- CAPTCHA для регистрации
- IP whitelisting для админских операций

**Расширенный аудит:**
- IP-адреса и геолокация запросов
- User Agent информация
- Session tracking (отслеживание всех сессий пользователя)
- Security event logging (подозрительная активность)

---

## 13. Расширяемость системы

### 13.1 Возможности для расширения

**Функциональные расширения:**
1. **Расписание приема** - автоматические напоминания
2. **Push-уведомления** - мобильные и email напоминания
3. **QR-коды** - быстрое добавление лекарств
4. **Фото лекарств** - визуальная идентификация
5. **Поиск в базе лекарств** - интеграция с внешними справочниками
6. **Взаимодействие лекарств** - предупреждения о несовместимости
7. **Дозы и запасы** - учет остатков лекарств
8. **Отчеты** - статистика приема, экспорт в PDF
9. **Врачи и рецепты** - управление назначениями
10. **Семейные аккаунты** - управление приемом для членов семьи

**Технические расширения:**
1. **Мобильные приложения** - iOS, Android
2. **Web-интерфейс** - SPA на React/Angular/Vue
3. **GraphQL API** - альтернатива REST
4. **SignalR** - real-time уведомления
5. **Background Jobs** - асинхронная обработка (Hangfire)
6. **Кеширование** - Redis для производительности
7. **Message Queue** - RabbitMQ/Kafka для событий
8. **Микросервисы** - разделение на отдельные сервисы
9. **API Gateway** - единая точка входа
10. **Service Mesh** - управление микросервисами

### 13.2 Точки расширения

**Domain Layer:**
- Новые сущности (Doctor, Prescription, Pharmacy)
- Новые Value Objects (Email, PhoneNumber, Address)
- Доменные события (UserCreated, MedicationTaken)
- Доменные сервисы (DrugInteractionChecker)

**Application Layer:**
- Новые сервисы (ReminderService, ReportService)
- Command/Query separation (CQRS)
- Event handlers (для доменных событий)
- Validators (FluentValidation)

**Infrastructure Layer:**
- Новые репозитории
- Интеграции с внешними API
- File storage (для фото)
- Email/SMS providers

**API Layer:**
- Новые контроллеры
- WebSocket endpoints
- Webhook endpoints
- Background tasks

---

## 14. Ограничения и допущения

### 14.1 Текущие ограничения

**Функциональные:**
1. Нет системы напоминаний (push, email уведомления)
2. Нет мобильных приложений (iOS, Android)
3. Нет web-интерфейса (только REST API)
4. Ограниченная фильтрация (только по датам и лекарству)
5. Нет функции восстановления пароля
6. Нет multi-factor authentication (2FA)

**Технические:**
1. Single-instance развертывание (нет кластеризации)
2. Отсутствует кеширование (Redis/MemoryCache)
3. Простая валидация email (только наличие @)
4. Все даты в UTC (нет поддержки часовых поясов)
5. Нет Refresh Tokens (требуется повторный логин после истечения JWT)
6. Базовый Rate Limiting (fixed window, не per-user)

### 14.2 Допущения

**Бизнес-логика:**
1. Пользователи вводят данные честно и корректно
2. Одно лекарство = одна дозировка (нет истории изменения дозировок)
3. Время приема точно до минуты (нет поддержки "примерно")
4. Нет различия между запланированным и фактическим приемом

**Технические:**
1. БД и API размещены в одной сети (низкая latency)
2. Часовой пояс сервера не имеет значения (используется UTC)
3. Объем данных одного пользователя умеренный (<10000 приемов)
4. Доступна стабильная сеть

---

## 15. Глоссарий

**Aggregate (Агрегат)** - кластер связанных объектов, рассматриваемых как единое целое для изменения данных. В проекте User является агрегатом.

**BCrypt** - криптографическая хеш-функция для безопасного хэширования паролей с автоматической генерацией соли.

**Bearer Token** - тип токена аутентификации, передаваемый в заголовке Authorization как "Bearer {token}".

**Claims** - утверждения о пользователе, содержащиеся в JWT токене (ID, Email, Role и т.д.).

**Clean Architecture** - архитектурный подход, разделяющий систему на слои с явными зависимостями от внутренних к внешним слоям.

**DDD (Domain-Driven Design)** - подход к разработке сложного ПО, фокусирующийся на моделировании предметной области.

**Domain Event** - событие, произошедшее в домене, о котором другие части системы должны знать.

**DTO (Data Transfer Object)** - объект, используемый для передачи данных между слоями системы.

**Entity (Сущность)** - объект с уникальной идентичностью, который существует во времени.

**HS256 (HMAC SHA256)** - алгоритм подписи JWT токенов, использующий секретный ключ.

**JWT (JSON Web Token)** - компактный токен безопасности, используемый для аутентификации и передачи Claims между сторонами.

**Medication (Лекарство)** - медицинский препарат, который принимает пользователь.

**Medication Intake (Прием лекарства)** - зарегистрированный факт приема лекарственного препарата.

**Rate Limiting** - механизм ограничения количества запросов к API для предотвращения перегрузки.

**Repository (Репозиторий)** - паттерн для абстракции доступа к данным, предоставляющий коллекцию-подобный интерфейс.

**Rich Domain Model** - доменная модель, содержащая бизнес-логику в самих объектах домена.

**Role (Роль)** - категория пользователя, определяющая уровень доступа (User, Admin).

**Unit of Work** - паттерн для управления транзакциями и координации изменений нескольких репозиториев.

**UTC (Coordinated Universal Time)** - всемирное координированное время, используемое для хранения временных меток.

**Value Object (Объект-значение)** - объект без идентичности, определяемый своими атрибутами.

---

## 16. Ссылки и документы

### 16.1 Связанные документы

- `README.md` - руководство по использованию и запуску
- `PROJECT_OVERVIEW.md` - обзор архитектуры и структуры проекта
- **API документация (Swagger UI)** - доступна по адресу `/swagger` в режиме Development

### 16.2 Внешние ресурсы

**Документация технологий:**
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Serilog](https://serilog.net/)
- [xUnit](https://xunit.net/)

**Паттерны и практики:**
- [Domain-Driven Design](https://martinfowler.com/tags/domain%20driven%20design.html)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)

---

## 17. История изменений

| Версия | Дата | Автор | Изменения |
|--------|------|-------|-----------|
| 1.3 | 06.12.2024 | Команда разработки | Добавлена поддержка JWT авторизации в Swagger UI:<br>- Кнопка "Authorize" для ввода Bearer токена<br>- SecurityDefinition и SecurityRequirement для OpenAPI<br>- Понижена версия Swashbuckle.AspNetCore до 6.9.0 для совместимости |
| 1.2 | 02.12.2024 | Команда разработки | Реализована система аутентификации и авторизации:<br>- JWT токены (Microsoft.AspNetCore.Authentication.JwtBearer 9.0.0)<br>- Хэширование паролей (BCrypt.Net-Next 4.0.3)<br>- Роли пользователей (User, Admin)<br>- AuthController (/api/auth/register, /api/auth/login)<br>- Rate Limiting (100 req/min)<br>- Защита всех контроллеров через [Authorize]<br>- Миграция AddUserAuthentication (PasswordHash, Role)<br>- Обновлена модель User и UserDto |
| 1.1 | 01.12.2024 | Команда разработки | Добавлен Swagger UI (Swashbuckle.AspNetCore 6.9.0) |
| 1.0 | 01.12.2024 | Команда разработки | Первая версия спецификации |

---

## 18. Приложения

### 18.1 Примеры сценариев

**Сценарий 1: Пожилой человек с хроническим заболеванием**

Иван Петрович, 70 лет, принимает 5 разных лекарств ежедневно.

1. Регистрируется в системе
2. Добавляет все свои лекарства с дозировками
3. Каждый раз после приема регистрирует факт через телефон
4. Раз в неделю просматривает историю, чтобы убедиться, что ничего не пропустил
5. Показывает историю врачу на приеме

**Сценарий 2: Молодая мама с маленьким ребенком**

Анна, 28 лет, следит за приемом витаминов ребенка и своих послеродовых препаратов.

1. Создает аккаунт
2. Добавляет витамины для ребенка и свои препараты
3. Отмечает прием сразу после того, как дала лекарство ребенку
4. Добавляет примечания о реакции ребенка
5. Фильтрует историю по конкретному витамину для отчета педиатру

---

**Конец спецификации**

_Документ является актуальным на дату последней версии._

