using ABCRetailWebApp.Data;
using ABCRetailWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailWebApp.Services
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly ABCRetailDbContext _context;
        private readonly ILogger<ShoppingCartService> _logger;

        public ShoppingCartService(ABCRetailDbContext context, ILogger<ShoppingCartService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddToCartAsync(int userId, string productRowKey, string productName,
            decimal unitPrice, int quantity, string? imageUrl)
        {
            try
            {
                var existingItem = await _context.ShoppingCart
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductRowKey == productRowKey);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    existingItem.LastModified = DateTime.UtcNow;

                    _logger.LogInformation("Updated cart quantity: CartId={CartId}, NewQuantity={Quantity}",
                        existingItem.CartId, existingItem.Quantity);
                }
                else
                {
                    var newCartItem = new ShoppingCartEntity
                    {
                        UserId = userId,
                        ProductRowKey = productRowKey,
                        ProductName = productName,
                        UnitPrice = unitPrice,
                        Quantity = quantity,
                        ImageUrl = imageUrl,
                        DateAdded = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };

                    _context.ShoppingCart.Add(newCartItem);

                    _logger.LogInformation("Added to cart: UserId={UserId}, Product={ProductName}, Quantity={Quantity}",
                        userId, productName, quantity);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart: UserId={UserId}, ProductRowKey={ProductRowKey}",
                    userId, productRowKey);
                throw;
            }
        }

        public async Task<List<ShoppingCartEntity>> GetCartItemsAsync(int userId)
        {
            try
            {
                return await _context.ShoppingCart
                    .Where(c => c.UserId == userId)
                    .OrderBy(c => c.DateAdded)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart items for UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateCartQuantityAsync(int cartId, int newQuantity)
        {
            try
            {
                var cartItem = await _context.ShoppingCart.FindAsync(cartId);

                if (cartItem == null)
                {
                    _logger.LogWarning("Cart item not found: CartId={CartId}", cartId);
                    return false;
                }

                if (newQuantity <= 0)
                {
                    _context.ShoppingCart.Remove(cartItem);
                    _logger.LogInformation("Removed cart item: CartId={CartId}", cartId);
                }
                else
                {
                    cartItem.Quantity = newQuantity;
                    cartItem.LastModified = DateTime.UtcNow;
                    _logger.LogInformation("Updated cart quantity: CartId={CartId}, NewQuantity={Quantity}",
                        cartId, newQuantity);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity: CartId={CartId}", cartId);
                return false;
            }
        }

        public async Task<bool> RemoveCartItemAsync(int cartId)
        {
            try
            {
                var cartItem = await _context.ShoppingCart.FindAsync(cartId);

                if (cartItem == null)
                {
                    _logger.LogWarning("Cart item not found for removal: CartId={CartId}", cartId);
                    return false;
                }

                _context.ShoppingCart.Remove(cartItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Removed cart item: CartId={CartId}", cartId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item: CartId={CartId}", cartId);
                return false;
            }
        }

        public async Task<bool> ClearCartAsync(int userId)
        {
            try
            {
                var cartItems = await _context.ShoppingCart
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                if (cartItems.Any())
                {
                    _context.ShoppingCart.RemoveRange(cartItems);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleared cart for UserId={UserId}, Items removed: {Count}",
                        userId, cartItems.Count);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for UserId: {UserId}", userId);
                return false;
            }
        }

        public async Task<decimal> GetCartTotalAsync(int userId)
        {
            try
            {
                var total = await _context.ShoppingCart
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.UnitPrice * c.Quantity);

                return total;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating cart total for UserId: {UserId}", userId);
                return 0;
            }
        }

        public async Task<ShoppingCartEntity?> GetCartItemByIdAsync(int cartId)
        {
            try
            {
                return await _context.ShoppingCart
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CartId == cartId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item by ID: {CartId}", cartId);
                throw;
            }
        }

        public async Task<int> GetCartItemCountAsync(int userId)
        {
            try
            {
                return await _context.ShoppingCart
                    .Where(c => c.UserId == userId)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count for UserId: {UserId}", userId);
                return 0;
            }
        }
    }
}
