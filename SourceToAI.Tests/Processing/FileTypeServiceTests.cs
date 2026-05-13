using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.Tests.Processing;

public class FileTypeServiceTests
{
    [Theory]
    [InlineData(".cs", "Code", "csharp")]
    [InlineData(".CS", "Code", "csharp")]
    [InlineData(".md", "Doc", "markdown")]
    [InlineData(".mdc", "Doc", "markdown")]
    [InlineData(".yaml", "Config", "yaml")]
    [InlineData(".yml", "Config", "yaml")]
    [InlineData(".json", "Config", "json")]
    [InlineData(".unknownext", "Unknown", "text")]
    public void GetFileTypeAndLanguage_maps_known_extensions(string extension, string expectedType, string expectedLanguage)
    {
        var (type, language) = FileTypeService.GetFileTypeAndLanguage(extension);

        Assert.Equal(expectedType, type);
        Assert.Equal(expectedLanguage, language);
    }
}
