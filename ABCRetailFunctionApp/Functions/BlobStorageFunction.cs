/**************************************************************************************************************
 * Title: Integrating Azure Blob Storage with .NET
 * Author: YogeshKumar Hadiya
 * Date: 2024
 * Availability: https://medium.com/c-sharp-programming/integrating-azure-blob-storage-with-net-b4fc16dfde73
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: Azure Functions HTTP trigger
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejs-v4%2Cfunctionsv2&pivots=programming-language-csharp
 **************************************************************************************************************/
/**************************************************************************************************************
 * Title: HTTP Functions [CLDV6211]
 * Author: The IIE
 * Date: 2025
 **************************************************************************************************************/

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ABCRetailFunctionApp.Functions;

public class BlobStorageFunction
{
    private readonly ILogger<BlobStorageFunction> _logger;
    private readonly IConfiguration _configuration;

    public BlobStorageFunction(ILogger<BlobStorageFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration["AzureStorage"] ?? throw new InvalidOperationException("AzureStorage not configured.");

    [Function("WriteBlob")]
    public async Task<HttpResponseData> WriteBlob([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            if (!req.Headers.TryGetValues("filename", out var filenameValues) || !filenameValues.Any())
            {
                var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Filename header is required");
                return bad;
            }

            var filename = filenameValues.First();
            var ext = Path.GetExtension(filename).ToLower();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowed.Contains(ext))
            {
                var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Only image files are allowed");
                return bad;
            }

            var blobServiceClient = new BlobServiceClient(ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient("productimages");
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var uniqueFilename = $"{Guid.NewGuid()}{ext}";
            var blobClient = containerClient.GetBlobClient(uniqueFilename);

            var headers = new BlobHttpHeaders
            {
                ContentType = ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                }
            };

            await blobClient.UploadAsync(req.Body, new BlobUploadOptions { HttpHeaders = headers });

            // Generate SAS token for the uploaded blob
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = "productimages",
                BlobName = uniqueFilename,
                Resource = "b", // "b" for blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Start time (with 5 min buffer for clock skew)
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(1) // Expiry time (1 year from now)
            };

            // Set the permissions for the SAS token (Read only)
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Generate the SAS URI
            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
            string urlWithSas = sasUri.ToString();

            _logger.LogInformation("Blob uploaded successfully: {Filename}, URL: {Url}", uniqueFilename, urlWithSas);

            var ok = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await ok.WriteStringAsync($"{{\"message\":\"File uploaded\",\"filename\":\"{uniqueFilename}\",\"url\":\"{urlWithSas}\"}}");
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading blob");
            var error = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await error.WriteStringAsync($"Error: {ex.Message}");
            return error;
        }
    }
}