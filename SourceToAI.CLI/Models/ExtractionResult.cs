namespace SourceToAI.CLI.Models;

/// <summary>
/// Kapselt das Ergebnis einer Operation, um Exceptions für den normalen Kontrollfluss zu vermeiden.
/// </summary>
public class ExtractionResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }

    private ExtractionResult(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    public static ExtractionResult<T> Success(T value) => new(true, value, null);

    public static ExtractionResult<T> Failure(string errorMessage) => new(false, default, errorMessage);
}