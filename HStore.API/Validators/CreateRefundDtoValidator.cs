using FluentValidation;
using HStore.Application.DTOs;

namespace HStore.API.Validators;

public class CreateRefundDtoValidator : AbstractValidator<CreateRefundDto>
{
    public CreateRefundDtoValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("Order ID is required.");

        RuleFor(x => x.PaymentIntentId)
            .NotEmpty().WithMessage("Payment Intent ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Refund amount must be non-negative.");
    }
}