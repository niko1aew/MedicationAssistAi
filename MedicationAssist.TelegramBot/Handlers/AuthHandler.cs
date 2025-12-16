using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using MedicationAssist.TelegramBot.Keyboards;
using MedicationAssist.TelegramBot.Resources;
using MedicationAssist.TelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MedicationAssist.TelegramBot.Handlers;

/// <summary>
/// Обработчик команд аутентификации
/// </summary>
public class AuthHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserSessionService _sessionService;
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ILogger<AuthHandler> _logger;

    public AuthHandler(
        ITelegramBotClient botClient,
        UserSessionService sessionService,
        IAuthService authService,
        IUserService userService,
        ILogger<AuthHandler> logger)
    {
        _botClient = botClient;
        _sessionService = sessionService;
        _authService = authService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Показать меню аутентификации
    /// </summary>
    public async Task ShowAuthMenuAsync(long chatId, CancellationToken ct)
    {
        await _botClient.SendMessage(
            chatId,
            Messages.AuthRequired,
            replyMarkup: InlineKeyboards.AuthMenu,
            cancellationToken: ct);
    }

    /// <summary>
    /// Начать процесс входа
    /// </summary>
    public async Task StartLoginAsync(long chatId, long userId, CancellationToken ct)
    {
        _sessionService.SetState(userId, ConversationState.AwaitingEmail);
        await _botClient.SendMessage(
            chatId,
            Messages.EnterEmail,
            replyMarkup: InlineKeyboards.CancelButton,
            cancellationToken: ct);
    }

    /// <summary>
    /// Начать процесс регистрации
    /// </summary>
    public async Task StartRegisterAsync(long chatId, long userId, CancellationToken ct)
    {
        _sessionService.SetState(userId, ConversationState.AwaitingRegisterName);
        await _botClient.SendMessage(
            chatId,
            Messages.EnterName,
            replyMarkup: InlineKeyboards.CancelButton,
            cancellationToken: ct);
    }

    /// <summary>
    /// Быстрый старт (автоматическая регистрация через Telegram ID)
    /// </summary>
    public async Task QuickStartAsync(long chatId, User telegramUser, CancellationToken ct)
    {
        var email = $"{telegramUser.Id}@telegram.local";

        // Проверяем, существует ли уже пользователь с таким email
        var existingUser = await _userService.GetByEmailAsync(email, ct);

        if (existingUser.IsSuccess && existingUser.Data != null)
        {
            // Пользователь уже существует - привязываем Telegram если еще не привязан
            if (existingUser.Data.TelegramUserId == null || existingUser.Data.TelegramUserId != telegramUser.Id)
            {
                var linkResult = await _userService.LinkTelegramAsync(
                    existingUser.Data.Id,
                    new LinkTelegramDto(telegramUser.Id, telegramUser.Username),
                    ct);

                if (!linkResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Failed to link Telegram account for user {UserId}: {Error}",
                        existingUser.Data.Id, linkResult.Error);
                }
            }

            _sessionService.Authenticate(telegramUser.Id, existingUser.Data.Id, existingUser.Data.Name);

            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.WelcomeBack, existingUser.Data.Name),
                replyMarkup: InlineKeyboards.MainMenu,
                cancellationToken: ct);

            _logger.LogInformation(
                "Telegram user {TelegramUserId} authenticated via quick start as {Email}",
                telegramUser.Id, email);
            return;
        }

        // Пользователь не существует - регистрируем нового
        var password = Guid.NewGuid().ToString();
        var name = telegramUser.FirstName + (string.IsNullOrEmpty(telegramUser.LastName) ? "" : " " + telegramUser.LastName);

        if (string.IsNullOrWhiteSpace(name))
        {
            name = telegramUser.Username ?? $"User{telegramUser.Id}";
        }

        var registerDto = new RegisterDto { Name = name, Email = email, Password = password };
        var result = await _authService.RegisterAsync(registerDto);

        if (result.IsSuccess)
        {
            // Привязываем Telegram аккаунт к новому пользователю
            var linkResult = await _userService.LinkTelegramAsync(
                result.Data!.User.Id,
                new LinkTelegramDto(telegramUser.Id, telegramUser.Username),
                ct);

            if (!linkResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to link Telegram account for user {UserId}: {Error}",
                    result.Data.User.Id, linkResult.Error);
            }

            _sessionService.Authenticate(telegramUser.Id, result.Data!.User.Id, result.Data.User.Name);

            var credentialsMessage = $"{Messages.QuickStartSuccess}\n\n" +
                                   $"🌐 <b>Ссылка на сайт:</b> https://medications.meteoassist.space/\n" +
                                   $"👤 <b>Логин (Email):</b> <code>{email}</code>\n" +
                                   $"🔑 <b>Пароль:</b> <code>{password}</code>\n\n" +
                                   $"💡 <i>Сохраните эти данные для входа на сайт!</i>";

            await _botClient.SendMessage(
                chatId,
                credentialsMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: InlineKeyboards.MainMenu,
                cancellationToken: ct);

            _logger.LogInformation(
                "Quick registration of Telegram user {TelegramUserId} as {Email}",
                telegramUser.Id, email);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.Error, result.Error),
                replyMarkup: InlineKeyboards.AuthMenu,
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Обработать ввод email для входа
    /// </summary>
    public async Task HandleEmailInputAsync(long chatId, long userId, string email, CancellationToken ct)
    {
        if (!email.Contains('@'))
        {
            await _botClient.SendMessage(
                chatId,
                Messages.InvalidEmail,
                replyMarkup: InlineKeyboards.CancelButton,
                cancellationToken: ct);
            return;
        }

        _sessionService.SetTempData(userId, "email", email);
        _sessionService.SetState(userId, ConversationState.AwaitingPassword);

        await _botClient.SendMessage(
            chatId,
            Messages.EnterPassword,
            replyMarkup: InlineKeyboards.CancelButton,
            cancellationToken: ct);
    }

    /// <summary>
    /// Обработать ввод пароля для входа
    /// </summary>
    public async Task HandlePasswordInputAsync(long chatId, User telegramUser, string password, CancellationToken ct)
    {
        var userId = telegramUser.Id;
        var email = _sessionService.GetTempData<string>(userId, "email");

        if (string.IsNullOrEmpty(email))
        {
            _sessionService.ResetState(userId);
            await ShowAuthMenuAsync(chatId, ct);
            return;
        }

        var result = await _authService.LoginAsync(new LoginDto { Email = email, Password = password });

        if (result.IsSuccess)
        {
            // Привязываем Telegram аккаунт если еще не привязан
            if (result.Data!.User.TelegramUserId == null || result.Data.User.TelegramUserId != userId)
            {
                var linkResult = await _userService.LinkTelegramAsync(
                    result.Data.User.Id,
                    new LinkTelegramDto(userId, telegramUser.Username),
                    ct);

                if (!linkResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "Failed to link Telegram account for user {UserId}: {Error}",
                        result.Data.User.Id, linkResult.Error);
                }
            }

            _sessionService.Authenticate(userId, result.Data!.User.Id, result.Data.User.Name);

            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.LoginSuccess, result.Data.User.Name),
                replyMarkup: InlineKeyboards.MainMenu,
                cancellationToken: ct);

            _logger.LogInformation(
                "Telegram user {TelegramUserId} logged in as {Email}",
                userId, email);
        }
        else
        {
            _sessionService.ResetState(userId);

            await _botClient.SendMessage(
                chatId,
                Messages.InvalidCredentials,
                replyMarkup: InlineKeyboards.AuthMenu,
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Обработать ввод имени для регистрации
    /// </summary>
    public async Task HandleRegisterNameInputAsync(long chatId, long userId, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            await _botClient.SendMessage(
                chatId,
                "❌ Имя должно быть от 1 до 200 символов.",
                replyMarkup: InlineKeyboards.CancelButton,
                cancellationToken: ct);
            return;
        }

        _sessionService.SetTempData(userId, "name", name);
        _sessionService.SetState(userId, ConversationState.AwaitingRegisterEmail);

        await _botClient.SendMessage(
            chatId,
            Messages.EnterEmail,
            replyMarkup: InlineKeyboards.CancelButton,
            cancellationToken: ct);
    }

    /// <summary>
    /// Обработать ввод email для регистрации
    /// </summary>
    public async Task HandleRegisterEmailInputAsync(long chatId, long userId, string email, CancellationToken ct)
    {
        if (!email.Contains('@') || email.Length > 200)
        {
            await _botClient.SendMessage(
                chatId,
                Messages.InvalidEmail,
                replyMarkup: InlineKeyboards.CancelButton,
                cancellationToken: ct);
            return;
        }

        _sessionService.SetTempData(userId, "email", email);
        _sessionService.SetState(userId, ConversationState.AwaitingRegisterPassword);

        await _botClient.SendMessage(
            chatId,
            Messages.EnterPassword,
            replyMarkup: InlineKeyboards.CancelButton,
            cancellationToken: ct);
    }

    /// <summary>
    /// Обработать ввод пароля для регистрации
    /// </summary>
    public async Task HandleRegisterPasswordInputAsync(long chatId, User telegramUser, string password, CancellationToken ct)
    {
        var userId = telegramUser.Id;
        if (password.Length < 6)
        {
            await _botClient.SendMessage(
                chatId,
                Messages.PasswordTooShort,
                replyMarkup: InlineKeyboards.CancelButton,
                cancellationToken: ct);
            return;
        }

        var name = _sessionService.GetTempData<string>(userId, "name");
        var email = _sessionService.GetTempData<string>(userId, "email");

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
        {
            _sessionService.ResetState(userId);
            await StartRegisterAsync(chatId, userId, ct);
            return;
        }

        var result = await _authService.RegisterAsync(new RegisterDto { Name = name, Email = email, Password = password });

        if (result.IsSuccess)
        {
            // Привязываем Telegram аккаунт к новому пользователю
            var linkResult = await _userService.LinkTelegramAsync(
                result.Data!.User.Id,
                new LinkTelegramDto(userId, telegramUser.Username),
                ct);

            if (!linkResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to link Telegram account for user {UserId}: {Error}",
                    result.Data.User.Id, linkResult.Error);
            }

            _sessionService.Authenticate(userId, result.Data!.User.Id, result.Data.User.Name);

            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.RegisterSuccess, result.Data.User.Name),
                replyMarkup: InlineKeyboards.MainMenu,
                cancellationToken: ct);

            _logger.LogInformation(
                "New user registered via Telegram: {Email}",
                email);
        }
        else
        {
            _sessionService.ResetState(userId);

            var errorMessage = result.Error?.Contains("email") == true
                ? Messages.EmailExists
                : string.Format(Messages.Error, result.Error);

            await _botClient.SendMessage(
                chatId,
                errorMessage,
                replyMarkup: InlineKeyboards.AuthMenu,
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Выход из аккаунта
    /// </summary>
    public async Task LogoutAsync(long chatId, long userId, CancellationToken ct)
    {
        _sessionService.Logout(userId);

        await _botClient.SendMessage(
            chatId,
            Messages.LogoutSuccess,
            replyMarkup: InlineKeyboards.AuthMenu,
            cancellationToken: ct);
    }
}

