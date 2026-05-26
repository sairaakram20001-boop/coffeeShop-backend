using System;
using System.Collections.Generic;

namespace CoffeeShop.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; }
        
        public string? Address { get; set; }
        public string? FullAddress { get; set; }
        public string? AddressLine2 { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? City { get; set; }

        public string? PhoneNumber { get; set; }
        public DateTime? ConfirmedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
    }
}
