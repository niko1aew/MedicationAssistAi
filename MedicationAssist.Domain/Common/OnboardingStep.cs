namespace MedicationAssist.Domain.Common;

/// <summary>
/// Шаги онбординга для новых пользователей
/// </summary>
public enum OnboardingStep
{
    /// <summary>
    /// Приветственный экран
    /// </summary>
    Welcome = 0,

    /// <summary>
    /// Переход на страницу лекарств
    /// </summary>
    NavigateToMedications = 1,

    /// <summary>
    /// Добавление лекарства
    /// </summary>
    AddMedication = 2,

    /// <summary>
    /// Настройка напоминания
    /// </summary>
    AddReminder = 3,

    /// <summary>
    /// Онбординг завершен
    /// </summary>
    Completed = 4
}
