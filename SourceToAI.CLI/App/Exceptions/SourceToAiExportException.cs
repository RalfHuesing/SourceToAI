namespace SourceToAI.CLI.App.Exceptions;

/// <summary>
/// Fehler beim Multi-View-Export; bei Parallelität typischerweise mit <see cref="AggregateException"/> als Inner.
/// </summary>
public sealed class SourceToAiExportException : Exception
{
    public SourceToAiExportException(string message)
        : base(message)
    {
    }

    public SourceToAiExportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
