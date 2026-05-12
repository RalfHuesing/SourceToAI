namespace SourceToAI.CLI.Models;

/// <summary>
/// Kapselt das Ergebnis einer Operation, um Exceptions für den normalen Kontrollfluss zu vermeiden.
/// </summary>
public class ExtractionResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }
    /// <summary>
    /// Optionale Hinweise (z. B. übersprungene Pfade beim Scan), nur bei erfolgreichen Ergebnissen gesetzt.
    /// </summary>
    public IReadOnlyList<string>? Warnings { get; }

    private ExtractionResult(bool isSuccess, T? value, string? errorMessage, IReadOnlyList<string>? warnings)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        Warnings = warnings;
    }

    public static ExtractionResult<T> Success(T value, IReadOnlyList<string>? warnings = null) =>
        new(true, value, null, warnings);

    public static ExtractionResult<T> Failure(string errorMessage) =>
        new(false, default, errorMessage, null);
}