using System.Text;

namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Definition der Manifest-Spalte „Size“: UTF-8-Byteanzahl der <strong>exportierten</strong> Zeichenkette
/// (dieselbe Zeichenkette, die später im CONTENT-Bereich erscheint), nicht die Rohdateigröße auf der Platte.
/// </summary>
public static class AiFeedExportedUtf8Size
{
    public static long OfExportedString(string exportedContent)
    {
        ArgumentNullException.ThrowIfNull(exportedContent);
        return Encoding.UTF8.GetByteCount(exportedContent);
    }
}
