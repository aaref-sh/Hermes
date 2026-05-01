using HStore.Domain.Classes;

namespace HStore.Application.DTOs;

public class UpdateProductDto
{
    public LocalizedProperty Name { get; set; }
    public LocalizedProperty Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public List<int>? CategoryIds { get; set; }
}
