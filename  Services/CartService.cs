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

        // ADD TO CART (WITH STOCK VALIDATION) 
        public async Task<string> AddToCart(int userId, int productId, int quantity)
        {
            // 1. Check product exists
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
                return "Product not found";

            // 2. Check stock availability
            if (product.StockQuantity <= 0)
                return "Product out of stock";

            // 3. Check requested quantity
            if (quantity <= 0)
                return "Quantity must be greater than 0";

            // 4. Check if enough stock
            if (quantity > product.StockQuantity)
                return $"Only {product.StockQuantity} items available in stock";

            // 5. Get or create cart
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CartItems = new List<CartItem>()
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // 6. Check if item already exists in cart
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);

            if (existingItem != null)
            {
                // validate total quantity after update
                if (existingItem.Quantity + quantity > product.StockQuantity)
                    return $"Cannot add more than available stock ({product.StockQuantity})";

                existingItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                };

                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            return "Product added to cart";
        }

        // GET CART 
        public async Task<Cart> GetCart(int userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        //REMOVE ITEM 
        public async Task<string> RemoveItem(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);

            if (item == null)
                return "Item not found";

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return "Item removed";
        }
    }
}