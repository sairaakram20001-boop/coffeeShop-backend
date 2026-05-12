using System;
using System.Collections.Generic;

namespace CoffeeShop.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        public int CartId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public Cart Cart { get; set; }

        public Product Product { get; set; }
    }
}