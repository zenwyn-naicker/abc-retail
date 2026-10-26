using ABCRetailWebApp.Data;
using ABCRetailWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailWebApp.Services
{
    public class OrderService : IOrderService
    {
        private readonly ABCRetailDbContext _context;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ABCRetailDbContext context, ILogger<OrderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> CreateOrderAsync(int userId, string customerName, string? deliveryAddress, string? notes, List<ShoppingCartEntity> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
                throw new ArgumentException("Cart is empty.", nameof(cartItems));

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create the order
                    var order = new CustomerOrderEntity
                    {
                        UserId = userId,
                        CustomerName = customerName,
                        DeliveryAddress = deliveryAddress,
                        Notes = notes,
                        OrderDate = DateTime.UtcNow.AddHours(2),
                        OrderStatus = "Pending",
                        TotalAmount = cartItems.Sum(c => c.UnitPrice * c.Quantity) // optional precompute if needed
                    };

                    _context.CustomerOrders.Add(order);

                    try
                    {
                        await _context.SaveChangesAsync(); // generates OrderId
                    }
                    catch (DbUpdateException dbEx)
                    {
                        _logger.LogError(dbEx, "Failed to save CustomerOrder. Inner: {Inner}", dbEx.InnerException?.Message);
                        throw;
                    }

                    // Map cart items to OrderItems 
                    var orderItems = cartItems.Select(c =>
                    {
                        if (string.IsNullOrEmpty(c.ProductRowKey)) throw new ArgumentNullException(nameof(c.ProductRowKey));
                        if (string.IsNullOrEmpty(c.ProductName)) throw new ArgumentNullException(nameof(c.ProductName));

                        return new OrderItemEntity
                        {
                            OrderId = order.OrderId,
                            ProductRowKey = c.ProductRowKey,
                            ProductName = c.ProductName,
                            Quantity = c.Quantity,
                            UnitPrice = Convert.ToDecimal(c.UnitPrice)
                            // TotalPrice is computed in DB
                        };
                    }).ToList();

                    _context.OrderItems.AddRange(orderItems);

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException dbEx)
                    {
                        _logger.LogError(dbEx, "Failed to save OrderItems. Inner: {Inner}", dbEx.InnerException?.Message);
                        throw;
                    }

                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("Order successfully created: OrderId={OrderId}", order.OrderId);

                    return order.OrderId;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction rolled back for creating order for UserId={UserId}", userId);
                    throw;
                }
            });
        }





        public async Task<List<CustomerOrderEntity>> GetOrdersAsync(int userId)
        {
            try
            {
                return await _context.CustomerOrders
                    .Include(o => o.Items)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<CustomerOrderEntity>> GetAllOrdersAsync()
        {
            try
            {
                return await _context.CustomerOrders
                    .Include(o => o.User)
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all orders");
                throw;
            }
        }

        public async Task<CustomerOrderEntity?> GetOrderByIdAsync(int orderId)
        {
            try
            {
                return await _context.CustomerOrders
                    .Include(o => o.User)
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order by ID: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            try
            {
                var order = await _context.CustomerOrders.FindAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order not found for status update: OrderId={OrderId}", orderId);
                    return false;
                }

                order.OrderStatus = newStatus;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order status updated: OrderId={OrderId}, NewStatus={Status}", orderId, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status: OrderId={OrderId}", orderId);
                return false;
            }
        }

        public async Task<Dictionary<string, int>> GetOrderStatusCountsAsync()
        {
            try
            {
                var statusCounts = await _context.CustomerOrders
                    .GroupBy(o => o.OrderStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count);

                return statusCounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order status counts");
                return new Dictionary<string, int>();
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            try
            {
                var order = await _context.CustomerOrders.FindAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order not found for cancellation: OrderId={OrderId}", orderId);
                    return false;
                }

                if (order.OrderStatus == "Shipped" || order.OrderStatus == "Delivered")
                {
                    _logger.LogWarning("Cannot cancel order with status {Status}: OrderId={OrderId}", order.OrderStatus, orderId);
                    return false;
                }

                order.OrderStatus = "Cancelled";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order cancelled: OrderId={OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order: OrderId={OrderId}", orderId);
                return false;
            }
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            try
            {
                var revenue = await _context.CustomerOrders
                    .Where(o => o.OrderStatus == "Delivered" || o.OrderStatus == "Completed")
                    .SumAsync(o => o.TotalAmount);

                return revenue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total revenue");
                return 0;
            }
        }

        public async Task<List<CustomerOrderEntity>> GetRecentOrdersAsync(int count = 10)
        {
            try
            {
                return await _context.CustomerOrders
                    .Include(o => o.User)
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent orders");
                return new List<CustomerOrderEntity>();
            }
        }

        public async Task<List<CustomerOrderEntity>> GetOrdersByStatusAsync(string status)
        {
            try
            {
                return await _context.CustomerOrders
                    .Include(o => o.User)
                    .Include(o => o.Items)
                    .Where(o => o.OrderStatus == status)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders by status: {Status}", status);
                return new List<CustomerOrderEntity>();
            }
        }
    }
}
