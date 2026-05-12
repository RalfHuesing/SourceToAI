using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Hash-Berechnung für Manifest-Spalte „Hash“: vollständiger MD5 als Hex,
/// gekürzt auf die ersten 8 Zeichen in Großbuchstaben (Konzept / Abgleich mit <see cref="SourceToAI.CLI.Services.Processing.HashService"/>).
/// </summary>
public static class AiFeedContentHash
{
    /// <summary>
    /// MD5 über <paramref name="contentUtf8"/>; Rückgabe = erste 8 Zeichen des 32-stelligen Hex-Strings (uppercase).
    /// </summary>
    public static string ComputeMd5HexPrefix8(ReadOnlySpan<byte> contentUtf8)
    {
        Span<byte> hashBytes = stackalloc byte[16];
        MD5.HashData(contentUtf8, hashBytes);
        return Convert.ToHexString(hashBytes)[..8];
    }

    /// <summary>
    /// Kodiert <paramref name="content"/> als UTF-8 und wendet <see cref="ComputeMd5HexPrefix8(ReadOnlySpan{byte})"/> an.
    /// </summary>
    public static string ComputeMd5HexPrefix8(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length == 0)
            return ComputeMd5HexPrefix8(ReadOnlySpan<byte>.Empty);

        var byteCount = Encoding.UTF8.GetByteCount(content);
        var rented = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Encoding.UTF8.GetBytes(content, rented);
            return ComputeMd5HexPrefix8(rented.AsSpan(0, byteCount));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
