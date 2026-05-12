namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Minimales Escaping für YAML-Werte in doppelten Anführungszeichen (YAGNI: kein vollständiger YAML-Builder).
/// </summary>
public static class YamlDoubleQuotedEscaping
{
    /// <summary>Escaping für YAML-Doppelquoted-Skalare (Sonderzeichen, Zeilenumbrüche, Anführungszeichen).</summary>
    public static string EscapeYamlDoubleQuoted(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }
}
