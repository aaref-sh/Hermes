using HStore.Domain.Classes;
using HStore.Application.DTOs;

namespace HStore.Application.DTOs;

public class CreateProductDto
{
    public LocalizedProperty Name { get; set; } = new();
    public LocalizedProperty Description { get; set; } = new();
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public double Weight { get; set; }
    public string WeightUnit { get; set; }
    public double Height { get; set; }
    public string HeightUnit { get; set; }
    public double Width { get; set; }
    public string WidthUnit { get; set; }
    public double Length { get; set; }
    public string LengthUnit { get; set; }
    public List<int> CategoryIds { get; set; } = [];
    public int SellerId { get; set; }
    public IEnumerable<ProductVariantDto> Variants { get; set; } = []; 
}
