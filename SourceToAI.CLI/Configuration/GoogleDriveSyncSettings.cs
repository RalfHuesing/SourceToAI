namespace SourceToAI.CLI.Configuration;

public class GoogleDriveSyncSettings
{
    public bool Enabled { get; set; } = false;
    public string TargetFolder { get; set; } = "Code";
}