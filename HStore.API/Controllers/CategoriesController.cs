using HStore.API.Attributes;
using HStore.Application.DTOs;
using HStore.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HStore.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController(ICategoryService categoryService) : ControllerBaseEx
{
    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await categoryService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var category = await categoryService.GetCategoryByIdAsync(id);
        return category == null ? NotFound() : Ok(category);
    }

    [HttpGet("{id:int}/subcategories")]
    public async Task<IActionResult> GetSubcategories(int id)
    {
        var subcategories = await categoryService.GetSubcategoriesAsync(id);
        return Ok(subcategories);
    }

    [HttpGet("All")]
    public async Task<IActionResult> GetCategoriesWithFilter([FromQuery] FilterParams filter)
    {
        var result = await categoryService.GetWithFilterAsync(filter);
        return Ok(result);
    }

    [AuthorizeMiddleware(["Admin"])]
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto categoryDto)
    {
        var newCategory = await categoryService.CreateCategoryAsync(categoryDto);
        return newCategory != null
            ? CreatedAtAction(nameof(GetCategoryById), new { id = newCategory.Id }, newCategory)
            : BadRequest("Failed to create category");
    }

    [AuthorizeMiddleware(["Admin"])]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto categoryDto)
    {
        if (categoryDto.Id != id)
        {
            return BadRequest("Category ID in request body does not match ID in URL.");
        }

        await categoryService.UpdateCategoryAsync(id, categoryDto);
        return NoContent();
    }

    [AuthorizeMiddleware(["Admin"])]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await categoryService.DeleteCategoryAsync(id);
        return NoContent();
    }
}
