using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailWebApp.Controllers
{
    [Authorize(Roles = "Admin")] // Only admins can access this controller
    public class AdminOrdersController : Controller
    {
        private readonly IOrderService _orderService; 
        private readonly AzureFunctionsService _functionsService;
        private readonly ILogger<AdminOrdersController> _logger;

        public AdminOrdersController(
            IOrderService orderService,
            AzureFunctionsService functionsService,
            ILogger<AdminOrdersController> logger)
        {
            _orderService = orderService;
            _functionsService = functionsService;
            _logger = logger;
        }

        // GET: /AdminOrders/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all orders for admin");
                TempData["Error"] = "Failed to load orders.";
                return View(new List<ABCRetailWebApp.Models.CustomerOrderEntity>());
            }
        }

        // GET: /AdminOrders/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int orderId)
        {
            if (orderId <= 0)
            {
                return NotFound();
            }

            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);

                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details: OrderId={OrderId}", orderId);
                TempData["Error"] = "Failed to load order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /AdminOrders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            if (orderId <= 0 || string.IsNullOrEmpty(newStatus))
            {
                TempData["Error"] = "Invalid order or status.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                string oldStatus = order.OrderStatus;

                await _orderService.UpdateOrderStatusAsync(orderId, newStatus);

                // Log status update via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Order Status Updated: OrderId={orderId}, Customer={order.CustomerName}, Status: {oldStatus} → {newStatus}, UpdatedBy=Admin, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                );

                TempData["Success"] = $"Order #{orderId} status updated to {newStatus}!";
                return RedirectToAction(nameof(Details), new { orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status: OrderId={OrderId}", orderId);
                TempData["Error"] = "Failed to update order status.";
                return RedirectToAction(nameof(Details), new { orderId });
            }
        }

        // POST: /AdminOrders/MarkAsProcessed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsProcessed(int orderId)
        {
            return await UpdateStatus(orderId, "PROCESSED");
        }

        // GET: /AdminOrders/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                var statusCounts = await _orderService.GetOrderStatusCountsAsync();

                ViewBag.TotalOrders = orders.Count;
                ViewBag.TotalRevenue = orders.Sum(o => o.TotalAmount);
                ViewBag.PendingOrders = statusCounts.GetValueOrDefault("Pending", 0);
                ViewBag.ProcessingOrders = statusCounts.GetValueOrDefault("Processing", 0);
                ViewBag.ProcessedOrders = statusCounts.GetValueOrDefault("PROCESSED", 0);
                ViewBag.ShippedOrders = statusCounts.GetValueOrDefault("Shipped", 0);
                ViewBag.DeliveredOrders = statusCounts.GetValueOrDefault("Delivered", 0);
                ViewBag.CancelledOrders = statusCounts.GetValueOrDefault("Cancelled", 0);

                ViewBag.RecentOrders = orders.Take(10).ToList();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["Error"] = "Failed to load dashboard.";
                return View();
            }
        }

        // GET: /AdminOrders/FilterByStatus?status=Pending
        [HttpGet]
        public async Task<IActionResult> FilterByStatus(string status)
        {
            try
            {
                List<ABCRetailWebApp.Models.CustomerOrderEntity> orders;

                if (!string.IsNullOrEmpty(status))
                {
                    orders = await _orderService.GetOrdersByStatusAsync(status);
                    ViewBag.CurrentFilter = status;
                }
                else
                {
                    orders = await _orderService.GetAllOrdersAsync();
                }

                return View("Index", orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering orders by status: {Status}", status);
                TempData["Error"] = "Failed to filter orders.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}