/**************************************************************************************************************
 * Title: Introduction to Azure File Share
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/storage/files/storage-files-introduction
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

using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ABCRetailFunctionApp.Functions;

public class FileStorageFunction
{
    private readonly ILogger<FileStorageFunction> _logger;
    private readonly IConfiguration _configuration;

    public FileStorageFunction(ILogger<FileStorageFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration["AzureStorage"] ?? throw new InvalidOperationException("AzureStorage not configured.");

    [Function("SendFile")]
    public async Task<HttpResponseData> SendFile([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
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
            var allowed = new[] { ".pdf", ".docx", ".doc", ".txt" };
            if (!allowed.Contains(ext))
            {
                var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Only document files are allowed");
                return bad;
            }

            var shareClient = new ShareServiceClient(ConnectionString).GetShareClient("contracts");
            await shareClient.CreateIfNotExistsAsync();

            var directory = shareClient.GetRootDirectoryClient();
            var fileClient = directory.GetFileClient(filename);

            using var stream = new MemoryStream();
            await req.Body.CopyToAsync(stream);
            stream.Position = 0;

            await fileClient.CreateAsync(stream.Length);
            await fileClient.UploadAsync(stream);

            var ok = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await ok.WriteStringAsync($"{{\"message\":\"File uploaded\",\"filename\":\"{filename}\"}}");
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            var error = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await error.WriteStringAsync($"Error: {ex.Message}");
            return error;
        }
    }
}
