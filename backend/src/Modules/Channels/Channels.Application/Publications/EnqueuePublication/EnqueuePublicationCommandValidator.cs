using Channels.Application.Validation;
using FluentValidation;

namespace Channels.Application.Publications.EnqueuePublication;

/// <summary>EnqueuePublicationCommand için doğrulama kuralları.</summary>
public sealed class EnqueuePublicationCommandValidator : AbstractValidator<EnqueuePublicationCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnqueuePublicationCommandValidator"/> class.
    /// </summary>
    public EnqueuePublicationCommandValidator()
    {
        RuleFor(x => x.MarketplaceCode).MarketplaceCode();
    }
}
