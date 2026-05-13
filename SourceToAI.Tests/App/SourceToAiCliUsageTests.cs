using SourceToAI.CLI.App.Cli;

namespace SourceToAI.Tests.App;

public class SourceToAiCliUsageTests
{
    [Fact]
    public void Usage_texts_mention_assembly_input()
    {
        Assert.Contains(".dll", SourceToAiCli.Usage.RootDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".exe", SourceToAiCli.Usage.RootDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Assembly", SourceToAiCli.Usage.UsageExampleAssembly, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".dll", SourceToAiCli.Usage.UsageLine, StringComparison.OrdinalIgnoreCase);
    }
}
