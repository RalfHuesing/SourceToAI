using SourceToAI.CLI.App.Cli;

namespace SourceToAI.Tests.App;

public sealed class ExportInputPathValidationTests
{
    [Fact]
    public void GetValidationError_ExistingDirectory_ReturnsNull()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            Assert.Null(ExportInputPathValidation.GetValidationError(temp));
        }
        finally
        {
            Directory.Delete(temp);
        }
    }

    [Fact]
    public void GetValidationError_ThisTestAssemblyDll_ReturnsNull()
    {
        var dllPath = typeof(ExportInputPathValidationTests).Assembly.Location;
        Assert.True(File.Exists(dllPath), "Erwartet eine .dll unter Output.");
        Assert.Null(ExportInputPathValidation.GetValidationError(dllPath));
    }

    [Fact]
    public void GetValidationError_NonExistentPath_ReturnsError()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing");
        var err = ExportInputPathValidation.GetValidationError(path);
        Assert.NotNull(err);
        Assert.Contains("nicht vorhanden", err, StringComparison.Ordinal);
        Assert.Contains(path, err, StringComparison.Ordinal);
    }

    [Fact]
    public void GetValidationError_ExistingNonAssemblyFile_ReturnsError()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(tempFile, "x");
        try
        {
            var err = ExportInputPathValidation.GetValidationError(tempFile);
            Assert.NotNull(err);
            Assert.Contains("nicht unterstützter Dateityp", err, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetValidationError_ExeExtensionAcceptedWhenFileExists()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var exePath = Path.Combine(dir, "stub.exe");
        File.WriteAllText(exePath, string.Empty);
        try
        {
            Assert.Null(ExportInputPathValidation.GetValidationError(exePath));
        }
        finally
        {
            File.Delete(exePath);
            Directory.Delete(dir);
        }
    }
}
