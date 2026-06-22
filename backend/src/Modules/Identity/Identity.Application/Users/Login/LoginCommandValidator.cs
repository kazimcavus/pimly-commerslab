using FluentValidation;
using Identity.Application.Validation;

namespace Identity.Application.Users.Login;

/// <summary>LoginCommand için doğrulama kuralları.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommandValidator"/> class.
    /// </summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Password).Password();
    }
}
