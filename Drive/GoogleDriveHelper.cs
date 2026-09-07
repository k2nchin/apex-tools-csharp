using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace ApexTools.Drive;

public class GoogleDriveHelper
{
    private DriveService? _service;
    private readonly string _credentialsPath;
    private readonly string[] _scopes = { DriveService.Scope.DriveFile };

    public bool IsConnected => _service != null;

    public GoogleDriveHelper(string credentialsPath = "credentials.json")
    {
        _credentialsPath = credentialsPath;
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            if (!File.Exists(_credentialsPath)) return false;

            using var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read);
            var credential = await GoogleCredential.FromStreamAsync(stream, CancellationToken.None);
            credential = credential.CreateScoped(_scopes);

            _service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Apex Tools"
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<(string id, string name)>> ListFoldersAsync()
    {
        var folders = new List<(string, string)>();
        if (_service == null) return folders;

        try
        {
            var request = _service.Files.List();
            request.Q = "mimeType='application/vnd.google-apps.folder' and trashed=false";
            request.Fields = "files(id, name)";
            request.Spaces = "drive";
            request.OrderBy = "name";

            var result = await request.ExecuteAsync();
            foreach (var file in result.Files ?? Enumerable.Empty<Google.Apis.Drive.v3.Data.File>())
            {
                folders.Add((file.Id, file.Name));
            }
        }
        catch { }
        return folders;
    }

    public async Task<string?> CreateFolderAsync(string name, string parentId = "")
    {
        if (_service == null) return null;

        try
        {
            var folderMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = name,
                MimeType = "application/vnd.google-apps.folder"
            };
            if (!string.IsNullOrEmpty(parentId))
                folderMetadata.Parents = new List<string> { parentId };

            var request = _service.Files.Create(folderMetadata);
            request.Fields = "id";
            var folder = await request.ExecuteAsync();
            return folder.Id;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UploadFileAsync(string filePath, string folderId)
    {
        if (_service == null || !File.Exists(filePath)) return false;

        try
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = Path.GetFileName(filePath),
                Parents = new List<string> { folderId }
            };

            using var stream = new FileStream(filePath, FileMode.Open);
            var request = _service.Files.Create(fileMetadata, stream,
                "application/octet-stream");
            request.Fields = "id";
            await request.UploadAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
