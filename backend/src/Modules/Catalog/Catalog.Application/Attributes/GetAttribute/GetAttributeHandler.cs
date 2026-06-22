using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.Attributes.GetAttribute;

/// <summary>Tek bir özniteliği kimliğine göre getirir.</summary>
public sealed class GetAttributeHandler(IAttributeRepository attributes) : IGetAttributeHandler
{
    /// <inheritdoc/>
    public async Task<Result<AttributeDto>> ExecuteAsync(
        GetAttributeQuery query,
        CancellationToken cancellationToken = default)
    {
        var attribute = await attributes.GetByIdAsync(query.Id, cancellationToken);
        return attribute is null
            ? Result.Failure<AttributeDto>(Error.NotFound("Attribute not found."))
            : Result.Success(attribute.ToDto());
    }
}
