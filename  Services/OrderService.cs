using CoffeeShop.Data;
using CoffeeShop.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeShop.Services
{
    public class OrderActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? OrderId { get; set; }
    }

    public class OrderService
    {
        private readonly AppDbContext _context;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderActionResult> PlaceOrder(
            int userId,
            string firstName,
            string lastName,
            string address,
            string fullAddress,
            string addressLine2,
            string city,
            string phoneNumber)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(firstName))
                    return new OrderActionResult { Success = false, Message = "First Name is required." };
                if (string.IsNullOrWhiteSpace(lastName))
                    return new OrderActionResult { Success = false, Message = "Last Name is required." };
                if (string.IsNullOrWhiteSpace(address) || address.Trim().Length < 8)
                    return new OrderActionResult { Success = false, Message = "Address is required." };
                if (string.IsNullOrWhiteSpace(city))
                    return new OrderActionResult { Success = false, Message = "City is required." };
                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return new OrderActionResult { Success = false, Message = "Phone number is required." };

                var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
                    return new OrderActionResult { Success = false, Message = "Phone number format is invalid." };

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    return new OrderActionResult { Success = false, Message = "Cart is empty" };

                var order = new Order
                {
                    UserId = userId,
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    Address = address.Trim(),
                    FullAddress = string.IsNullOrWhiteSpace(fullAddress)
                        ? $"{address.Trim()} {addressLine2?.Trim()} {city.Trim()}".Trim()
                        : fullAddress.Trim(),
                    AddressLine2 = addressLine2?.Trim(),
                    City = city.Trim(),
                    PhoneNumber = phoneNumber.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ConfirmedAt = DateTime.UtcNow,
                    Status = "Confirmed",
                    TotalAmount = cart.CartItems.Sum(x => x.Quantity * x.Product.Price)
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cart.CartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);

                    if (product == null || product.StockQuantity < item.Quantity)
                    {
                        throw new Exception(product == null ? "Product missing" : $"{product.Name} stock exhausted.");
                    }

                    product.StockQuantity -= item.Quantity;

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = product.Price,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.OrderItems.Add(orderItem);
                }

                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return new OrderActionResult
                {
                    Success = true,
                    Message = $"Order #{order.Id} placed successfully",
                    OrderId = order.Id
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var detail = ex.InnerException?.Message ?? ex.Message;
                return new OrderActionResult { Success = false, Message = $"Checkout failed: {detail}" };
            }
        }

        public async Task<OrderActionResult> ConfirmOrder(int userId, int orderId, string fullName, string address, string phoneNumber, string city)
        {
            var normalizedCity = city?.Trim() ?? string.Empty;
            if (!string.Equals(normalizedCity, "Lahore", StringComparison.OrdinalIgnoreCase))
                return new OrderActionResult { Success = false, Message = "Only Lahore is allowed for city." };

            if (string.IsNullOrWhiteSpace(fullName))
                return new OrderActionResult { Success = false, Message = "Full Name is required." };

            if (string.IsNullOrWhiteSpace(address) || address.Trim().Length < 10)
                return new OrderActionResult { Success = false, Message = "Address must be at least 10 characters." };

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new OrderActionResult { Success = false, Message = "Phone number is required." };
            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
                return new OrderActionResult { Success = false, Message = "Phone number format is invalid." };

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (order == null) return new OrderActionResult { Success = false, Message = "Order not found." };
            if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                return new OrderActionResult { Success = false, Message = "Cancelled order cannot be confirmed." };

            order.Address = address.Trim();
            order.PhoneNumber = phoneNumber.Trim();
            order.Status = "Confirmed";
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new OrderActionResult
            {
                Success = true,
                Message = $"Order #{order.Id} confirmed successfully.",
                OrderId = order.Id
            };
        }

        public async Task<OrderActionResult> CancelOrder(int userId, int orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                if (order == null) return new OrderActionResult { Success = false, Message = "Order not found" };
                if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    return new OrderActionResult { Success = false, Message = "Order is already cancelled." };
                if (DateTime.UtcNow > order.CreatedAt.AddHours(1))
                    return new OrderActionResult { Success = false, Message = "Order can only be cancelled within 1 hour of placement." };

                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }

                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new OrderActionResult
                {
                    Success = true,
                    Message = "Order cancelled successfully and stock returned.",
                    OrderId = order.Id
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new OrderActionResult { Success = false, Message = $"Cancellation failed: {ex.Message}" };
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

            return orders.Select(o => new
            {
                o.Id,
                o.Status,
                o.TotalAmount,
                o.FirstName,
                o.LastName,
                o.Address,
                o.FullAddress,
                o.AddressLine2,
                o.City,
                o.PhoneNumber,
                o.ConfirmedAt,
                o.CreatedAt,
                o.UpdatedAt,
                CancelWindowEndsAt = o.CreatedAt.AddHours(1),
                CancelAllowed = !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                    && DateTime.UtcNow <= o.CreatedAt.AddHours(1),
                Items = o.OrderItems.Select(oi => new
                {
                    oi.ProductId,
                    ProductName = oi.Product.Name,
                    ProductImageUrl = oi.Product.ImageUrl,
                    oi.Quantity,
                    oi.Price,
                    SubTotal = oi.Quantity * oi.Price
                })
            });
        }

        public async Task<OrderActionResult> Reorder(int userId, int orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                if (order == null) return new OrderActionResult { Success = false, Message = "Order not found." };
                if (!order.OrderItems.Any()) return new OrderActionResult { Success = false, Message = "Order has no items to reorder." };

                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                foreach (var item in order.OrderItems)
                {
                    var product = item.Product ?? await _context.Products.FindAsync(item.ProductId);
                    if (product == null) continue;

                    if (product.StockQuantity < item.Quantity)
                        return new OrderActionResult { Success = false, Message = $"{product.Name} does not have enough stock." };

                    var existingItem = await _context.CartItems
                        .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == item.ProductId);

                    if (existingItem == null)
                    {
                        _context.CartItems.Add(new CartItem
                        {
                            CartId = cart.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        var nextQty = existingItem.Quantity + item.Quantity;
                        if (product.StockQuantity < nextQty)
                            return new OrderActionResult { Success = false, Message = $"{product.Name} stock is not enough for reorder quantity." };
                        existingItem.Quantity = nextQty;
                    }
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new OrderActionResult
                {
                    Success = true,
                    Message = $"Order #{order.Id} items added to cart."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new OrderActionResult { Success = false, Message = $"Reorder failed: {ex.Message}" };
            }
        }
    }
}
