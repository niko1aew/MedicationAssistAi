# Инструкция по применению миграции WebLoginTokens вручную

## 📋 Содержание

- [Файлы миграции](#файлы-миграции)
- [Предварительные требования](#предварительные-требования)
- [Быстрый старт](#быстрый-старт)
- [Применение миграции вручную](#применение-миграции-вручную)
- [Откат миграции](#откат-миграции)
- [Проверка результата](#проверка-результата)
- [Возможные проблемы](#возможные-проблемы)

---

## 📁 Файлы миграции

- `add-web-login-token-table.sql` - SQL скрипт создания таблицы
- `apply-web-login-migration.sh` - Bash скрипт для автоматического применения
- `rollback-web-login-migration.sql` - SQL скрипт для отката миграции

---

## ✅ Предварительные требования

1. **Docker и docker-compose** установлены и запущены
2. **PostgreSQL контейнер** запущен:
   ```bash
   docker-compose up -d postgres
   ```
3. Проверьте имя контейнера:
   ```bash
   docker ps | grep postgres
   ```

---

## 🚀 Быстрый старт

### Вариант 1: Автоматическое применение (рекомендуется)

```bash
# Дать права на выполнение скрипта
chmod +x apply-web-login-migration.sh

# Применить миграцию
./apply-web-login-migration.sh
```

### Вариант 2: Ручное применение SQL

```bash
# Применить SQL скрипт напрямую
docker exec -i medicationassist-postgres psql -U postgres -d medication_assist < add-web-login-token-table.sql
```

---

## 🔧 Применение миграции вручную

### Шаг 1: Проверка подключения к БД

```bash
docker exec -it medicationassist-postgres psql -U postgres -d medication_assist
```

Если подключение успешно, увидите приглашение `medication_assist=#`

### Шаг 2: Проверка существующих таблиц

```sql
\dt
```

Убедитесь, что таблица `Users` существует (необходима для foreign key).

### Шаг 3: Применение SQL скрипта

**Вариант A: Из терминала (находясь вне PostgreSQL)**

```bash
docker exec -i medicationassist-postgres psql -U postgres -d medication_assist < add-web-login-token-table.sql
```

**Вариант B: Изнутри PostgreSQL**

```bash
# Войти в контейнер
docker exec -it medicationassist-postgres bash

# Войти в PostgreSQL
psql -U postgres -d medication_assist

# Выполнить содержимое файла (если файл скопирован в контейнер)
\i /path/to/add-web-login-token-table.sql

# Или вставить содержимое вручную
```

### Шаг 4: Проверка результата

```sql
-- Проверить структуру таблицы
\d "WebLoginTokens"

-- Проверить индексы
\di "WebLoginTokens"*

-- Проверить foreign keys
SELECT conname, contype
FROM pg_constraint
WHERE conrelid = '"WebLoginTokens"'::regclass;
```

---

## ⏪ Откат миграции

Если необходимо удалить таблицу `WebLoginTokens`:

### Автоматический откат

```bash
docker exec -i medicationassist-postgres psql -U postgres -d medication_assist < rollback-web-login-migration.sql
```

### Ручной откат

```sql
-- Войти в PostgreSQL
docker exec -it medicationassist-postgres psql -U postgres -d medication_assist

-- Удалить таблицу
DROP TABLE IF EXISTS "WebLoginTokens" CASCADE;

-- Проверить удаление
\dt "WebLoginTokens"
```

---

## ✔️ Проверка результата

### 1. Проверка таблицы

```bash
docker exec -it medicationassist-postgres psql -U postgres -d medication_assist -c "\d \"WebLoginTokens\""
```

**Ожидаемый результат:**

```
                          Table "public.WebLoginTokens"
   Column   |           Type           | Collation | Nullable | Default
------------+--------------------------+-----------+----------+---------
 Id         | uuid                     |           | not null |
 Token      | character varying(64)    |           | not null |
 UserId     | uuid                     |           | not null |
 ExpiresAt  | timestamp with time zone |           | not null |
 IsUsed     | boolean                  |           | not null | false
 UsedAt     | timestamp with time zone |           |          |
 CreatedAt  | timestamp with time zone |           | not null |
 UpdatedAt  | timestamp with time zone |           |          |
Indexes:
    "PK_WebLoginTokens" PRIMARY KEY, btree ("Id")
    "IX_WebLoginTokens_Token" UNIQUE, btree ("Token")
    "IX_WebLoginTokens_ExpiresAt" btree ("ExpiresAt")
    "IX_WebLoginTokens_UserId" btree ("UserId")
    "IX_WebLoginTokens_UserId_IsUsed_ExpiresAt" btree ("UserId", "IsUsed", "ExpiresAt")
Foreign-key constraints:
    "FK_WebLoginTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
```

### 2. Проверка количества индексов

```bash
docker exec -it medicationassist-postgres psql -U postgres -d medication_assist -c \
  "SELECT COUNT(*) as index_count FROM pg_indexes WHERE tablename = 'WebLoginTokens';"
```

**Ожидаемый результат:** `index_count = 5`

### 3. Тестовая вставка данных

```sql
-- Войти в PostgreSQL
docker exec -it medicationassist-postgres psql -U postgres -d medication_assist

-- Вставить тестовый токен (замените user_id на существующий)
INSERT INTO "WebLoginTokens" ("Id", "Token", "UserId", "ExpiresAt", "IsUsed", "CreatedAt")
VALUES (
    gen_random_uuid(),
    'test_token_12345678901234567890',
    (SELECT "Id" FROM "Users" LIMIT 1),
    NOW() + INTERVAL '5 minutes',
    false,
    NOW()
);

-- Проверить вставку
SELECT * FROM "WebLoginTokens";

-- Удалить тестовую запись
DELETE FROM "WebLoginTokens" WHERE "Token" = 'test_token_12345678901234567890';
```

---

## 🐛 Возможные проблемы

### Проблема 1: Контейнер не найден

**Ошибка:**

```
Error: No such container: medicationassist-postgres
```

**Решение:**

```bash
# Проверить имя контейнера
docker ps

# Обновить переменную CONTAINER_NAME в скрипте apply-web-login-migration.sh
# или использовать правильное имя контейнера
```

### Проблема 2: База данных не существует

**Ошибка:**

```
FATAL: database "medication_assist" does not exist
```

**Решение:**

```bash
# Создать базу данных
docker exec -it medicationassist-postgres psql -U postgres -c "CREATE DATABASE medication_assist;"

# Или проверить правильное имя БД в docker-compose.yml
```

### Проблема 3: Таблица Users не существует

**Ошибка:**

```
ERROR: relation "Users" does not exist
```

**Решение:**

```bash
# Применить базовые миграции сначала
dotnet ef database update --project MedicationAssist.Infrastructure --startup-project MedicationAssist.API

# Или убедиться, что все предыдущие миграции применены
```

### Проблема 4: Таблица уже существует

**Предупреждение:**

```
NOTICE: relation "WebLoginTokens" already exists, skipping
```

**Решение:**
Это нормально, если таблица уже создана. Скрипт использует `CREATE TABLE IF NOT EXISTS`.

### Проблема 5: Права доступа к скрипту

**Ошибка:**

```
bash: ./apply-web-login-migration.sh: Permission denied
```

**Решение:**

```bash
chmod +x apply-web-login-migration.sh
```

---

## 📝 Примечания

1. **Безопасность:** SQL скрипт использует `IF NOT EXISTS`, поэтому безопасно запускать несколько раз
2. **Индексы:** Все необходимые индексы создаются автоматически для оптимальной производительности
3. **Foreign Key:** Связь с таблицей `Users` с каскадным удалением (ON DELETE CASCADE)
4. **Комментарии:** Добавлены комментарии к таблице и столбцам для документации

---

## 🔗 Связанные файлы

- `README_WEB_LOGIN_FEATURE.md` - Общая документация фичи
- `docs/FRONTEND_SPEC_WEB_LOGIN.md` - Спецификация для frontend
- Миграция EF Core: `MedicationAssist.Infrastructure/Migrations/..._AddWebLoginToken.cs`

---

## ✅ Checklist применения миграции

- [ ] PostgreSQL контейнер запущен
- [ ] Подключение к БД проверено
- [ ] Таблица `Users` существует
- [ ] SQL скрипт применён без ошибок
- [ ] Таблица `WebLoginTokens` создана
- [ ] Все 5 индексов созданы
- [ ] Foreign key constraint установлен
- [ ] Тестовая вставка прошла успешно
- [ ] Приложение перезапущено

**После выполнения всех пунктов миграция считается успешно применённой! ✨**
