using Telegram.Bot.Types.ReplyMarkups;

namespace MedicationAssist.TelegramBot.Keyboards;

/// <summary>
/// Клавиатура для подписки на канал
/// </summary>
public static class SubscriptionKeyboard
{
    /// <summary>
    /// Получить клавиатуру с кнопкой подписки на канал
    /// </summary>
    /// <param name="channelUrl">URL канала</param>
    public static InlineKeyboardMarkup GetKeyboard(string channelUrl)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("📢 Подписаться на канал", channelUrl)
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Я подписался", "recheck_subscription")
            }
        });
    }
}
