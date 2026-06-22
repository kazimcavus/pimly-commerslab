using Catalog.Domain;
using Catalog.Domain.Categories;

namespace Catalog.Application.Categories;

/// <summary>Kategori-özellik atamasını kimlik ile bulan yardımcı sınıf.</summary>
internal static class CategoryAssignmentLookup
{
    internal static async Task<Category?> FindByAssignmentIdAsync(
        ICategoryRepository categories,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        foreach (var summary in await categories.ListAsync(cancellationToken))
        {
            var loaded = await categories.GetByIdAsync(summary.Id, cancellationToken);
            if (loaded?.Assignments.Any(a => a.Id == assignmentId) == true)
            {
                return loaded;
            }
        }

        return null;
    }
}
