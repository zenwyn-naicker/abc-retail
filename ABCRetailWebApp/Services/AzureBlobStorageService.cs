/**************************************************************************************************************
 * Title: Integrating Azure Blob Storage with .NET
 * Author: YogeshKumar Hadiya
 * Date: 2024
 * Availability: https://medium.com/c-sharp-programming/integrating-azure-blob-storage-with-net-b4fc16dfde73
 **************************************************************************************************************/

using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace ABCRetailWebApp.Services
{
    public class AzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(
            IConfiguration configuration,
            ILogger<AzureBlobStorageService> logger)
        {
            var connectionString = configuration.GetConnectionString("AzureStorage") ??
                throw new ArgumentNullException("Azure Storage connection string is required");

            _blobServiceClient = new BlobServiceClient(connectionString);
            _logger = logger;
        }

        public async Task<string> UploadProductImageAsync(IFormFile file, string? customFileName = null)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Attempted to upload null or empty file");
                throw new ArgumentException("File is required");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                _logger.LogWarning("Invalid file extension: {Extension}", fileExtension);
                throw new ArgumentException("Only image files are allowed");
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient("productimages");

            try
            {
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                var fileName = customFileName ?? $"{Guid.NewGuid()}{fileExtension}";
                var blobClient = containerClient.GetBlobClient(fileName);

                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = GetContentType(fileExtension)
                };

                await blobClient.UploadAsync(file.OpenReadStream(), new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                });

                _logger.LogInformation("Successfully uploaded image: {FileName}", fileName);

                return GenerateLongTermSasUrl(blobClient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload product image: {FileName}", file.FileName);
                throw;
            }
        }

        public async Task<List<string>> GetProductImageUrlsAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("productimages");
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var imageUrls = new List<string>();
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                imageUrls.Add(GenerateLongTermSasUrl(blobClient));
            }
            return imageUrls;
        }

        public async Task<bool> DeleteProductImageAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                _logger.LogWarning("Attempted to delete image with empty filename");
                return false;
            }

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient("productimages");
                var blobClient = containerClient.GetBlobClient(fileName);

                var response = await blobClient.DeleteIfExistsAsync();

                if (response.Value)
                {
                    _logger.LogInformation("Successfully deleted image: {FileName}", fileName);
                }
                else
                {
                    _logger.LogWarning("Image not found for deletion: {FileName}", fileName);
                }

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete product image: {FileName}", fileName);
                return false;
            }
        }

        public string GetProductImageUrl(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            var containerClient = _blobServiceClient.GetBlobContainerClient("productimages");
            var blobClient = containerClient.GetBlobClient(fileName);
            return GenerateLongTermSasUrl(blobClient);
        }

        public async Task<string> UploadBlobAsync(string containerName, IFormFile file, string? customFileName = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required");

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var fileName = customFileName ?? $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobClient = containerClient.GetBlobClient(fileName);

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = GetContentType(fileExtension)
            };

            await blobClient.UploadAsync(file.OpenReadStream(), new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            return GenerateLongTermSasUrl(blobClient);
        }

        public async Task<Stream> DownloadBlobAsync(string containerName, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }

        public async Task<List<string>> GetBlobNamesAsync(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobNames = new List<string>();
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                blobNames.Add(blobItem.Name);
            }
            return blobNames;
        }

        public async Task<bool> DeleteBlobAsync(string containerName, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var response = await blobClient.DeleteIfExistsAsync();
            return response.Value;
        }

        public async Task<bool> BlobExistsAsync(string containerName, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var response = await blobClient.ExistsAsync();
            return response.Value;
        }

        private string GenerateLongTermSasUrl(BlobClient blobClient)
        {
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddDays(30)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();
                _logger.LogDebug("Generated SAS URL for {BlobName}, expires: {ExpiresOn}",
                    blobClient.Name, sasBuilder.ExpiresOn);

                return sasUrl;
            }
            else
            {
                _logger.LogWarning("Cannot generate SAS URI for blob: {BlobName}", blobClient.Name);
                return blobClient.Uri.ToString();
            }
        }

        private static string GetContentType(string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }

        public static string ExtractFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return Path.GetFileName(uri.LocalPath);
            }
            catch
            {
                var lastSlash = url.LastIndexOf('/');
                if (lastSlash >= 0 && lastSlash < url.Length - 1)
                {
                    var filename = url.Substring(lastSlash + 1);
                    var questionMark = filename.IndexOf('?');
                    if (questionMark >= 0)
                        filename = filename.Substring(0, questionMark);
                    return filename;
                }
                return string.Empty;
            }
        }
    }
}