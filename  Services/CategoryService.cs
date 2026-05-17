using CoffeeShop.Data;
using CoffeeShop.Models;
using CoffeeShop.DTOs.Category;
using Microsoft.EntityFrameworkCore;

namespace CoffeeShop.Services
{
    public class CategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        // 1. CREATE
        public async Task<CategoryDto?> CreateAsync(CreateCategoryDto dto)
        {
            bool exists = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower());

            if (exists) return null;

            var category = new Category
            {
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return MapToDto(category);
        }

        // 2. GET ALL
        public async Task<List<CategoryDto>> GetAllAsync()
        {
            var categories = await _context.Categories.ToListAsync();
            return categories.Select(c => MapToDto(c)).ToList();
        }

        // 3. GET BY ID
        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return null;

            return MapToDto(category);
        }

        // 4. UPDATE
        public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return null;

            bool nameExists = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.Id != id);

            if (nameExists) return null;

            category.Name = dto.Name;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(category);
        }

        // 5. DELETE
        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id);

            if (category == null) return false;

            _context.Categories.Remove(category);
            int rowsAffected = await _context.SaveChangesAsync();

            return rowsAffected > 0;
        }

        // --- HELPER METHOD: Yeh "Map" error ko fix karega ---
        private CategoryDto MapToDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name
                // Agar CategoryDto mein aur fields hain (jaise CreatedAt), to wo bhi yahan add karein
            };
        }
    }
}