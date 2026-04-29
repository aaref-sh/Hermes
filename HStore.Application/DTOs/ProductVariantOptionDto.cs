using HStore.Domain.Classes;
using HStore.Domain.Enums;

namespace HStore.Application.DTOs;

public class ProductVariantOptionDto 
{
    public LocalizedProperty Name { get; set; } = new();
    public string Value { get; set; }
    public VariantOptionType Type { get; set; }
}
