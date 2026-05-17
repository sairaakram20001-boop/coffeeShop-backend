using CoffeeShop.Data;
using CoffeeShop.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeShop.Services
{
    public class OrderService
    {
        private readonly AppDbContext _context;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> PlaceOrder(int userId)
        {
            // Start Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    return "Cart is empty";

                // 1. Create the Order first
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    TotalAmount = cart.CartItems.Sum(x => x.Quantity * x.Product.Price)
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 2. Process Items and Deduct Stock
                foreach (var item in cart.CartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);

                    if (product == null || product.StockQuantity < item.Quantity)
                    {
                        throw new Exception(product == null ? "Product missing" : $"{product.Name} stock exhausted.");
                    }

                    product.StockQuantity -= item.Quantity; // Deduct Stock

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = product.Price
                    };
                    _context.OrderItems.Add(orderItem);
                }

                // 3. Cleanup
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return $"Order #{order.Id} placed successfully";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return $"Checkout failed: {ex.Message}";
            }
        }

        public async Task<string> CancelOrder(int orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return "Order not found";
                if (order.Status != "Pending") return $"Cannot cancel order with status: {order.Status}";

                // 1. Reverse Stock
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }

                // 2. Update Status
                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return "Order cancelled successfully and stock returned.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return $"Cancellation failed: {ex.Message}";
            }
        }

        public async Task<object> GetOrders(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o => new {
                o.Id,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                Items = o.OrderItems.Select(oi => new {
                    oi.ProductId,
                    ProductName = oi.Product.Name,
                    oi.Quantity,
                    oi.Price,
                    SubTotal = oi.Quantity * oi.Price
                })
            });
        }
    }
}