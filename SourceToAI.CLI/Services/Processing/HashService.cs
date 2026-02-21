using System.Security.Cryptography;
using System.Text;

namespace SourceToAI.CLI.Services.Processing;

public class HashService : IHashService
{
    public string ComputeShortHash(string content)
    {
        if (string.IsNullOrEmpty(content))
            return "D41D8CD9"; // Standard MD5 für leeren String (gekürzt)

        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = MD5.HashData(contentBytes);

        // Konvertiere in Hex und nimm die ersten 8 Zeichen für das Manifest
        return Convert.ToHexString(hashBytes)[..8];
    }
}