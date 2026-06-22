using Catalog.Domain;
using Catalog.Domain.Categories;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Catalog.Infrastructure.Repositories;

/// <summary>Kategori varlıkları için veritabanı erişim katmanı.</summary>
internal sealed class CategoryRepository(CatalogDbContext db) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await db.Categories
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Categories
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Category>> ListAsync(
        Pagination pagination,
        CancellationToken cancellationToken = default) =>
        await db.Categories
            .OrderBy(c => c.Name)
            .ToPagedResultAsync(pagination, cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetDescendantIdsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var all = await db.Categories
            .Select(c => new { c.Id, c.ParentId })
            .ToListAsync(cancellationToken);

        var childrenByParent = all
            .Where(c => c.ParentId.HasValue)
            .GroupBy(c => c.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var descendants = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        if (childrenByParent.TryGetValue(categoryId, out var direct))
        {
            foreach (var child in direct)
            {
                stack.Push(child);
            }
        }

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!descendants.Add(current))
            {
                continue;
            }

            if (childrenByParent.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    stack.Push(child);
                }
            }
        }

        return descendants;
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default) =>
        await db.Categories.AddAsync(category, cancellationToken);

    public void Update(Category category) => RepositoryExtensions.UpdateIfDetached(db, category);

    public void Remove(Category category) => db.Categories.Remove(category);
}
