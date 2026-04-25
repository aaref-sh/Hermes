namespace HStore.Application.DTOs;

using HStore.Domain.Enums;

public class ProductVariantOptionDto 
{
    public string Name { get; set; }
    public string Value { get; set; }
    public VariantOptionType Type { get; set; }
}
