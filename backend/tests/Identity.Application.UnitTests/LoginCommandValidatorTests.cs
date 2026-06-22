using FluentAssertions;
using Identity.Application.Users.Login;
using SharedKernel;

namespace Identity.Application.UnitTests;

/// <summary>LoginCommandValidator için smoke testleri.</summary>
public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyEmail_Fails()
    {
        var result = _validator.Validate(new LoginCommand("  ", "secret"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_EmptyPassword_Fails()
    {
        var result = _validator.Validate(new LoginCommand("user@example.com", "  "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new LoginCommand("user@example.com", "secret"));
        result.IsValid.Should().BeTrue();
    }
}
