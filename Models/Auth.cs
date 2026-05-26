using System;
using System.Collections.Generic;

namespace CoffeeShop.Models
{
    public class Auth
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Token { get; set; }

        public bool IsRevoked { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        // Navigation Property
        public User User { get; set; }
    }
}