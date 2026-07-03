using FluentValidation;
using Identity.Application.Validation;

namespace Identity.Application.Users.Register;

/// <summary>RegisterUserCommand için doğrulama kuralları.</summary>
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterUserCommandValidator"/> class.
    /// </summary>
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Password).RegisterPassword();
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Name));
        RuleFor(x => x.TenantName)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.TenantName is not null);
    }
}
