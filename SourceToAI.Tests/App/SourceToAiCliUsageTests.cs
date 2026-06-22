using SourceToAI.CLI.App.Cli;

namespace SourceToAI.Tests.App;

public class SourceToAiCliUsageTests
{
    [Fact]
    public void Usage_texts_mention_assembly_input()
    {
        Assert.Contains(".dll", SourceToAiCli.RootDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".exe", SourceToAiCli.RootDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Assembly", SourceToAiCli.UsageExampleAssembly, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".dll", SourceToAiCli.UsageLine, StringComparison.OrdinalIgnoreCase);
    }
}
