using HStore.Domain.Classes;
using HStore.Application.DTOs;

namespace HStore.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public LocalizedProperty Name { get; set; } = new();
    public LocalizedProperty Description { get; set; } = new();
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public List<int> CategoryIds { get; set; } = [];
    public List<LocalizedProperty> CategoryNames { get; set; } = [];
    public int SellerId { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<ProductVariantDto> Variants { get; set; } = [];
    public List<ReviewDto> Reviews { get; set; } = [];
    public List<ProductMediaDto> Medias { get; set; } = [];
}
