-- Ручное добавление Telegram полей в таблицу Users
-- Используйте этот скрипт только если автоматические миграции не работают

-- Проверка существования колонки TelegramUserId
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'Users' 
        AND column_name = 'TelegramUserId'
    ) THEN
        -- Добавляем колонку TelegramUserId
        ALTER TABLE "Users" ADD COLUMN "TelegramUserId" bigint NULL;
        RAISE NOTICE 'Column TelegramUserId added successfully';
    ELSE
        RAISE NOTICE 'Column TelegramUserId already exists';
    END IF;
END $$;

-- Проверка существования колонки TelegramUsername
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'Users' 
        AND column_name = 'TelegramUsername'
    ) THEN
        -- Добавляем колонку TelegramUsername
        ALTER TABLE "Users" ADD COLUMN "TelegramUsername" character varying(255) NULL;
        RAISE NOTICE 'Column TelegramUsername added successfully';
    ELSE
        RAISE NOTICE 'Column TelegramUsername already exists';
    END IF;
END $$;

-- Проверка существования индекса IX_Users_TelegramUserId
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE tablename = 'Users' 
        AND indexname = 'IX_Users_TelegramUserId'
    ) THEN
        -- Создаём индекс на TelegramUserId
        CREATE INDEX "IX_Users_TelegramUserId" ON "Users" ("TelegramUserId");
        RAISE NOTICE 'Index IX_Users_TelegramUserId created successfully';
    ELSE
        RAISE NOTICE 'Index IX_Users_TelegramUserId already exists';
    END IF;
END $$;

-- Добавляем запись в таблицу миграций (если нужно)
-- ВНИМАНИЕ: Это нужно только если вы применяли этот скрипт вручную
-- и хотите отметить, что миграция 20251215120000_AddTelegramFieldsToUser применена
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20251215120000_AddTelegramFieldsToUser', '9.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" 
    WHERE "MigrationId" = '20251215120000_AddTelegramFieldsToUser'
);

-- Проверка результата
SELECT 
    column_name, 
    data_type, 
    character_maximum_length,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'Users' 
AND column_name IN ('TelegramUserId', 'TelegramUsername')
ORDER BY column_name;

-- Проверка индексов
SELECT 
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename = 'Users' 
AND indexname LIKE '%Telegram%';
