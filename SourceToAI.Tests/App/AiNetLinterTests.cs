using System.Diagnostics;
using System.Text;

namespace SourceToAI.Tests.App;

public sealed class AiNetLinterTests
{
    [Fact]
    public void Run_AiNetLinter_And_Verify_No_Violations()
    {
        // 1. Find solution root directory
        var solutionDir = FindSolutionDirectory();
        
        // 2. Resolve paths
        var linterExe = Environment.GetEnvironmentVariable("AINETLINTER_EXE")
            ?? @"C:\Daten\AiNetLinter-win-x64\AiNetLinter.exe";

        Assert.True(File.Exists(linterExe), $"AiNetLinter executable not found at: {linterExe}. Please install it or set AINETLINTER_EXE environment variable.");

        var rulesPath = Path.Combine(solutionDir, "rules.json");
        Assert.True(File.Exists(rulesPath), $"rules.json not found at: {rulesPath}");

        var slnPath = Path.Combine(solutionDir, "SourceToAI.sln");
        Assert.True(File.Exists(slnPath), $"Solution file not found at: {slnPath}");

        var tempDir = Path.Combine(solutionDir, "SourceToAI.Tests", "Temp");
        Directory.CreateDirectory(tempDir);

        var reportPath = Path.Combine(tempDir, "ainetlinter-report.md");

        // 3. First run: Synchronize Cursor Rules
        var syncStartInfo = new ProcessStartInfo
        {
            FileName = linterExe,
            Arguments = $"-c \"{rulesPath}\" -p \"{slnPath}\" --sync-cursor-rules",
            WorkingDirectory = solutionDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using (var syncProcess = new Process())
        {
            syncProcess.StartInfo = syncStartInfo;
            syncProcess.Start();
            bool syncExited = syncProcess.WaitForExit(TimeSpan.FromMinutes(1));
            if (!syncExited)
            {
                syncProcess.Kill();
                throw new TimeoutException("AiNetLinter --sync-cursor-rules process timed out.");
            }
            if (syncProcess.ExitCode != 0)
            {
                var err = syncProcess.StandardError.ReadToEnd();
                var outStr = syncProcess.StandardOutput.ReadToEnd();
                Assert.Fail($"AiNetLinter --sync-cursor-rules failed with exit code {syncProcess.ExitCode}.\nError: {err}\nOutput: {outStr}");
            }
        }

        // 4. Second run: Start linter process
        var startInfo = new ProcessStartInfo
        {
            FileName = linterExe,
            Arguments = $"-c \"{rulesPath}\" -p \"{slnPath}\"",
            WorkingDirectory = solutionDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process();
        process.StartInfo = startInfo;

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait with a generous timeout (e.g. 2 minutes)
        bool exited = process.WaitForExit(TimeSpan.FromMinutes(2));
        if (!exited)
        {
            process.Kill();
            throw new TimeoutException("AiNetLinter process timed out.");
        }

        var reportContent = outputBuilder.ToString();
        var errorContent = errorBuilder.ToString();

        // 5. Dump the report to Temp
        File.WriteAllText(reportPath, reportContent, Encoding.UTF8);

        // 6. Assert exit code
        if (process.ExitCode != 0)
        {
            var message = $"""
            ================================================================================
            [UNIT TEST FAILED]: AiNetLinter found code style or architecture violations.
            Report File Path: {reportPath}
            ================================================================================
            Linter Error Output:
            {errorContent}
            ================================================================================
            Linter Report Output:
            {reportContent}
            ================================================================================
            """;
            
            Assert.Fail(message);
        }
    }

    private static string FindSolutionDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("SourceToAI.sln").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not find solution directory.");
    }
}
