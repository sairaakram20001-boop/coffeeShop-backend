using CoffeeShop.DTOs.Order;
using Microsoft.AspNetCore.Mvc;
using CoffeeShop.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace CoffeeShop.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class OrderController : ControllerBase
	{
		private readonly OrderService _orderService;

		public OrderController(OrderService orderService)
		{
			_orderService = orderService;
		}

		private int? GetCurrentUserId()
		{
			var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (int.TryParse(claim, out var userId))
			{
				return userId;
			}
			return null;
		}

		// Place order
		[HttpPost("place")]
		public async Task<IActionResult> PlaceOrder(PlaceOrderDto dto)
		{
			var currentUserId = GetCurrentUserId();
			if (currentUserId == null) return Unauthorized(new { message = "Unauthorized." });
			if (dto.UserId != currentUserId.Value) return Forbid();

			var result = await _orderService.PlaceOrder(
				dto.UserId,
				dto.FirstName,
				dto.LastName,
				dto.Address,
				dto.FullAddress,
				dto.AddressLine2,
				dto.City,
				dto.PhoneNumber
			);
			if (!result.Success) return BadRequest(new { message = result.Message });
			return Ok(new { message = result.Message, orderId = result.OrderId, status = "Confirmed" });
		}


		[HttpPost("confirm/{orderId}")]
		public async Task<IActionResult> ConfirmOrder(int orderId, ConfirmOrderDto dto)
		{
			var currentUserId = GetCurrentUserId();
			if (currentUserId == null) return Unauthorized(new { message = "Unauthorized." });

			var result = await _orderService.ConfirmOrder(currentUserId.Value, orderId, dto.FullName, dto.Address, dto.PhoneNumber, dto.City);
			if (!result.Success) return BadRequest(new { message = result.Message });
			return Ok(new { message = result.Message, orderId = result.OrderId, status = "Confirmed" });
		}

		// Order history
		[HttpGet("{userId}")]
		public async Task<IActionResult> GetOrders(int userId)
		{
			var currentUserId = GetCurrentUserId();
			if (currentUserId == null) return Unauthorized(new { message = "Unauthorized." });
			if (userId != currentUserId.Value) return Forbid();

			var orders = await _orderService.GetOrders(userId);
			return Ok(orders);
		}

		// Cancel order
		[HttpPost("cancel/{orderId}")]
		public async Task<IActionResult> Cancel(int orderId)
		{
			var currentUserId = GetCurrentUserId();
			if (currentUserId == null) return Unauthorized(new { message = "Unauthorized." });

			var result = await _orderService.CancelOrder(currentUserId.Value, orderId);
			if (!result.Success) return BadRequest(new { message = result.Message });
			return Ok(new { message = result.Message, orderId = result.OrderId, status = "Cancelled" });
		}

		[HttpPost("reorder/{orderId}")]
		public async Task<IActionResult> Reorder(int orderId)
		{
			var currentUserId = GetCurrentUserId();
			if (currentUserId == null) return Unauthorized(new { message = "Unauthorized." });

			var result = await _orderService.Reorder(currentUserId.Value, orderId);
			if (!result.Success) return BadRequest(new { message = result.Message });
			return Ok(new { message = result.Message, orderId = result.OrderId });
		}
	}
}
