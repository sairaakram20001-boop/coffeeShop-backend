using CoffeeShop.DTOs.Order;
using Microsoft.AspNetCore.Mvc;
using CoffeeShop.Services;
namespace CoffeeShop.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class OrderController : ControllerBase
	{
		private readonly OrderService _orderService;

		public OrderController(OrderService orderService)
		{
			_orderService = orderService;
		}

		// Place order
		[HttpPost("place")]
		public async Task<IActionResult> PlaceOrder(PlaceOrderDto dto)
		{
			var result = await _orderService.PlaceOrder(dto.UserId);
			return Ok(new { message = result });
		}

		// Order history
		[HttpGet("{userId}")]
		public async Task<IActionResult> GetOrders(int userId)
		{
			var orders = await _orderService.GetOrders(userId);
			return Ok(orders);
		}

		// Cancel order
		[HttpPost("cancel/{orderId}")]
		public async Task<IActionResult> Cancel(int orderId)
		{
			var result = await _orderService.CancelOrder(orderId);
			return Ok(new { message = result });
		}
	}
}