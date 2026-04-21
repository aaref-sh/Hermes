using FluentValidation;
using HStore.Application.DTOs;

namespace HStore.API.Validators;

public class TrackingInformationValidator : AbstractValidator<TrackingInformation>
{
    public TrackingInformationValidator()
    {
        RuleFor(x => x.TrackingNumber)
            .NotEmpty().WithMessage("Tracking number is required.");

        RuleFor(x => x.Carrier)
            .NotEmpty().WithMessage("Carrier is required.");

        RuleFor(x => x.CurrentStatus)
            .NotEmpty().WithMessage("Current status is required.");
    }
}