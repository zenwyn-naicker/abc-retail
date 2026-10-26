/**************************************************************************************************************
 * Title: Introduction to Azure Table Storage
 * Author: Microsoft
 * Date: 2025
 * Availability: https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview
 **************************************************************************************************************/
using ABCRetailWebApp.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ABCRetailWebApp.Services
{
    public class AzureTableStorageService
    {
        private readonly TableServiceClient _tableServiceClient;

        public AzureTableStorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AzureStorage") ??
                                   throw new ArgumentNullException("Azure Storage connection string is required");

            _tableServiceClient = new TableServiceClient(connectionString);
        }

        public async Task<CustomerEntity> AddCustomerAsync(CustomerEntity customer)
        {
            var tableClient = _tableServiceClient.GetTableClient("customers");
            await tableClient.CreateIfNotExistsAsync();

            customer.PartitionKey = "Customer";
            customer.RowKey = Guid.NewGuid().ToString();
            customer.CreatedDate = DateTime.UtcNow;

            await tableClient.AddEntityAsync(customer);

            return customer;
        }

        public async Task<List<CustomerEntity>> GetCustomersAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("customers");
            await tableClient.CreateIfNotExistsAsync();

            var customers = new List<CustomerEntity>();
            await foreach (var customer in tableClient.QueryAsync<CustomerEntity>())
                customers.Add(customer);

            return customers;
        }

        public async Task<CustomerEntity?> GetCustomerAsync(string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient("customers");
            await tableClient.CreateIfNotExistsAsync();

            try
            {
                var response = await tableClient.GetEntityAsync<CustomerEntity>("Customer", rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<CustomerEntity> UpdateCustomerAsync(CustomerEntity customer)
        {
            var tableClient = _tableServiceClient.GetTableClient("customers");
            await tableClient.CreateIfNotExistsAsync();

            customer.PartitionKey ??= "Customer";
            await tableClient.UpdateEntityAsync(customer, customer.ETag, TableUpdateMode.Replace);

            return customer;
        }

        public async Task DeleteCustomerAsync(string rowKey, ETag eTag, string? name = null)
        {
            var tableClient = _tableServiceClient.GetTableClient("customers");
            await tableClient.CreateIfNotExistsAsync();

            await tableClient.DeleteEntityAsync("Customer", rowKey, eTag);
        }

        public async Task<ProductEntity> AddProductAsync(ProductEntity product)
        {
            var tableClient = _tableServiceClient.GetTableClient("products");
            await tableClient.CreateIfNotExistsAsync();

            product.PartitionKey = "Product";
            product.RowKey = Guid.NewGuid().ToString();
            product.CreatedDate = DateTime.UtcNow;

            await tableClient.AddEntityAsync(product);

            return product;
        }

        public async Task<List<ProductEntity>> GetProductsAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("products");
            await tableClient.CreateIfNotExistsAsync();

            var products = new List<ProductEntity>();
            await foreach (var product in tableClient.QueryAsync<ProductEntity>())
                products.Add(product);

            return products;
        }

        public async Task<ProductEntity?> GetProductAsync(string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient("products");
            await tableClient.CreateIfNotExistsAsync();

            try
            {
                var response = await tableClient.GetEntityAsync<ProductEntity>("Product", rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<ProductEntity> UpdateProductAsync(ProductEntity product)
        {
            var tableClient = _tableServiceClient.GetTableClient("products");
            await tableClient.CreateIfNotExistsAsync();

            product.PartitionKey = "Product";
            await tableClient.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);

            return product;
        }

        public async Task DeleteProductAsync(string rowKey, ETag eTag, string? name = null)
        {
            var tableClient = _tableServiceClient.GetTableClient("products");
            await tableClient.CreateIfNotExistsAsync();

            await tableClient.DeleteEntityAsync("Product", rowKey, eTag);
        }

        public async Task<OrderEntity> AddOrderAsync(OrderEntity order)
        {
            var tableClient = _tableServiceClient.GetTableClient("orders");
            await tableClient.CreateIfNotExistsAsync();

            order.PartitionKey = "Order";
            order.RowKey = Guid.NewGuid().ToString();
            order.OrderDate = DateTime.UtcNow;

            await tableClient.AddEntityAsync(order);

            return order;
        }

        public async Task<List<OrderEntity>> GetOrdersAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient("orders");
            await tableClient.CreateIfNotExistsAsync();

            var orders = new List<OrderEntity>();
            await foreach (var order in tableClient.QueryAsync<OrderEntity>())
                orders.Add(order);

            return orders.OrderByDescending(o => o.OrderDate).ToList();
        }

        public async Task<OrderEntity?> GetOrderAsync(string rowKey)
        {
            var tableClient = _tableServiceClient.GetTableClient("orders");
            await tableClient.CreateIfNotExistsAsync();

            try
            {
                var response = await tableClient.GetEntityAsync<OrderEntity>("Order", rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<OrderEntity> UpdateOrderAsync(OrderEntity order)
        {
            var tableClient = _tableServiceClient.GetTableClient("orders");
            await tableClient.CreateIfNotExistsAsync();

            order.PartitionKey ??= "Order";
            await tableClient.UpdateEntityAsync(order, order.ETag, TableUpdateMode.Replace);

            return order;
        }

        public async Task DeleteOrderAsync(string rowKey, ETag eTag)
        {
            var tableClient = _tableServiceClient.GetTableClient("orders");
            await tableClient.CreateIfNotExistsAsync();

            await tableClient.DeleteEntityAsync("Order", rowKey, eTag);
        }
    }
}