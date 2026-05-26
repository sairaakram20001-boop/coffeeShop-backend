using CoffeeShop.Data;
using CoffeeShop.Models;
using CoffeeShop.DTOs.Product;
using Microsoft.EntityFrameworkCore;

namespace CoffeeShop.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        // 1. CREATE PRODUCT (Initial Stock Set)
        public async Task<Product?> CreateAsync(ProductDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Duplicate Name Check
                var nameExists = await _context.Products
                    .AnyAsync(p => p.Name.ToLower().Trim() == dto.Name.ToLower().Trim());
                if (nameExists) throw new Exception("Product with this name already exists.");

                // Category Check
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
                if (!categoryExists) throw new Exception("Invalid CategoryId.");

                var product = new Product
                {
                    Name = dto.Name.Trim(),
                    Title = dto.Name.Trim(), // Sync Title with Name
                    Description = dto.Description,
                    ImageUrl = dto.ImageUrl,
                    Price = dto.Price,
                    CategoryId = dto.CategoryId,
                    StockQuantity = dto.StockQuantity, // Logic for initial stock
                    Stock = dto.StockQuantity,         // Sync both DB columns
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return product;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 2. UPDATE PRODUCT (Restock/Edit Logic)
        public async Task<Product?> UpdateAsync(int id, ProductDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return null;

                // Duplicate Name Check (excluding current ID)
                var nameExists = await _context.Products
                    .AnyAsync(p => p.Name.ToLower().Trim() == dto.Name.ToLower().Trim() && p.Id != id);
                if (nameExists) throw new Exception("Another product already has this name.");

                // Map updated values
                product.Name = dto.Name.Trim();
                product.Title = dto.Name.Trim();
                product.Description = dto.Description;
                product.ImageUrl = dto.ImageUrl;
                product.Price = dto.Price;
                product.CategoryId = dto.CategoryId;

                // STOCK UPDATE LOGIC: Yahan se DB mein stock update hoga
                product.StockQuantity = dto.StockQuantity;
                product.Stock = dto.StockQuantity;

                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return product;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 3. GET ALL PRODUCTS
        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
        }

        // 4. GET BY ID
        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Product>> GetByCategoryIdAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
        }

        // 5. DELETE PRODUCT
        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            _context.Products.Remove(product);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
