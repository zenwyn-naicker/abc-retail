using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Azure;

namespace ABCRetailWebApp.Controllers
{
    public class OrderController : Controller
    {
        private readonly AzureTableStorageService _tableStorageService;
        private readonly AzureFunctionsService _functionsService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            AzureTableStorageService tableStorageService,
            AzureFunctionsService functionsService,
            ILogger<OrderController> logger)
        {
            _tableStorageService = tableStorageService;
            _functionsService = functionsService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _tableStorageService.GetOrdersAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return NotFound();
            var order = await _tableStorageService.GetOrderAsync(rowKey);
            if (order == null) return NotFound();
            return View(order);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropdownLists();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderEntity order)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownLists();
                return View(order);
            }

            try
            {
                order.PartitionKey = "Order";
                order.RowKey = Guid.NewGuid().ToString();  

                var customers = await _tableStorageService.GetCustomersAsync();
                var selectedCustomer = customers.FirstOrDefault(c => c.RowKey == order.CustomerRowKey);
                if (selectedCustomer != null)
                {
                    order.CustomerName = selectedCustomer.Name;
                }

                var product = await _tableStorageService.GetProductAsync(order.ProductRowKey);
                if (product != null)
                {
                    order.ProductName = product.Name;
                    order.UnitPrice = product.Price;
                }

                // Use Azure Function to store order
                var functionResult = await _functionsService.StoreOrderViaFunctionAsync(order);

                // Log order creation via function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Order Created: Customer={order.CustomerName}, Product={order.ProductName}, Qty={order.Quantity}, Total=R{(order.UnitPrice * order.Quantity):F2}, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                );

                if (product != null)  
                {
                    var oldStock = product.Stock;
                    product.Stock -= order.Quantity;
                    await _tableStorageService.UpdateProductAsync(product);

                    // Log stock update via function
                    await _functionsService.WriteQueueMessageViaFunctionAsync(
                        $"Product Stock Updated: Product={product.Name}, Stock: {oldStock} → {product.Stock}, RelatedOrder={order.RowKey.Substring(0, 8)}..., Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                    );
                }

                TempData["Success"] = "Order created successfully via function!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order via function");
                TempData["Error"] = "Failed to create order.";
                await PopulateDropdownLists();
                return View(order);
            }
        }

        public async Task<IActionResult> Edit(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return NotFound();
            var order = await _tableStorageService.GetOrderAsync(rowKey);
            if (order == null) return NotFound();

            await PopulateDropdownLists();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string rowKey, OrderEntity order)
        {
            if (rowKey != order.RowKey) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateDropdownLists();
                return View(order);
            }

            try
            {
                var existingOrder = await _tableStorageService.GetOrderAsync(rowKey);
                if (existingOrder == null) return NotFound();

                var product = await _tableStorageService.GetProductAsync(order.ProductRowKey);
                if (product != null)
                {
                    int quantityDiff = order.Quantity - existingOrder.Quantity;
                    if (quantityDiff != 0)
                    {
                        var oldStock = product.Stock;
                        product.Stock -= quantityDiff;
                        await _tableStorageService.UpdateProductAsync(product);

                        // Log stock update via function
                        await _functionsService.WriteQueueMessageViaFunctionAsync(
                            $"Product Stock Updated: Product={product.Name}, Stock: {oldStock} → {product.Stock}, RelatedOrder={existingOrder.RowKey.Substring(0, 8)}..., QtyChange={quantityDiff:+#;-#;0}, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                        );
                    }
                }

                var changes = new List<string>();
                if (existingOrder.Quantity != order.Quantity)
                    changes.Add($"Qty: {existingOrder.Quantity} → {order.Quantity}");
                if (existingOrder.Status != order.Status)
                    changes.Add($"Status: {existingOrder.Status} → {order.Status}");

                existingOrder.ProductName = order.ProductName;
                existingOrder.CustomerName = order.CustomerName;
                existingOrder.Quantity = order.Quantity;
                existingOrder.Status = order.Status;
                existingOrder.DeliveryAddress = order.DeliveryAddress;
                existingOrder.Notes = order.Notes;

                await _tableStorageService.UpdateOrderAsync(existingOrder);

                if (changes.Any())
                {
                    // Log order update via function
                    await _functionsService.WriteQueueMessageViaFunctionAsync(
                        $"Order Updated: Customer={existingOrder.CustomerName}, Product={existingOrder.ProductName}, Changes={string.Join(", ", changes)}, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                    );
                }

                TempData["Success"] = "Order updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order: {RowKey}", rowKey);
                TempData["Error"] = "Failed to update order.";
                await PopulateDropdownLists();
                return View(order);
            }
        }

        public async Task<IActionResult> Delete(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey)) return NotFound();
            var order = await _tableStorageService.GetOrderAsync(rowKey);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string rowKey)
        {
            try
            {
                var order = await _tableStorageService.GetOrderAsync(rowKey);
                if (order != null)
                {
                    var product = await _tableStorageService.GetProductAsync(order.ProductRowKey);
                    if (product != null)
                    {
                        var oldStock = product.Stock;
                        product.Stock += order.Quantity;
                        await _tableStorageService.UpdateProductAsync(product);

                        // Log stock restoration via function
                        await _functionsService.WriteQueueMessageViaFunctionAsync(
                            $"Product Stock Restored: Product={product.Name}, Stock: {oldStock} → {product.Stock}, RelatedOrder={order.RowKey.Substring(0, 8)}..., QtyRestored={order.Quantity}, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                        );
                    }

                    await _tableStorageService.DeleteOrderAsync(rowKey, order.ETag);

                    // Log order deletion via function
                    await _functionsService.WriteQueueMessageViaFunctionAsync(
                        $"Order Deleted: Customer={order.CustomerName}, Product={order.ProductName}, Qty={order.Quantity}, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                    );
                }

                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order: {RowKey}", rowKey);
                TempData["Error"] = "Failed to delete order.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownLists()
        {
            try
            {
                var customers = await _tableStorageService.GetCustomersAsync();
                ViewBag.Customers = customers.Select(c => new SelectListItem
                {
                    Value = c.RowKey,
                    Text = $"{c.Name} - {c.Email}"
                }).ToList();

                var products = await _tableStorageService.GetProductsAsync();
                ViewBag.Products = products
                    .Where(p => p.Stock > 0)
                    .Select(p => new SelectListItem
                    {
                        Value = p.RowKey,
                        Text = $"{p.Name} - R{p.Price:F2} (Stock: {p.Stock})"
                    }).ToList();

                ViewBag.OrderStatuses = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Pending", Text = "Pending" },
                    new SelectListItem { Value = "Processing", Text = "Processing" },
                    new SelectListItem { Value = "Shipped", Text = "Shipped" },
                    new SelectListItem { Value = "Delivered", Text = "Delivered" },
                    new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating dropdown lists");
                ViewBag.Customers = new List<SelectListItem>();
                ViewBag.Products = new List<SelectListItem>();
                ViewBag.OrderStatuses = new List<SelectListItem>();
            }
        }
    }
}
