using HStore.Domain.Classes;

namespace HStore.Domain.Entities;

public class Category : BaseEntity
{
    public LocalizedProperty Name { get; set; } = new();
    public LocalizedProperty Description { get; set; } = new();
    public string ImageUrl { get; set; } = "";

    public int? ParentCategoryId { get; set; }
    public Category ParentCategory { get; set; }

    // Navigation Properties
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<Category> SubCategories { get; set; } = [];
}
