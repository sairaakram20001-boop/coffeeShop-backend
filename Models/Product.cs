using System;
using System.Collections.Generic;

namespace CoffeeShop.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int CategoryId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public Category Category { get; set; }

        public ICollection<CartItem> CartItems { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
        public int StockQuantity { get; set; }
    }

}