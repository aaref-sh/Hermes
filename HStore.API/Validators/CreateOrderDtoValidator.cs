using FluentValidation;
using HStore.Application.DTOs;
using HStore.Domain.Enums;

namespace HStore.API.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required.")
            .SetValidator(new AddressDtoValidator());

        RuleFor(x => x.BillingAddress)
            .NotNull().WithMessage("Billing address is required.")
            .SetValidator(new AddressDtoValidator()); 

        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .WithMessage("Invalid payment method. Valid values are: Card (0), PayOnDelivery (1), Wallet (2).");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required.");

        RuleFor(x => x.CodFee)
            .GreaterThanOrEqualTo(0).When(x => x.CodFee.HasValue)
            .WithMessage("COD fee must be non-negative.");

        RuleFor(x => x.CodFee)
            .Must((dto, codFee) => !codFee.HasValue || dto.PaymentMethod == PaymentMethodType.PayOnDelivery)
            .WithMessage("COD fee is only allowed for Pay-on-Delivery orders.");

        RuleForEach(x => x.OrderItems)
            .SetValidator(new OrderItemDtoValidator());
    }
}