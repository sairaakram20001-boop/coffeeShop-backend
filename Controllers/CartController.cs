using Microsoft.AspNetCore.Mvc;
using CoffeeShop.DTOs.Cart;
using CoffeeShop.Services;

namespace CoffeeShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly CartService _cartService;

        public CartController(CartService cartService)
        {
            _cartService = cartService;
        }

        // ADD TO CART
        [HttpPost("add")]
        public async Task<IActionResult> Add(AddToCartDto dto)
        {
            var result = await _cartService.AddToCart(
                dto.UserId,
                dto.ProductId,
                dto.Quantity
            );

            return Ok(result);
        }

        // GET CART
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            var cart = await _cartService.GetCart(userId);
            return Ok(cart);
        }

        //  REMOVE ITEM
        [HttpDelete("{cartItemId}")]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var result = await _cartService.RemoveItem(cartItemId);
            return Ok(result);
        }
    }
}