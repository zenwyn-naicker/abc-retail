/**************************************************************************************************************
 * Title: Introduction to Azure Table Storage
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview
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

using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using ABCRetailFunctionApp.Models;

namespace ABCRetailFunctionApp.Functions;

public class TableStorageFunction
{
    private readonly ILogger<TableStorageFunction> _logger;
    private readonly IConfiguration _configuration;

    public TableStorageFunction(ILogger<TableStorageFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration["AzureStorage"] ?? throw new InvalidOperationException("AzureStorage not configured.");

    [Function("StoreCustomer")]
    public async Task<HttpResponseData> StoreCustomer([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var customer = JsonSerializer.Deserialize<CustomerModel>(body);

            if (customer == null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid customer data");
                return bad;
            }

            var tableClient = new TableServiceClient(ConnectionString).GetTableClient("customers");
            await tableClient.CreateIfNotExistsAsync();

            var entity = new TableEntity("Customer", Guid.NewGuid().ToString())
            {
                ["Name"] = customer.Name,
                ["Email"] = customer.Email,
                ["Phone"] = customer.Phone ?? "",
                ["Address"] = customer.Address ?? "",
                ["CreatedDate"] = DateTime.UtcNow
            };

            await tableClient.AddEntityAsync(entity);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteStringAsync(JsonSerializer.Serialize(new { Message = "Customer stored", CustomerId = entity.RowKey }));
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing customer");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync($"Error: {ex.Message}");
            return error;
        }
    }

    [Function("StoreProduct")]
    public async Task<HttpResponseData> StoreProduct([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var product = JsonSerializer.Deserialize<ProductModel>(body);

            if (product == null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid product data");
                return bad;
            }

            var tableClient = new TableServiceClient(ConnectionString).GetTableClient("products");
            await tableClient.CreateIfNotExistsAsync();

            var entity = new TableEntity("Product", Guid.NewGuid().ToString())
            {
                ["Name"] = product.Name,
                ["Price"] = product.Price,
                ["Stock"] = product.Stock,
                ["Description"] = product.Description ?? "",
                ["Category"] = product.Category ?? "",
                ["ImageUrl"] = product.ImageUrl ?? "",
                ["CreatedDate"] = DateTime.UtcNow
            };

            await tableClient.AddEntityAsync(entity);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteStringAsync(JsonSerializer.Serialize(new { Message = "Product stored", ProductId = entity.RowKey }));
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing product");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync($"Error: {ex.Message}");
            return error;
        }
    }

    [Function("StoreOrder")]
    public async Task<HttpResponseData> StoreOrder(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var order = JsonSerializer.Deserialize<OrderModel>(body);

            if (order == null || string.IsNullOrWhiteSpace(order.CustomerRowKey) || string.IsNullOrWhiteSpace(order.ProductRowKey))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid order data. CustomerRowKey and ProductRowKey are required.");
                return bad;
            }

            string rowKey = !string.IsNullOrWhiteSpace(order.RowKey)
                ? order.RowKey
                : Guid.NewGuid().ToString();

            var tableClient = new TableServiceClient(ConnectionString).GetTableClient("orders");
            await tableClient.CreateIfNotExistsAsync();

            var entity = new TableEntity("Order", rowKey)  
            {
                ["CustomerRowKey"] = order.CustomerRowKey,
                ["CustomerName"] = order.CustomerName,
                ["ProductRowKey"] = order.ProductRowKey,
                ["ProductName"] = order.ProductName,
                ["Quantity"] = order.Quantity,
                ["UnitPrice"] = order.UnitPrice,
                ["TotalAmount"] = order.TotalAmount,
                ["Status"] = order.Status,
                ["OrderDate"] = order.OrderDate,
                ["DeliveryAddress"] = order.DeliveryAddress ?? "",
                ["Notes"] = order.Notes ?? ""
            };

            await tableClient.AddEntityAsync(entity);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteStringAsync(JsonSerializer.Serialize(new
            {
                Message = "Order stored successfully",
                OrderId = entity.RowKey
            }));
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing order");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync($"Error: {ex.Message}");
            return error;
        }
    }
}
