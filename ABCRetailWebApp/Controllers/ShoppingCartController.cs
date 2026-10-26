using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ABCRetailWebApp.Controllers
{
    [Authorize] // Require login for all cart actions
    public class ShoppingCartController : Controller
    {
        private readonly IShoppingCartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly AzureFunctionsService _functionsService;
        private readonly ILogger<ShoppingCartController> _logger;

        public ShoppingCartController(
            IShoppingCartService cartService,
            IOrderService orderService,
            IUserService userService,
            AzureFunctionsService functionsService,
            ILogger<ShoppingCartController> logger)
        {
            _cartService = cartService;
            _orderService = orderService;
            _userService = userService;
            _functionsService = functionsService;
            _logger = logger;
        }

        // GET: /ShoppingCart/Index
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

                var cartItems = await _cartService.GetCartItemsAsync(userId);

                ViewBag.CartTotal = cartItems.Sum(item => item.TotalPrice);
                ViewBag.CartCount = cartItems.Count;

                return View(cartItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shopping cart for user");
                TempData["Error"] = "Failed to load shopping cart.";
                return View(new List<ShoppingCartEntity>());
            }
        }

        // POST: /ShoppingCart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(
            string productRowKey,
            string productName,
            decimal unitPrice,
            int quantity = 1,
            string? imageUrl = null)
        {
            if (string.IsNullOrEmpty(productRowKey) || string.IsNullOrEmpty(productName) || quantity <= 0)
            {
                TempData["Error"] = "Invalid product or quantity.";
                return RedirectToAction("Index", "Product");
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                await _cartService.AddToCartAsync(
                    userId,
                    productRowKey,
                    productName,
                    unitPrice,
                    quantity,
                    imageUrl
                );

                // Log via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Cart Item Added: UserId={userId}, Product={productName}, Quantity={quantity}, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                );

                TempData["Success"] = $"{productName} added to cart!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart: ProductRowKey={ProductRowKey}", productRowKey);
                TempData["Error"] = "Failed to add item to cart.";
                return RedirectToAction("Index", "Product");
            }
        }

        // POST: /ShoppingCart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartId, int quantity)
        {
            if (cartId <= 0 || quantity <= 0)
            {
                TempData["Error"] = "Invalid cart item or quantity.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var cartItem = await _cartService.GetCartItemByIdAsync(cartId);

                if (cartItem == null)
                {
                    TempData["Error"] = "Cart item not found.";
                    return RedirectToAction(nameof(Index));
                }

                await _cartService.UpdateCartQuantityAsync(cartId, quantity);

                // Log via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Cart Item Updated: CartId={cartId}, Product={cartItem.ProductName}, NewQuantity={quantity}, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                );

                TempData["Success"] = "Cart updated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity: CartId={CartId}", cartId);
                TempData["Error"] = "Failed to update cart.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /ShoppingCart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartId)
        {
            if (cartId <= 0)
            {
                TempData["Error"] = "Invalid cart item.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var cartItem = await _cartService.GetCartItemByIdAsync(cartId);

                await _cartService.RemoveCartItemAsync(cartId);

                // Log via Azure Function
                if (cartItem != null)
                {
                    await _functionsService.WriteQueueMessageViaFunctionAsync(
                        $"Cart Item Removed: Product={cartItem.ProductName}, Quantity={cartItem.Quantity}, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                    );
                }

                TempData["Success"] = "Item removed from cart!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item: CartId={CartId}", cartId);
                TempData["Error"] = "Failed to remove item from cart.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /ShoppingCart/ClearCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                await _cartService.ClearCartAsync(userId);

                // Log via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Cart Cleared: UserId={userId}, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                );

                TempData["Success"] = "Cart cleared successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["Error"] = "Failed to clear cart.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /ShoppingCart/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var cartItems = await _cartService.GetCartItemsAsync(userId);

                if (!cartItems.Any())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.CartItems = cartItems;
                ViewBag.CartTotal = cartItems.Sum(item => item.TotalPrice);

                var user = await _userService.GetUserByIdAsync(userId);
                var model = new CheckoutViewModel
                {
                    CustomerName = user?.FullName ?? "",
                    DeliveryAddress = "",
                    Notes = ""
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page");
                TempData["Error"] = "Failed to load checkout page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /ShoppingCart/ProcessCheckout 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            // Reload cart items
            var cartItems = await _cartService.GetCartItemsAsync(userId);
            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CartItems = cartItems;
                ViewBag.CartTotal = cartItems.Sum(i => i.TotalPrice);
                return View("Checkout", model);
            }

            try
            {
                // Create order including cart items
                var orderId = await _orderService.CreateOrderAsync(
                    userId,
                    model.CustomerName,
                    model.DeliveryAddress,
                    model.Notes,
                    cartItems // pass cart items here
                );

                // Log order creation via Azure Function
                await _functionsService.WriteQueueMessageViaFunctionAsync(
                    $"Order Placed: OrderId={orderId}, Customer={model.CustomerName}, Total=R{cartItems.Sum(i => i.TotalPrice):F2}, Items={cartItems.Count}, Timestamp={DateTime.UtcNow.AddHours(2):yyyy-MM-dd HH:mm:ss} SAST"
                );

                // Clear cart after successful order
                await _cartService.ClearCartAsync(userId);

                TempData["Success"] = $"Order placed successfully! Order ID: {orderId}";
                return RedirectToAction("Details", "CustomerOrders", new { orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing checkout for UserId={UserId}", userId);
                TempData["Error"] = $"Failed to process order: {ex.Message}";
                ViewBag.CartItems = cartItems;
                ViewBag.CartTotal = cartItems.Sum(i => i.TotalPrice);
                return View("Checkout", model);
            }
        }


        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}
