namespace CoffeeShop.DTOs.Category
{
	
	// CREATE DTO
	
	public class CreateCategoryDto
	{
		public string Name { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}

	
	// UPDATE DTO
	
	public class UpdateCategoryDto
	{
		public string Name { get; set; }

		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}

	// -----------------------------
	// READ DTO (GET response)
	// -----------------------------
	public class CategoryDto
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public DateTime CreatedAt { get; set; }

		public DateTime? UpdatedAt { get; set; }
	}
}