using CoffeeShop.Data;
using CoffeeShop.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeShop.Services
{
    public class CartService
    {
        private readonly AppDbContext _context;

        public CartService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> AddToCart(int userId, int productId, int quantity)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (quantity <= 0) return "Quantity must be greater than 0";

                var product = await _context.Products.FindAsync(productId);
                if (product == null) return "Product not found";

                if (product.StockQuantity < quantity)
                    return $"Insufficient stock. Only {product.StockQuantity} available.";

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

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);

                if (existingItem != null)
                {
                    if (existingItem.Quantity + quantity > product.StockQuantity)
                        return $"Limit reached. Total in cart ({existingItem.Quantity + quantity}) would exceed stock ({product.StockQuantity}).";

                    existingItem.Quantity += quantity;
                    _context.CartItems.Update(existingItem);
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = productId,
                        Quantity = quantity
                        // Yahan se CreatedAt hata diya gaya hai taake error na aaye
                    };
                    _context.CartItems.Add(cartItem);
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return "Success: Product added to cart";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return $"Error: {ex.Message}";
            }
        }

        public async Task<object?> GetCart(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return null;

            return new
            {
                cart.Id,
                cart.UserId,
                Items = cart.CartItems.Select(ci => new
                {
                    ci.Id,
                    ci.ProductId,
                    ProductName = ci.Product.Name,
                    ImageUrl = ci.Product.ImageUrl,
                    Price = ci.Product.Price,
                    ci.Quantity,
                    Total = ci.Product.Price * ci.Quantity
                }).ToList()
            };
        }

        public async Task<bool> RemoveItem(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) return false;

            _context.CartItems.Remove(item);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ClearCart(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any()) return true;

            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.UtcNow;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<string> ChangeItemQuantity(int cartItemId, int delta)
        {
            if (delta == 0) return "No quantity change requested";

            var cartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            if (cartItem == null) return "Cart item not found";

            var nextQuantity = cartItem.Quantity + delta;
            if (nextQuantity < 1) return "Quantity cannot be less than 1";

            if (cartItem.Product.StockQuantity < nextQuantity)
                return $"Insufficient stock. Only {cartItem.Product.StockQuantity} available.";

            cartItem.Quantity = nextQuantity;
            await _context.SaveChangesAsync();

            return "Success: Cart item quantity updated";
        }
    }
}
