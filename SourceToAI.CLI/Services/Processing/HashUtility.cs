using SourceToAI.CLI.Services.Export.AiFeed;

namespace SourceToAI.CLI.Services.Processing;

public static class HashUtility
{
    /// <summary>
    /// Kurzer MD5-Prefix (8 Hex-Zeichen, UTF-8); null/leer wie MD5 des leeren Strings.
    /// </summary>
    public static string ComputeShortHash(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return "D41D8CD9";

        return AiFeedContentHash.ComputeMd5HexPrefix8(content);
    }
}
