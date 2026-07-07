using Catalog.Domain;
using SharedKernel;

namespace Catalog.Application.PriceDefinitions.DeletePriceDefinition;

/// <summary>Fiyat tanımı silme işlemini yürüten handler.</summary>
public sealed class DeletePriceDefinitionHandler(
    IPriceDefinitionRepository priceDefinitions,
    IUnitOfWork unitOfWork) : IDeletePriceDefinitionHandler
{
    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(
        DeletePriceDefinitionCommand command,
        CancellationToken cancellationToken = default)
    {
        var definition = await priceDefinitions.GetByIdAsync(command.Id, cancellationToken);
        if (definition is null)
        {
            return Result.Failure(Error.NotFound("Price definition not found."));
        }

        priceDefinitions.Remove(definition);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
