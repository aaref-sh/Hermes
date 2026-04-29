using FluentValidation;
using HStore.Application.DTOs;

namespace HStore.API.Validators;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
RuleFor(x => x.Name.En)
            .NotEmpty().WithMessage("Category name (English) is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.")
        .When(x => string.IsNullOrEmpty(x.Name.Ar));

        RuleFor(x => x.Name.Ar)
            .MaximumLength(100).WithMessage("Category name (Arabic) cannot exceed 100 characters.");

        RuleFor(x => x.Description.En)
            .MaximumLength(500).WithMessage("Category description (English) cannot exceed 500 characters.")
        .When(x => string.IsNullOrEmpty(x.Description.Ar));

        RuleFor(x => x.Description.Ar)
            .MaximumLength(500).WithMessage("Category description (Arabic) cannot exceed 500 characters.");
    }
}