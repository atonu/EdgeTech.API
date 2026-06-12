using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EdgeTech.API.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(IFormFile file, string container, string folder);
    Task DeleteAsync(string blobUrl);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration config)
    {
        var connectionString = config["AzureBlobStorage:ConnectionString"]!;
        _containerName = config["AzureBlobStorage:ContainerName"] ?? "edgetech-products";
        _client = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadAsync(IFormFile file, string container, string folder)
    {
        var containerClient = _client.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var extension = Path.GetExtension(file.FileName);
        var blobName = $"{folder}/{Guid.NewGuid()}{extension}";
        var blobClient = containerClient.GetBlobClient(blobName);

        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl)
    {
        var uri = new Uri(blobUrl);
        var blobName = string.Join("/", uri.Segments.Skip(2));
        var containerClient = _client.GetBlobContainerClient(_containerName);
        await containerClient.DeleteBlobIfExistsAsync(blobName);
    }
}
