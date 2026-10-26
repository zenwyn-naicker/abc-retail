using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ABCRetailWebApp.Controllers
{
    [Authorize] // Customers must be logged in to view their orders
    public class CustomerOrdersController : Controller
    {
        private readonly IOrderService _orderService; 
        private readonly ILogger<CustomerOrdersController> _logger;

        public CustomerOrdersController(
            IOrderService orderService,
            ILogger<CustomerOrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        // GET: /CustomerOrders/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var orders = await _orderService.GetOrdersAsync(userId);
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer orders");
                TempData["Error"] = "Failed to load your orders.";
                return View(new List<ABCRetailWebApp.Models.CustomerOrderEntity>());
            }
        }

        // GET: /CustomerOrders/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int orderId)
        {
            if (orderId <= 0)
            {
                return NotFound();
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var order = await _orderService.GetOrderByIdAsync(orderId);

                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (order.UserId != userId)
                {
                    TempData["Error"] = "You do not have permission to view this order.";
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

        // GET: /CustomerOrders/TrackOrder/5
        [HttpGet]
        public async Task<IActionResult> TrackOrder(int orderId)
        {
            if (orderId <= 0)
            {
                return NotFound();
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var order = await _orderService.GetOrderByIdAsync(orderId);

                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (order.UserId != userId)
                {
                    TempData["Error"] = "You do not have permission to track this order.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.OrderId = order.OrderId;
                ViewBag.OrderDate = order.OrderDate;
                ViewBag.OrderStatus = order.OrderStatus;
                ViewBag.EstimatedDelivery = order.OrderDate.AddDays(7);

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking order: OrderId={OrderId}", orderId);
                TempData["Error"] = "Failed to track order.";
                return RedirectToAction(nameof(Index));
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}