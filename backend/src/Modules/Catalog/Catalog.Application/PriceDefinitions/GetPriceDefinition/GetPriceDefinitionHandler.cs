using Catalog.Application.Contracts;
using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.PriceDefinitions.GetPriceDefinition;

/// <summary>Fiyat tanımı getirme işlemini yürüten handler.</summary>
public sealed class GetPriceDefinitionHandler(IPriceDefinitionRepository priceDefinitions) : IGetPriceDefinitionHandler
{
    /// <inheritdoc/>
    public async Task<Result<PriceDefinitionDto>> ExecuteAsync(
        GetPriceDefinitionQuery query,
        CancellationToken cancellationToken = default)
    {
        var definition = await priceDefinitions.GetByIdAsync(query.Id, cancellationToken);
        return definition is null
            ? Result.Failure<PriceDefinitionDto>(Error.NotFound("Price definition not found."))
            : Result.Success(definition.ToDto());
    }
}
