using ABCRetailWebApp.Models;

namespace ABCRetailWebApp.Services
{
    public interface IShoppingCartService
    {
        Task AddToCartAsync(int userId, string productRowKey, string productName, decimal unitPrice, int quantity, string? imageUrl);
        Task<List<ShoppingCartEntity>> GetCartItemsAsync(int userId);
        Task<bool> UpdateCartQuantityAsync(int cartId, int newQuantity);
        Task<bool> RemoveCartItemAsync(int cartId);
        Task<bool> ClearCartAsync(int userId);
        Task<decimal> GetCartTotalAsync(int userId);
        Task<ShoppingCartEntity?> GetCartItemByIdAsync(int cartId);
        Task<int> GetCartItemCountAsync(int userId);
    }
}
