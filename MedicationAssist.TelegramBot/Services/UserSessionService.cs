using System.Collections.Concurrent;

namespace MedicationAssist.TelegramBot.Services;

/// <summary>
/// Состояние диалога с пользователем
/// </summary>
public enum ConversationState
{
    None,

    // Аутентификация
    AwaitingEmail,
    AwaitingPassword,
    AwaitingName,
    AwaitingRegisterEmail,
    AwaitingRegisterPassword,
    AwaitingRegisterName,

    // Лекарства
    AwaitingMedicationName,
    AwaitingMedicationDosage,
    AwaitingMedicationDescription,

    // Приёмы
    AwaitingIntakeNotes,

    // Напоминания
    AwaitingReminderTime,
}

/// <summary>
/// Данные сессии пользователя
/// </summary>
public class UserSession
{
    /// <summary>
    /// Telegram User ID
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// ID пользователя в системе (после авторизации)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Текущее состояние диалога
    /// </summary>
    public ConversationState State { get; set; } = ConversationState.None;

    /// <summary>
    /// Временные данные для многошаговых операций
    /// </summary>
    public Dictionary<string, object> TempData { get; set; } = new();

    /// <summary>
    /// Время последней активности
    /// </summary>
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Авторизован ли пользователь
    /// </summary>
    public bool IsAuthenticated => UserId.HasValue;

    /// <summary>
    /// Флаг обработки операции (для предотвращения повторных нажатий)
    /// </summary>
    public bool IsProcessing { get; set; }

    /// <summary>
    /// Сбросить состояние диалога
    /// </summary>
    public void ResetState()
    {
        State = ConversationState.None;
        TempData.Clear();
    }

    /// <summary>
    /// Выход из аккаунта
    /// </summary>
    public void Logout()
    {
        UserId = null;
        UserName = null;
        ResetState();
    }
}

/// <summary>
/// Сервис управления сессиями пользователей
/// </summary>
public class UserSessionService
{
    private readonly ConcurrentDictionary<long, UserSession> _sessions = new();
    private readonly ILogger<UserSessionService> _logger;

    public UserSessionService(ILogger<UserSessionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Получить или создать сессию пользователя
    /// </summary>
    public UserSession GetOrCreateSession(long telegramUserId)
    {
        return _sessions.GetOrAdd(telegramUserId, id => new UserSession
        {
            TelegramUserId = id
        });
    }

    /// <summary>
    /// Получить сессию пользователя (если существует)
    /// </summary>
    public UserSession? GetSession(long telegramUserId)
    {
        return _sessions.TryGetValue(telegramUserId, out var session) ? session : null;
    }

    /// <summary>
    /// Авторизовать пользователя
    /// </summary>
    public void Authenticate(long telegramUserId, Guid userId, string userName)
    {
        var session = GetOrCreateSession(telegramUserId);
        session.UserId = userId;
        session.UserName = userName;
        session.ResetState();
        session.LastActivity = DateTime.UtcNow;

        _logger.LogInformation(
            "Пользователь {TelegramUserId} авторизован как {UserName} (ID: {UserId})",
            telegramUserId, userName, userId);
    }

    /// <summary>
    /// Выйти из аккаунта
    /// </summary>
    public void Logout(long telegramUserId)
    {
        if (_sessions.TryGetValue(telegramUserId, out var session))
        {
            _logger.LogInformation(
                "Пользователь {TelegramUserId} ({UserName}) вышел из аккаунта",
                telegramUserId, session.UserName);
            session.Logout();
        }
    }

    /// <summary>
    /// Установить состояние диалога
    /// </summary>
    public void SetState(long telegramUserId, ConversationState state)
    {
        var session = GetOrCreateSession(telegramUserId);
        session.State = state;
        session.LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Сбросить состояние диалога
    /// </summary>
    public void ResetState(long telegramUserId)
    {
        var session = GetOrCreateSession(telegramUserId);
        session.ResetState();
        session.LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Сохранить временные данные
    /// </summary>
    public void SetTempData(long telegramUserId, string key, object value)
    {
        var session = GetOrCreateSession(telegramUserId);
        session.TempData[key] = value;
        session.LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Получить временные данные
    /// </summary>
    public T? GetTempData<T>(long telegramUserId, string key)
    {
        var session = GetOrCreateSession(telegramUserId);
        if (session.TempData.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Очистить временные данные
    /// </summary>
    public void ClearTempData(long telegramUserId)
    {
        var session = GetOrCreateSession(telegramUserId);
        session.TempData.Clear();
    }

    /// <summary>
    /// Получить все активные сессии авторизованных пользователей
    /// </summary>
    public IEnumerable<UserSession> GetAuthenticatedSessions()
    {
        return _sessions.Values.Where(s => s.IsAuthenticated);
    }

    /// <summary>
    /// Получить Telegram ID по User ID системы
    /// </summary>
    public long? GetTelegramUserIdByUserId(Guid userId)
    {
        return _sessions.Values
            .FirstOrDefault(s => s.UserId == userId)
            ?.TelegramUserId;
    }

    /// <summary>
    /// Очистить неактивные сессии (старше указанного времени)
    /// </summary>
    public int CleanupInactiveSessions(TimeSpan inactivityThreshold)
    {
        var cutoff = DateTime.UtcNow - inactivityThreshold;
        var toRemove = _sessions
            .Where(kvp => !kvp.Value.IsAuthenticated && kvp.Value.LastActivity < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in toRemove)
        {
            _sessions.TryRemove(key, out _);
        }

        if (toRemove.Count > 0)
        {
            _logger.LogInformation("Очищено {Count} неактивных сессий", toRemove.Count);
        }

        return toRemove.Count;
    }
}

