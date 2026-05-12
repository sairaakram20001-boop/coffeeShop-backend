using System;
using System.Collections.Generic;

namespace CoffeeShop.Models
{
    public class Category
{
    public int Id { get; set; }

    public string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation Property
    public ICollection<Product> Products { get; set; }
}
}