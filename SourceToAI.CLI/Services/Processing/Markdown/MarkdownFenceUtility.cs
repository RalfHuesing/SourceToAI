namespace SourceToAI.CLI.Services.Processing.Markdown;

/// <summary>Hilfsfunktionen für sicheres Markdown-Code-Fencing.</summary>
public static class MarkdownFenceUtility
{
    /// <summary>
    /// Analysiert den Text auf längste Folge von Backticks und liefert eine sichere Fence-Länge
    /// (mindestens 4, sonst Maximalfolge + 1).
    /// </summary>
    public static int CalculateRequiredBackticks(string content)
    {
        int maxConsecutiveBackticks = 0;
        int currentConsecutive = 0;

        foreach (char c in content)
        {
            if (c == '`')
            {
                currentConsecutive++;
                if (currentConsecutive > maxConsecutiveBackticks)
                    maxConsecutiveBackticks = currentConsecutive;
            }
            else
            {
                currentConsecutive = 0;
            }
        }

        return Math.Max(4, maxConsecutiveBackticks + 1);
    }
}
