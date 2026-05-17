using CoffeeShop.DTOs.Product;
using CoffeeShop.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductController(ProductService productService)
        {
            _productService = productService;
        }

        // 1. CREATE: Postman se stockQuantity ke sath call karein
        [HttpPost]
        public async Task<IActionResult> Create(ProductDto dto)
        {
            try
            {
                var product = await _productService.CreateAsync(dto);
                return Ok(new { message = "Product created successfully", data = product });
            }
            catch (Exception ex)
            {
                // Agar duplicate name ya invalid category ho to error message dega
                return BadRequest(new { message = ex.Message });
            }
        }

        // 2. UPDATE: Stock update karne ke liye lazmi hai
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ProductDto dto)
        {
            try
            {
                var updatedProduct = await _productService.UpdateAsync(id, dto);
                if (updatedProduct == null)
                    return NotFound(new { message = "Product not found" });

                return Ok(new { message = "Product updated successfully", data = updatedProduct });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // 3. GET ALL
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        // 4. GET BY ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            return Ok(product);
        }

        // 5. DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _productService.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "Product not found" });

            return Ok(new { message = "Product deleted successfully" });
        }
    }
}