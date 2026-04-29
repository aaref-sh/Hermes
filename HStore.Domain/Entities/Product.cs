using HStore.Domain.Classes;
using HStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HStore.Domain.Entities;

public class Product : BaseEntity
{
    public LocalizedProperty Name { get; set; } = new();
    public LocalizedProperty Description { get; set; } = new();
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public double Weight { get; set; }
    public string? WeightUnit { get; set; }
    public double Height { get; set; }
    public string? HeightUnit { get; set; }
    public double Width { get; set; }
    public string? WidthUnit { get; set; }
    public double Length { get; set; }
    public string? LengthUnit { get; set; }
    public List<string> Tags { get; set; } = [];
    public HostedAt HostedAt { get; set; }

    // Navigation Properties
    public ICollection<Category> Categories { get; set; } = [];
    public int SellerId { get; set; }
    public User Seller { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductMedia> Medias { get; set; } = [];
}

[Owned]
public class ProductMedia
{
    public string MediaId { get; set; }
    public ProductMediaType MediaType { get; set; }
}
