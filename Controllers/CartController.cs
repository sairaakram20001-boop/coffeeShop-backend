using Microsoft.AspNetCore.Mvc;
using CoffeeShop.DTOs.Cart;
using CoffeeShop.Services;

namespace CoffeeShop.Controllers
{
    public class ChangeQuantityDto
    {
        public int Delta { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly CartService _cartService;

        public CartController(CartService cartService)
        {
            _cartService = cartService;
        }

        // 1. ADD TO CART
        [HttpPost("add")]
        public async Task<IActionResult> Add(AddToCartDto dto)
        {
            try
            {
                var result = await _cartService.AddToCart(dto.UserId, dto.ProductId, dto.Quantity);

                if (result.StartsWith("Success"))
                    return Ok(new { message = result });

                return BadRequest(new { message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2. GET USER CART
        // CartController.cs mein GetCart method update 
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            // Yahan 'var' use karein taake anonymous object handle ho sake
            var cartData = await _cartService.GetCart(userId);

            if (cartData == null)
                return NotFound(new { message = "Cart not found for this user." });

            return Ok(cartData); //  loop nahi karega
        }

        // 3. REMOVE SPECIFIC ITEM FROM CART
        [HttpDelete("remove/{cartItemId}")]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var success = await _cartService.RemoveItem(cartItemId);
            if (!success)
                return NotFound(new { message = "Item not found in cart." });

            return Ok(new { message = "Item removed successfully." });
        }

        // 4. CLEAR ENTIRE CART (Optional but useful)
        [HttpDelete("clear/{userId}")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            // Note: Ensure ClearCart is implemented in your CartService
            var success = await _cartService.ClearCart(userId);
            if (!success) return BadRequest(new { message = "Could not clear cart." });

            return Ok(new { message = "Cart cleared successfully." });
        }

        // 5. CHANGE ITEM QUANTITY (+1 / -1)
        [HttpPut("quantity/{cartItemId}")]
        public async Task<IActionResult> ChangeQuantity(int cartItemId, [FromBody] ChangeQuantityDto dto)
        {
            var result = await _cartService.ChangeItemQuantity(cartItemId, dto.Delta);
            if (result.StartsWith("Success"))
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }
    }
}
