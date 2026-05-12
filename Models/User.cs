using System;
using System.Collections.Generic;

namespace CoffeeShop.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        public ICollection<Auth> Auths { get; set; }

        public Cart Cart { get; set; }

        public ICollection<Order> Orders { get; set; }
    }
}