using HStore.Domain.Classes;

namespace HStore.Application.DTOs;

public class CreateCategoryDto
{
    public LocalizedProperty Name { get; set; } = new();
    public LocalizedProperty Description { get; set; } = new();
    public string ImageUrl { get; set; }
    public int? ParentCategoryId { get; set; }
}
