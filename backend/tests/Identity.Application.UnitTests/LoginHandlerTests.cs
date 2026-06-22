using FluentAssertions;
using FluentValidation;
using Identity.Application.Auth;
using Identity.Application.Contracts;
using Identity.Application.Users.Login;
using Identity.Domain;
using Identity.Domain.Users;
using Moq;
using SharedKernel;

namespace Identity.Application.UnitTests;

/// <summary>LoginHandler için birim testleri.</summary>
public class LoginHandlerTests
{
    private readonly Mock<IValidator<LoginCommand>> _validator = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordService> _passwords = new();
    private readonly Mock<ITokenService> _tokens = new();

    [Fact]
    public async Task ExecuteAsync_InvalidPassword_ReturnsUnauthorized()
    {
        var user = User.Create("user@example.com", "hash").Value;
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _users
            .Setup(u => u.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwords
            .Setup(p => p.VerifyPassword(user, "wrong", user.PasswordHash))
            .Returns(false);

        var handler = new LoginHandler(
            _validator.Object,
            _users.Object,
            _passwords.Object,
            _tokens.Object);

        var result = await handler.ExecuteAsync(new LoginCommand("user@example.com", "wrong"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Unauthorized);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCredentials_ReturnsToken()
    {
        var user = User.Create("user@example.com", "hash").Value;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _users
            .Setup(u => u.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwords
            .Setup(p => p.VerifyPassword(user, "secret", user.PasswordHash))
            .Returns(true);
        _tokens
            .Setup(t => t.GenerateToken(user))
            .Returns(("jwt-token", expiresAt));

        var handler = new LoginHandler(
            _validator.Object,
            _users.Object,
            _passwords.Object,
            _tokens.Object);

        var result = await handler.ExecuteAsync(new LoginCommand("user@example.com", "secret"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("jwt-token");
        result.Value.User.Email.Should().Be("user@example.com");
    }
}
