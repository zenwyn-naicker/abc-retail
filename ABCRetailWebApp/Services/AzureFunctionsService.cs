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

using System.Net.Http;
using System.Text;
using System.Text.Json;
using ABCRetailWebApp.Models;

namespace ABCRetailWebApp.Services
{
    public class AzureFunctionsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AzureFunctionsService> _logger;

        public AzureFunctionsService(IHttpClientFactory httpClientFactory, ILogger<AzureFunctionsService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // Store Customer via Function
        public async Task<string> StoreCustomerViaFunctionAsync(CustomerEntity customer)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AzureFunctions");
                var json = JsonSerializer.Serialize(new
                {
                    Name = customer.Name,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Address = customer.Address
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/StoreCustomer", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Customer stored via function: {Result}", result);
                    return result;
                }
                else
                {
                    _logger.LogError("Failed to store customer via function: {Result}", result);
                    throw new Exception($"Function call failed: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling StoreCustomer function");
                throw;
            }
        }

        // Store Product via Function
        public async Task<string> StoreProductViaFunctionAsync(ProductEntity product)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AzureFunctions");
                var json = JsonSerializer.Serialize(new
                {
                    Name = product.Name,
                    Price = product.Price,
                    Stock = product.Stock,
                    Description = product.Description,
                    Category = product.Category,
                    ImageUrl = product.ImageUrl
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/StoreProduct", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Product stored via function: {Result}", result);
                    return result;
                }
                else
                {
                    _logger.LogError("Failed to store product via function: {Result}", result);
                    throw new Exception($"Function call failed: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling StoreProduct function");
                throw;
            }
        }

        // Store Order via Function
        public async Task<string> StoreOrderViaFunctionAsync(OrderEntity order)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AzureFunctions");
                var json = JsonSerializer.Serialize(new
                {
                    CustomerRowKey = order.CustomerRowKey,
                    CustomerName = order.CustomerName,
                    ProductRowKey = order.ProductRowKey,
                    ProductName = order.ProductName,
                    Quantity = order.Quantity,
                    UnitPrice = order.UnitPrice,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    OrderDate = order.OrderDate,
                    DeliveryAddress = order.DeliveryAddress,
                    Notes = order.Notes
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/StoreOrder", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Order stored via function: {Result}", result);
                    return result;
                }
                else
                {
                    _logger.LogError("Failed to store order via function: {Result}", result);
                    throw new Exception($"Function call failed: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling StoreOrder function");
                throw;
            }
        }

        // Write Blob via Function (images)
        public async Task<string> WriteBlobViaFunctionAsync(IFormFile file)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AzureFunctions");
                using var stream = file.OpenReadStream();
                var content = new StreamContent(stream);
                content.Headers.Add("filename", file.FileName);

                var response = await client.PostAsync("api/WriteBlob", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(result);
                    var url = jsonDoc.RootElement.GetProperty("url").GetString();

                    _logger.LogInformation("Blob uploaded via function: {Url}", url);
                    return url ?? string.Empty;
                }
                else
                {
                    _logger.LogError("Failed to upload blob via function: {Result}", result);
                    throw new Exception($"Function call failed: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling WriteBlob function");
                throw;
            }
        }

        // Read Queue Messages via Function
        public async Task<List<string>> ReadQueueMessagesViaFunctionAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AzureFunctions");
                var response = await client.GetAsync("api/ReadQueueMessages");
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(result);
                    var messagesArray = jsonDoc.RootElement.GetProperty("Messages");

                    var messages = new List<string>();
                    foreach (var msgElement in messagesArray.EnumerateArray())
                    {
                        var messageText = msgElement.GetProperty("MessageText").GetString();
                        if (!string.IsNullOrEmpty(messageText))
                        {
                            messages.Add(messageText);
                        }
                    }

                    _logger.LogInformation("Queue messages read via function: {Count} messages", messages.Count);
                    return messages;
                }
                else
                {
                    _logger.LogError("Failed to read queue messages via function: {Result}", result);
                    throw new Exception($"Function call failed: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ReadQueueMessages function");
                throw;
            }
        }

        // Write Queue Message via Function
        public async Task<string> WriteQueueMessageViaFunctionAsync(string message)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AzureFunctions");
                var json = JsonSerializer.Serialize(new { Message = message });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/WriteQueueMessage", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Queue message sent via function: {Result}", result);
                    return result;
                }
                else
                {
                    _logger.LogError("Failed to send queue message via function: {Result}", result);
                    throw new Exception($"Function call failed: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling WriteQueueMessage function");
                throw;
            }
        }

        // Send File via Function (documents)
        public async Task<string> SendFileViaFunctionAsync(IFormFile file)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AzureFunctions");
                using var stream = file.OpenReadStream();
                var content = new StreamContent(stream);
                content.Headers.Add("filename", file.FileName);

                var response = await client.PostAsync("api/SendFile", content);
                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("File uploaded via function: {Result}", result);
                    return result;
                }
                else
                {
                    _logger.LogError("Failed to upload file via function: {Result}", result);
                    throw new Exception($"Function call failed: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling SendFile function");
                throw;
            }
        }
    }
}