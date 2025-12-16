using System.Text;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using MedicationAssist.TelegramBot.Keyboards;
using MedicationAssist.TelegramBot.Resources;
using MedicationAssist.TelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MedicationAssist.TelegramBot.Handlers;

/// <summary>
/// Обработчик команд управления лекарствами
/// </summary>
public class MedicationHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserSessionService _sessionService;
    private readonly IMedicationService _medicationService;
    private readonly ILogger<MedicationHandler> _logger;

    public MedicationHandler(
        ITelegramBotClient botClient,
        UserSessionService sessionService,
        IMedicationService medicationService,
        ILogger<MedicationHandler> logger)
    {
        _botClient = botClient;
        _sessionService = sessionService;
        _medicationService = medicationService;
        _logger = logger;
    }

    /// <summary>
    /// Показать меню лекарств
    /// </summary>
    public async Task ShowMedicationsMenuAsync(long chatId, CancellationToken ct)
    {
        await _botClient.SendMessage(
            chatId,
            "💊 Управление лекарствами",
            replyMarkup: InlineKeyboards.MedicationsMenu,
            cancellationToken: ct);
    }

    /// <summary>
    /// Показать список лекарств пользователя
    /// </summary>
    public async Task ShowMedicationsListAsync(long chatId, long userId, CancellationToken ct)
    {
        var session = _sessionService.GetSession(userId);
        if (session?.UserId == null)
        {
            await _botClient.SendMessage(chatId, Messages.AuthRequired, cancellationToken: ct);
            return;
        }

        var result = await _medicationService.GetByUserIdAsync(session.UserId.Value, ct);
        
        if (!result.IsSuccess)
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.Error, result.Error),
                replyMarkup: InlineKeyboards.BackToMainMenu,
                cancellationToken: ct);
            return;
        }

        var medications = result.Data!.ToList();
        
        if (medications.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                Messages.NoMedications,
                replyMarkup: InlineKeyboards.MedicationsMenu,
                cancellationToken: ct);
            return;
        }

        var sb = new StringBuilder();
        foreach (var med in medications)
        {
            var dosage = string.IsNullOrEmpty(med.Dosage) 
                ? "" 
                : string.Format(Messages.MedicationDosage, med.Dosage);
            sb.AppendLine(string.Format(Messages.MedicationItem, med.Name, dosage));
        }

        await _botClient.SendMessage(
            chatId,
            string.Format(Messages.MedicationsList, sb.ToString()),
            replyMarkup: InlineKeyboards.MedicationsList(medications, "med_details"),
            cancellationToken: ct);
    }

    /// <summary>
    /// Показать детали лекарства
    /// </summary>
    public async Task ShowMedicationDetailsAsync(long chatId, Guid medicationId, CancellationToken ct)
    {
        var result = await _medicationService.GetByIdAsync(medicationId, ct);
        
        if (!result.IsSuccess)
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.Error, result.Error),
                replyMarkup: InlineKeyboards.BackToMainMenu,
                cancellationToken: ct);
            return;
        }

        var med = result.Data!;
        var message = string.Format(
            Messages.MedicationDetails,
            med.Name,
            string.IsNullOrEmpty(med.Dosage) ? Messages.NotSpecified : med.Dosage,
            string.IsNullOrEmpty(med.Description) ? Messages.NotSpecified : med.Description,
            med.CreatedAt.ToString(Messages.DateFormat));

        await _botClient.SendMessage(
            chatId,
            message,
            replyMarkup: InlineKeyboards.MedicationActions(medicationId),
            cancellationToken: ct);
    }

    /// <summary>
    /// Начать добавление нового лекарства
    /// </summary>
    public async Task StartAddMedicationAsync(long chatId, long userId, CancellationToken ct)
    {
        _sessionService.SetState(userId, ConversationState.AwaitingMedicationName);
        _sessionService.ClearTempData(userId);
        
        await _botClient.SendMessage(
            chatId,
            Messages.EnterMedicationName,
            replyMarkup: InlineKeyboards.CancelButton,
            cancellationToken: ct);
    }

    /// <summary>
    /// Обработать ввод названия лекарства
    /// </summary>
    public async Task HandleMedicationNameInputAsync(long chatId, long userId, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            await _botClient.SendMessage(
                chatId,
                "❌ Название не может быть пустым.",
                replyMarkup: InlineKeyboards.CancelButton,
                cancellationToken: ct);
            return;
        }

        if (name.Length > 200)
        {
            await _botClient.SendMessage(
                chatId,
                Messages.MedicationNameTooLong,
                replyMarkup: InlineKeyboards.CancelButton,
                cancellationToken: ct);
            return;
        }

        _sessionService.SetTempData(userId, "medicationName", name);
        _sessionService.SetState(userId, ConversationState.AwaitingMedicationDosage);
        
        await _botClient.SendMessage(
            chatId,
            Messages.EnterMedicationDosage,
            replyMarkup: InlineKeyboards.CancelButton,
            cancellationToken: ct);
    }

    /// <summary>
    /// Обработать ввод дозировки лекарства
    /// </summary>
    public async Task HandleMedicationDosageInputAsync(long chatId, long userId, string? dosage, CancellationToken ct)
    {
        if (dosage?.ToLower() == "/skip")
        {
            dosage = null;
        }
        else if (dosage?.Length > 100)
        {
            await _botClient.SendMessage(
                chatId,
                "❌ Дозировка не может превышать 100 символов.",
                replyMarkup: InlineKeyboards.CancelButton,
                cancellationToken: ct);
            return;
        }

        _sessionService.SetTempData(userId, "medicationDosage", dosage ?? "");
        _sessionService.SetState(userId, ConversationState.AwaitingMedicationDescription);
        
        await _botClient.SendMessage(
            chatId,
            Messages.EnterMedicationDescription,
            replyMarkup: InlineKeyboards.CancelButton,
            cancellationToken: ct);
    }

    /// <summary>
    /// Обработать ввод описания и создать лекарство
    /// </summary>
    public async Task HandleMedicationDescriptionInputAsync(long chatId, long userId, string? description, CancellationToken ct)
    {
        if (description?.ToLower() == "/skip")
        {
            description = null;
        }
        else if (description?.Length > 1000)
        {
            await _botClient.SendMessage(
                chatId,
                "❌ Описание не может превышать 1000 символов.",
                replyMarkup: InlineKeyboards.CancelButton,
                cancellationToken: ct);
            return;
        }

        var session = _sessionService.GetSession(userId);
        if (session?.UserId == null)
        {
            _sessionService.ResetState(userId);
            await _botClient.SendMessage(chatId, Messages.AuthRequired, cancellationToken: ct);
            return;
        }

        var name = _sessionService.GetTempData<string>(userId, "medicationName");
        var dosage = _sessionService.GetTempData<string>(userId, "medicationDosage");

        if (string.IsNullOrEmpty(name))
        {
            _sessionService.ResetState(userId);
            await StartAddMedicationAsync(chatId, userId, ct);
            return;
        }

        var createDto = new CreateMedicationDto(
            name,
            string.IsNullOrEmpty(description) ? null : description,
            string.IsNullOrEmpty(dosage) ? null : dosage);

        var result = await _medicationService.CreateAsync(session.UserId.Value, createDto, ct);

        _sessionService.ResetState(userId);

        if (result.IsSuccess)
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.MedicationAdded, name),
                replyMarkup: InlineKeyboards.AfterAddMedicationMenu,
                cancellationToken: ct);
            
            _logger.LogInformation(
                "User {UserId} added medication: {MedicationName}",
                session.UserId, name);
        }
        else
        {
            var errorMessage = result.Error?.Contains("уже существует") == true
                ? Messages.MedicationExists
                : string.Format(Messages.Error, result.Error);
            
            await _botClient.SendMessage(
                chatId,
                errorMessage,
                replyMarkup: InlineKeyboards.MedicationsMenu,
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// Показать меню удаления лекарств
    /// </summary>
    public async Task ShowDeleteMedicationMenuAsync(long chatId, long userId, CancellationToken ct)
    {
        var session = _sessionService.GetSession(userId);
        if (session?.UserId == null)
        {
            await _botClient.SendMessage(chatId, Messages.AuthRequired, cancellationToken: ct);
            return;
        }

        var result = await _medicationService.GetByUserIdAsync(session.UserId.Value, ct);
        
        if (!result.IsSuccess || !result.Data!.Any())
        {
            await _botClient.SendMessage(
                chatId,
                Messages.NoMedications,
                replyMarkup: InlineKeyboards.MedicationsMenu,
                cancellationToken: ct);
            return;
        }

        await _botClient.SendMessage(
            chatId,
            Messages.SelectMedicationToDelete,
            replyMarkup: InlineKeyboards.MedicationsList(result.Data!, "confirm_delete_med"),
            cancellationToken: ct);
    }

    /// <summary>
    /// Показать подтверждение удаления
    /// </summary>
    public async Task ShowDeleteConfirmationAsync(long chatId, Guid medicationId, CancellationToken ct)
    {
        var result = await _medicationService.GetByIdAsync(medicationId, ct);
        
        if (!result.IsSuccess)
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.Error, result.Error),
                cancellationToken: ct);
            return;
        }

        await _botClient.SendMessage(
            chatId,
            string.Format(Messages.ConfirmDeleteMedication, result.Data!.Name),
            replyMarkup: InlineKeyboards.ConfirmCancel($"delete_med:{medicationId}", "list_medications"),
            cancellationToken: ct);
    }

    /// <summary>
    /// Удалить лекарство
    /// </summary>
    public async Task DeleteMedicationAsync(long chatId, Guid medicationId, CancellationToken ct)
    {
        var medResult = await _medicationService.GetByIdAsync(medicationId, ct);
        var medName = medResult.IsSuccess ? medResult.Data!.Name : "лекарство";

        var result = await _medicationService.DeleteAsync(medicationId, ct);
        
        if (result.IsSuccess)
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.MedicationDeleted, medName),
                replyMarkup: InlineKeyboards.MedicationsMenu,
                cancellationToken: ct);
            
            _logger.LogInformation("Medication deleted: {MedicationId}", medicationId);
        }
        else
        {
            await _botClient.SendMessage(
                chatId,
                string.Format(Messages.Error, result.Error),
                replyMarkup: InlineKeyboards.MedicationsMenu,
                cancellationToken: ct);
        }
    }
}

