using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SourceToAI.CLI.Services.Integration;

public class GoogleDriveClient : IGoogleDriveClient
{
    private readonly string[] Scopes = { DriveService.Scope.DriveFile };
    private readonly string ApplicationName = "SourceToAI Sync";
    private readonly string CredentialsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GoogleDrive.CodeSync");

    public async Task ReplaceSolutionFolderAsync(string targetRootFolder, string solutionName, string localDirectory)
    {
        var service = await GetDriveServiceAsync();

        // 1. Root-Ordner finden oder erstellen
        var rootFolderId = await GetOrCreateFolderAsync(service, targetRootFolder, "root");

        // 2. Solution-Ordner finden und ggf. löschen
        var existingSolutionFolderId = await GetFolderIdAsync(service, solutionName, rootFolderId);
        if (existingSolutionFolderId != null)
        {
            await service.Files.Delete(existingSolutionFolderId).ExecuteAsync();
        }

        // 3. Solution-Ordner neu erstellen
        var newSolutionFolderId = await CreateFolderAsync(service, solutionName, rootFolderId);

        // 4. Dateien hochladen
        var files = Directory.GetFiles(localDirectory, "*.md");
        foreach (var file in files)
        {
            await UploadFileAsync(service, file, newSolutionFolderId);
        }
    }

    private async Task<DriveService> GetDriveServiceAsync()
    {
        UserCredential credential;

        if (!Directory.Exists(CredentialsPath))
        {
            Directory.CreateDirectory(CredentialsPath);
        }

        var secretFiles = Directory.GetFiles(CredentialsPath, "client_secret_*.json");
        if (secretFiles.Length == 0)
        {
            throw new FileNotFoundException($"Kein client_secret_*.json gefunden in '{CredentialsPath}'. Bitte legen Sie die Datei dort ab.");
        }

        var clientSecretPath = secretFiles[0];
        using (var stream = new FileStream(clientSecretPath, FileMode.Open, FileAccess.Read))
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(CredentialsPath, true));
        }

        return new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });
    }

    private async Task<string?> GetFolderIdAsync(DriveService service, string folderName, string parentId)
    {
        var request = service.Files.List();
        request.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and '{parentId}' in parents and trashed=false";
        request.Spaces = "drive";
        request.Fields = "files(id, name)";

        var result = await request.ExecuteAsync();
        return result.Files.FirstOrDefault()?.Id;
    }

    private async Task<string> CreateFolderAsync(DriveService service, string folderName, string parentId)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { parentId }
        };
        var request = service.Files.Create(fileMetadata);
        request.Fields = "id";
        var folder = await request.ExecuteAsync();
        return folder.Id;
    }

    private async Task<string> GetOrCreateFolderAsync(DriveService service, string folderName, string parentId)
    {
        var folderId = await GetFolderIdAsync(service, folderName, parentId);
        if (folderId == null)
        {
            folderId = await CreateFolderAsync(service, folderName, parentId);
        }
        return folderId;
    }

    private async Task UploadFileAsync(DriveService service, string filePath, string parentId)
    {
        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = Path.GetFileName(filePath),
            Parents = new List<string> { parentId }
        };

        FilesResource.CreateMediaUpload request;
        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            request = service.Files.Create(fileMetadata, stream, "text/markdown");
            request.Fields = "id";
            await request.UploadAsync();
        }
    }
}