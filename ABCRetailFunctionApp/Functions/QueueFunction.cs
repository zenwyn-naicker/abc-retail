/**************************************************************************************************************
 * Title: Introduction to Azure Queue Storage
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/storage/queues/storage-queues-introduction
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

using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using ABCRetailFunctionApp.Models;

namespace ABCRetailFunctionApp.Functions;

public class QueueFunction
{
    private readonly ILogger<QueueFunction> _logger;
    private readonly IConfiguration _configuration;

    public QueueFunction(ILogger<QueueFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration["AzureStorage"] ?? throw new InvalidOperationException("AzureStorage not configured.");

    [Function("ReadQueueMessages")]
    public async Task<HttpResponseData> ReadQueueMessages(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        try
        {
            var queue = new QueueClient(ConnectionString, "notifications");
            await queue.CreateIfNotExistsAsync();

            // Peek messages without removing them from the queue
            var messages = await queue.PeekMessagesAsync(maxMessages: 32);
            var result = messages.Value.Select(m => new
            {
                MessageId = m.MessageId,
                MessageText = m.MessageText,
                InsertedOn = m.InsertedOn
            }).ToList();

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteStringAsync(JsonSerializer.Serialize(new
            {
                Count = result.Count,
                Messages = result
            }));
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading queue messages");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync($"Error: {ex.Message}");
            return error;
        }
    }

    [Function("WriteQueueMessage")]
    public async Task<HttpResponseData> WriteQueueMessage(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var messageModel = JsonSerializer.Deserialize<QueueMessageModel>(body);

            if (messageModel == null || string.IsNullOrEmpty(messageModel.Message))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Message is required");
                return bad;
            }

            var queue = new QueueClient(ConnectionString, "notifications");
            await queue.CreateIfNotExistsAsync();

            var timestamp = DateTime.UtcNow.AddHours(2).ToString("yyyy-MM-dd HH:mm:ss");
            var fullMessage = $"{timestamp} | {messageModel.Message}";

            var response = await queue.SendMessageAsync(fullMessage);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteStringAsync(JsonSerializer.Serialize(new
            {
                message = "Message written successfully",
                messageId = response.Value.MessageId,
                queuedMessage = fullMessage
            }));
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing queue message");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync($"Error: {ex.Message}");
            return error;
        }
    }
}