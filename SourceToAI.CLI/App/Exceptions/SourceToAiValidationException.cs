namespace SourceToAI.CLI.App.Exceptions;

/// <summary>
/// Harte Validierungs- oder Eingabefehler (fehlendes Root, keine Projekte, Ausgabeordner nicht nutzbar).
/// </summary>
public sealed class SourceToAiValidationException : Exception
{
    public SourceToAiValidationException(string message)
        : base(message)
    {
    }

    public SourceToAiValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
