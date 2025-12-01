namespace MedicationAssist.Domain.Common;

/// <summary>
/// Базовое исключение домена
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

