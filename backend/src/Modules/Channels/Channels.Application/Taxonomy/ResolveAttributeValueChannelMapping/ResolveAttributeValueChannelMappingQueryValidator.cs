using FluentValidation;

namespace Channels.Application.Taxonomy.ResolveAttributeValueChannelMapping;

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
