using Azure.Storage.Blobs;
using Azure.Storage.Sas;

public class BlobService
{
    private readonly BlobContainerClient _container;
    private readonly string _containerName;
    private readonly Uri _containerUri;

    public BlobService(IConfiguration config)
    {
        var connectionString = config["AzureBlob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "AzureBlob:ConnectionString is not configured. Provide it via environment variables or dotnet user-secrets.");

        _containerName = config["AzureBlob:ContainerName"]
            ?? throw new InvalidOperationException("Azure blob container name is not configured.");

        var service = new BlobServiceClient(connectionString);
        _container = service.GetBlobContainerClient(_containerName);
        _containerUri = _container.Uri;
    }

    public async Task<string> UploadProfilePicture(string userId, IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        var blobName = $"profile-picture/{userId}{extension}";

        var blobClient = _container.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);

        return blobName;
    }

    public async Task<string> UploadProjectPicture(Guid projectId, IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        var blobName = $"project-picture/{projectId}{extension}";

        var blobClient = _container.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);

        return blobName;
    }

    public async Task<string> UploadProjectImage(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        var blobName = $"project-picture/{Guid.NewGuid()}{extension}";

        var blobClient = _container.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);

        return blobName;
    }

    public string GetReadSasUrl(string blobName)
    {
        var blobClient = _container.GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
            throw new InvalidOperationException("Cannot generate SAS URI");

        var sas = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        sas.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sas).ToString();
    }

    public string? GetReadUrl(string? blobNameOrUrl)
    {
        if (string.IsNullOrWhiteSpace(blobNameOrUrl))
        {
            return null;
        }

        if (TryExtractBlobName(blobNameOrUrl, out var blobName))
        {
            return GetReadSasUrl(blobName);
        }

        return blobNameOrUrl;
    }

    public async Task Delete(string blobName)
    {
        var blobClient = _container.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }

    private bool TryExtractBlobName(string blobNameOrUrl, out string blobName)
    {
        blobName = string.Empty;

        if (!Uri.TryCreate(blobNameOrUrl, UriKind.Absolute, out var absoluteUri))
        {
            blobName = blobNameOrUrl.TrimStart('/');
            return blobName.Length > 0;
        }

        if (!string.Equals(absoluteUri.Scheme, _containerUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(absoluteUri.Host, _containerUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var containerPath = _containerUri.AbsolutePath.TrimEnd('/');
        var blobPathPrefix = containerPath + "/";

        if (!absoluteUri.AbsolutePath.StartsWith(blobPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        blobName = Uri.UnescapeDataString(absoluteUri.AbsolutePath[blobPathPrefix.Length..]);
        return blobName.Length > 0;
    }
}
