using FluentValidation;
using HStore.Application.DTOs;

namespace HStore.API.Validators;

public class JwtTokenDtoValidator : AbstractValidator<JwtTokenDto>
{
    public JwtTokenDtoValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("Access token is required.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}