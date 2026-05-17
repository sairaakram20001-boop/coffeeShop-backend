using System.Text.Json.Serialization;

namespace CoffeeShop.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [JsonIgnore] // This stops the circular reference loop
        public virtual Cart Cart { get; set; } = null!;

        public virtual Product Product { get; set; } = null!;
    }
}