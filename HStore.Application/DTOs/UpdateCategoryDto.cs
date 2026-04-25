namespace HStore.Application.DTOs;

public class UpdateCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public int? ParentCategoryId { get; set; }
}