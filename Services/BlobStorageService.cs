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
    private readonly Lazy<BlobServiceClient> _client;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration config)
    {
        var connectionString = config["AzureBlobStorage:ConnectionString"]!;
        _containerName = config["AzureBlobStorage:ContainerName"] ?? "edgetech-products";
        // ponytail: lazy so a missing/placeholder connection string only breaks uploads, not every controller that depends on this service
        _client = new Lazy<BlobServiceClient>(() => new BlobServiceClient(connectionString));
    }

    public async Task<string> UploadAsync(IFormFile file, string container, string folder)
    {
        var containerClient = _client.Value.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var isImage = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        if (isImage)
        {
            try
            {
                using var inStream = file.OpenReadStream();
                using var image = await SixLabors.ImageSharp.Image.LoadAsync(inStream);
                using var outStream = new MemoryStream();
                await image.SaveAsync(outStream, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder
                {
                    Quality = 85
                });
                outStream.Position = 0;

                var blobName = $"{folder}/{Guid.NewGuid()}.webp";
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.UploadAsync(outStream, new BlobHttpHeaders { ContentType = "image/webp" });
                return blobClient.Uri.ToString();
            }
            catch
            {
                // Fallback to original stream if ImageSharp cannot process
            }
        }

        var extension = Path.GetExtension(file.FileName);
        var fallbackBlobName = $"{folder}/{Guid.NewGuid()}{extension}";
        var fallbackBlobClient = containerClient.GetBlobClient(fallbackBlobName);

        using var stream = file.OpenReadStream();
        await fallbackBlobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return fallbackBlobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl)
    {
        var uri = new Uri(blobUrl);
        var blobName = string.Join("/", uri.Segments.Skip(2));
        var containerClient = _client.Value.GetBlobContainerClient(_containerName);
        await containerClient.DeleteBlobIfExistsAsync(blobName);
    }
}
