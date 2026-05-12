namespace SourceToAI.CLI.Services.IO;

public sealed class PhysicalFileReader : IFileReader
{
    public string ReadAllText(string path) => File.ReadAllText(path);
}
