using HStore.Domain.Classes;

namespace HStore.Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public LocalizedProperty Name { get; set; } = new();
    public LocalizedProperty Description { get; set; } = new();
    public string ImageUrl { get; set; }
    public int? ParentCategoryId { get; set; }
    public LocalizedProperty ParentCategoryName { get; set; }
}
