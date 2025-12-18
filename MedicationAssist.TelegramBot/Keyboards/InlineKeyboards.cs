using MedicationAssist.Application.DTOs;
using Telegram.Bot.Types.ReplyMarkups;

namespace MedicationAssist.TelegramBot.Keyboards;

/// <summary>
/// Фабрика inline-клавиатур для бота
/// </summary>
public static class InlineKeyboards
{
    /// <summary>
    /// Главное меню для авторизованного пользователя
    /// </summary>
    public static InlineKeyboardMarkup MainMenu => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("💊 Мои лекарства", "medications") },
        new[] { InlineKeyboardButton.WithCallbackData("✅ Записать приём", "intake") },
        new[] { InlineKeyboardButton.WithCallbackData("📜 История приёмов", "history") },
        new[] { InlineKeyboardButton.WithCallbackData("⏰ Напоминания", "reminders") },
        new[] { InlineKeyboardButton.WithCallbackData("⚙️ Настройки", "settings") },
    });

    /// <summary>
    /// Меню аутентификации
    /// </summary>
    public static InlineKeyboardMarkup AuthMenu => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("🔑 Войти в аккаунт", "login") },
        new[] { InlineKeyboardButton.WithCallbackData("📝 Зарегистрироваться", "register") },
        new[] { InlineKeyboardButton.WithCallbackData("⚡ Быстрый старт", "quick_start") },
    });

    /// <summary>
    /// Меню управления лекарствами
    /// </summary>
    public static InlineKeyboardMarkup MedicationsMenu => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить лекарство", "add_medication") },
        new[] { InlineKeyboardButton.WithCallbackData("📋 Список лекарств", "list_medications") },
        new[] { InlineKeyboardButton.WithCallbackData("🗑️ Удалить лекарство", "delete_medication_menu") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "main_menu") },
    });

    /// <summary>
    /// Меню настроек
    /// </summary>
    public static InlineKeyboardMarkup SettingsMenu => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("🚪 Выйти из аккаунта", "logout") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "main_menu") },
    });

    /// <summary>
    /// Кнопка возврата в главное меню
    /// </summary>
    public static InlineKeyboardMarkup BackToMainMenu => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Главное меню", "main_menu") },
    });

    /// <summary>
    /// Кнопка отмены операции
    /// </summary>
    public static InlineKeyboardMarkup CancelButton => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel") },
    });

    /// <summary>
    /// Кнопки подтверждения/отмены
    /// </summary>
    public static InlineKeyboardMarkup ConfirmCancel(string confirmCallback, string cancelCallback = "cancel") => new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("✅ Да", confirmCallback),
            InlineKeyboardButton.WithCallbackData("❌ Нет", cancelCallback),
        },
    });

    /// <summary>
    /// Список лекарств в виде кнопок
    /// </summary>
    public static InlineKeyboardMarkup MedicationsList(IEnumerable<MedicationDto> medications, string callbackPrefix)
    {
        var buttons = medications
            .Select(m => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"💊 {m.Name}" + (string.IsNullOrEmpty(m.Dosage) ? "" : $" ({m.Dosage})"),
                    $"{callbackPrefix}:{m.Id}")
            })
            .ToList();

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "main_menu") });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Меню напоминаний
    /// </summary>
    public static InlineKeyboardMarkup RemindersMenu => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить напоминание", "add_reminder") },
        new[] { InlineKeyboardButton.WithCallbackData("📋 Мои напоминания", "list_reminders") },
        new[] { InlineKeyboardButton.WithCallbackData("🗑️ Удалить напоминание", "delete_reminder_menu") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "main_menu") },
    });

    /// <summary>
    /// Выбор периода истории
    /// </summary>
    public static InlineKeyboardMarkup HistoryPeriodMenu => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("📅 Сегодня", "history:today") },
        new[] { InlineKeyboardButton.WithCallbackData("📅 Вчера", "history:yesterday") },
        new[] { InlineKeyboardButton.WithCallbackData("📅 За неделю", "history:week") },
        new[] { InlineKeyboardButton.WithCallbackData("📅 За месяц", "history:month") },
        new[] { InlineKeyboardButton.WithCallbackData("📅 За всё время", "history:all") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "main_menu") },
    });

    /// <summary>
    /// Действия с конкретным лекарством
    /// </summary>
    public static InlineKeyboardMarkup MedicationActions(Guid medicationId) => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("✅ Записать приём", $"quick_intake:{medicationId}") },
        new[] { InlineKeyboardButton.WithCallbackData("⏰ Добавить напоминание", $"med_add_reminder:{medicationId}") },
        new[] { InlineKeyboardButton.WithCallbackData("📜 История приёмов", $"medication_history:{medicationId}") },
        new[] { InlineKeyboardButton.WithCallbackData("🗑️ Удалить", $"confirm_delete_med:{medicationId}") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "list_medications") },
    });

    /// <summary>
    /// После записи приёма
    /// </summary>
    public static InlineKeyboardMarkup AfterIntakeMenu => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("✅ Записать ещё", "intake") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Главное меню", "main_menu") },
    });

    /// <summary>
    /// После добавления лекарства
    /// </summary>
    public static InlineKeyboardMarkup AfterAddMedicationMenu => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить ещё", "add_medication") },
        new[] { InlineKeyboardButton.WithCallbackData("⏰ Добавить напоминание", "add_reminder") },
        new[] { InlineKeyboardButton.WithCallbackData("◀️ Главное меню", "main_menu") },
    });

    /// <summary>
    /// Кнопки действий для напоминания о приёме лекарства
    /// </summary>
    public static InlineKeyboardMarkup ReminderActions(Guid reminderId, Guid medicationId) => new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("✅ Принять", $"take_reminder:{reminderId}"),
            InlineKeyboardButton.WithCallbackData("⏭️ Пропустить", $"skip_reminder:{reminderId}")
        }
    });
}

