using FluentValidation;

namespace Channels.Application.AttributeChannelMappings.ResolveAttributeValueChannelMapping;

/// <summary>ResolveAttributeValueChannelMappingQuery doğrulama kuralları.</summary>
public sealed class ResolveAttributeValueChannelMappingQueryValidator
    : AbstractValidator<ResolveAttributeValueChannelMappingQuery>
{
    public ResolveAttributeValueChannelMappingQueryValidator()
    {
        RuleFor(x => x.AttributeChannelMappingId).NotEmpty();
        RuleFor(x => x.CatalogValueId).NotEmpty();
    }
}
