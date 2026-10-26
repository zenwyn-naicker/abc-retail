using ABCRetailWebApp.Models;

namespace ABCRetailWebApp.Services
{
    public interface IOrderService
    {
        Task<int> CreateOrderAsync(int userId, string customerName, string? deliveryAddress, string? notes, List<ShoppingCartEntity> cartItems);
        Task<List<CustomerOrderEntity>> GetOrdersAsync(int userId);
        Task<List<CustomerOrderEntity>> GetAllOrdersAsync();
        Task<CustomerOrderEntity?> GetOrderByIdAsync(int orderId);
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);
        Task<Dictionary<string, int>> GetOrderStatusCountsAsync();
        Task<bool> CancelOrderAsync(int orderId);
        Task<decimal> GetTotalRevenueAsync();
        Task<List<CustomerOrderEntity>> GetRecentOrdersAsync(int count = 10);
        Task<List<CustomerOrderEntity>> GetOrdersByStatusAsync(string status);
    }
}
