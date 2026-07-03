using FluentAssertions;
using FluentValidation;
using Identity.Application.Auth;
using Identity.Application.Contracts;
using Identity.Application.Users.Register;
using Identity.Domain;
using Identity.Domain.Tenants;
using Identity.Domain.Users;
using Moq;
using SharedKernel;

namespace Identity.Application.UnitTests;

/// <summary>RegisterUserHandler için birim testleri.</summary>
public class RegisterUserHandlerTests
{
    private readonly Mock<IValidator<RegisterUserCommand>> _validator = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ITenantRepository> _tenants = new();
    private readonly Mock<ITenantMembershipRepository> _memberships = new();
    private readonly Mock<IPasswordService> _passwords = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public async Task ExecuteAsync_CreatesUserTenantAndMembership()
    {
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _users
            .Setup(u => u.GetByEmailAsync("shop@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwords
            .Setup(p => p.HashPassword(It.IsAny<User>(), "secret1234"))
            .Returns("hashed-password");
        _tokens
            .Setup(t => t.GenerateToken(It.IsAny<User>(), It.IsAny<Tenant>()))
            .Returns(("jwt-token", DateTimeOffset.UtcNow.AddHours(1)));

        var handler = CreateHandler();

        var result = await handler.ExecuteAsync(new RegisterUserCommand(
            "shop@example.com",
            "secret1234",
            "Owner",
            "My Shop"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("jwt-token");
        result.Value.Tenant.Name.Should().Be("My Shop");

        _tenants.Verify(t => t.AddAsync(It.Is<Tenant>(tenant => tenant.Name == "My Shop"), It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(u => u.AddAsync(It.Is<User>(user => user.Email == "shop@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        _memberships.Verify(m => m.AddAsync(It.IsAny<TenantMembership>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailExists_ReturnsConflict()
    {
        var existingUser = User.Create("shop@example.com", "hash").Value;
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _users
            .Setup(u => u.GetByEmailAsync("shop@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var handler = CreateHandler();

        var result = await handler.ExecuteAsync(new RegisterUserCommand(
            "shop@example.com",
            "secret1234",
            "Owner",
            "My Shop"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Conflict);
    }

    private RegisterUserHandler CreateHandler() =>
        new(
            _validator.Object,
            _users.Object,
            _tenants.Object,
            _memberships.Object,
            _passwords.Object,
            _tokens.Object,
            _timeProvider,
            Mock.Of<IUnitOfWork>());
}
