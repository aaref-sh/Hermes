using System.ComponentModel.DataAnnotations;

namespace HStore.Application.DTOs;

public class FilterParams
{
    public string? Name { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
    public int PageNumber { get; set; } = 1;
    
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 20;
}
