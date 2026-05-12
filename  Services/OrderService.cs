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

		// PLACE ORDER 
		public async Task<string> PlaceOrder(int userId)
		{
			var cart = await _context.Carts
				.Include(c => c.CartItems)
				.ThenInclude(ci => ci.Product)
				.FirstOrDefaultAsync(c => c.UserId == userId);

			if (cart == null || !cart.CartItems.Any())
				return "Cart is empty";

			// Final stock re-check before order
			foreach (var item in cart.CartItems)
			{
				var product = await _context.Products.FindAsync(item.ProductId);

				if (product == null)
					return $"Product with ID {item.ProductId} not found";

				if (product.StockQuantity < item.Quantity)
					return $"{product.Name} has insufficient stock";
			}

			// Create order
			var order = new Order
			{
				UserId = userId,
				CreatedAt = DateTime.Now,
				UpdatedAt = DateTime.Now,
				Status = "Pending",
				TotalAmount = cart.CartItems.Sum(x => x.Quantity * x.Product.Price)
			};

			_context.Orders.Add(order);
			await _context.SaveChangesAsync();

			// Create order items + deduct stock
			foreach (var item in cart.CartItems)
			{
				var product = await _context.Products.FindAsync(item.ProductId);

				product.StockQuantity -= item.Quantity;

				var orderItem = new OrderItem
				{
					OrderId = order.Id,
					ProductId = item.ProductId,
					Quantity = item.Quantity,
					Price = product.Price
				};

				_context.OrderItems.Add(orderItem);
			}

			// Clear cart after successful order
			_context.CartItems.RemoveRange(cart.CartItems);

			await _context.SaveChangesAsync();

			return "Order placed successfully";
		}

		// ORDER HISTORY
		public async Task<List<Order>> GetOrders(int userId)
		{
			return await _context.Orders
				.Include(o => o.OrderItems)
				.ThenInclude(oi => oi.Product)
				.Where(o => o.UserId == userId)
				.ToListAsync();
		}

		// CANCEL ORDER
		public async Task<string> CancelOrder(int orderId)
		{
			var order = await _context.Orders
				.Include(o => o.OrderItems)
				.FirstOrDefaultAsync(o => o.Id == orderId);

			if (order == null)
				return "Order not found";

			if (order.Status == "Cancelled")
				return "Order already cancelled";

			if (order.Status == "Delivered")
				return "Delivered orders cannot be cancelled";

			if (order.Status == "Shipped")
				return "Shipped orders cannot be cancelled";

			foreach (var item in order.OrderItems)
			{
				var product = await _context.Products.FindAsync(item.ProductId);

				if (product == null)
					return $"Product with ID {item.ProductId} not found";

				product.StockQuantity += item.Quantity;
			}

			order.Status = "Cancelled";
			order.UpdatedAt = DateTime.Now;

			await _context.SaveChangesAsync();

			return "Order cancelled successfully";
		}
	}
}