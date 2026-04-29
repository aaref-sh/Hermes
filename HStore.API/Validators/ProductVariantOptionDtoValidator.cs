using FluentValidation;
using HStore.Application.DTOs;

namespace HStore.API.Validators;

public class ProductVariantOptionDtoValidator : AbstractValidator<ProductVariantOptionDto>
{
    public ProductVariantOptionDtoValidator()
    {
RuleFor(x => x.Name.En)
            .NotEmpty().WithMessage("Option name (English) is required.")
            .MaximumLength(50).WithMessage("Option name cannot exceed 50 characters.")
        .When(x => string.IsNullOrEmpty(x.Name.Ar));

        RuleFor(x => x.Name.Ar)
            .MaximumLength(50).WithMessage("Option name (Arabic) cannot exceed 50 characters.");

        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Attribute value is required.")
            .MaximumLength(100).WithMessage("Attribute value cannot exceed 100 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid option type.");
    }
}