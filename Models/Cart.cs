using System;
using System.Collections.Generic;

namespace CoffeeShop.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        // If you have a User model, add it here
        // public virtual User User { get; set; } = null!;
    }
}