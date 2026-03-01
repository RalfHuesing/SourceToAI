using SourceToAI.CLI.Configuration;
using System;
using System.Threading.Tasks;

namespace SourceToAI.CLI.Services.Integration;

public class GoogleDriveSyncService : IPostExportTask
{
    private readonly GoogleDriveSyncSettings _settings;
    private readonly IGoogleDriveClient _googleDriveClient;

    public GoogleDriveSyncService(GoogleDriveSyncSettings settings, IGoogleDriveClient googleDriveClient)
    {
        _settings = settings;
        _googleDriveClient = googleDriveClient;
    }

    public async Task ExecuteAsync(string solutionName, string outputDirectory)
    {
        if (!_settings.Enabled)
        {
            Console.WriteLine("[INFO] Google Drive Sync ist deaktiviert. Überspringe Upload.");
            return;
        }

        Console.WriteLine($"[INFO] Google Drive Sync ist aktiviert. Starte Upload für '{solutionName}'...");

        try
        {
            var folderId = await _googleDriveClient.ReplaceSolutionFolderAsync(_settings.TargetFolder, solutionName, outputDirectory);
            Console.WriteLine($"[INFO] Google Drive Upload für '{solutionName}' erfolgreich abgeschlossen.");
            Console.WriteLine($"[INFO] Google Drive Folder ID: {folderId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FEHLER] Fehler beim Google Drive Upload: {ex.Message}");
        }
    }
}