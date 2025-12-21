using MedicationAssist.Application.DTOs;

namespace MedicationAssist.Application.Services;

/// <summary>
/// Интерфейс сервиса для работы с Telegram Mini App
/// </summary>
public interface ITelegramWebAppService
{
    /// <summary>
    /// Валидирует initData от Telegram Web App
    /// Проверяет подпись HMAC-SHA256 и срок действия данных
    /// </summary>
    /// <param name="initData">URL-encoded строка initData от Telegram</param>
    /// <returns>Результат валидации с данными пользователя</returns>
    TelegramWebAppValidationResult ValidateInitData(string initData);
}
