using Catalog.Api.Requests;
using Catalog.Application.Categories.AssignCategoryAttribute;
using Catalog.Application.Categories.CreateCategory;
using Catalog.Application.Categories.DeleteCategory;
using Catalog.Application.Categories.GetCategory;
using Catalog.Application.Categories.ListCategories;
using Catalog.Application.Categories.ListCategoryAttributes;
using Catalog.Application.Categories.RemoveCategoryAttribute;
using Catalog.Application.Categories.UpdateCategory;
using Catalog.Application.Categories.UpdateCategoryAttribute;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Api;

/// <summary>Kategori ve kategori-öznitelik eşlemesi endpoint'lerini tanımlar.</summary>
internal static class CategoryEndpoints
{
    internal static void MapCategoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/categories", async (CreateCategoryRequest request, ICreateCategoryHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new CreateCategoryCommand(request.Name, request.Code, request.ParentId));
            return result.ToCreatedResult(dto => $"/api/v1/catalog/categories/{dto.Id}");
        });

        group.MapGet("/categories", async (
            IListCategoriesHandler handler,
            int page = 0,
            int page_size = 0) =>
        {
            var result = await handler.ExecuteAsync(new ListCategoriesQuery(page, page_size));
            return result.ToHttpResult();
        });

        group.MapGet("/categories/{id:guid}", async (Guid id, IGetCategoryHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new GetCategoryQuery(id));
            return result.ToHttpResult();
        });

        group.MapPatch("/categories/{id:guid}", async (Guid id, UpdateCategoryRequest request, IUpdateCategoryHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateCategoryCommand(id, request.Name, request.Code, request.ParentId));
            return result.ToHttpResult();
        });

        group.MapDelete("/categories/{id:guid}", async (Guid id, IDeleteCategoryHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new DeleteCategoryCommand(id));
            return result.ToHttpResult();
        });

        group.MapPost("/categories/{id:guid}/attributes", async (
            Guid id,
            AssignCategoryAttributeRequest request,
            IAssignCategoryAttributeHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new AssignCategoryAttributeCommand(
                id,
                request.AttributeId,
                request.Required,
                request.MarketplaceRequired,
                request.SortOrder));
            return result.ToCreatedResult(dto => $"/api/v1/catalog/category-attributes/{dto.CategoryAttributeId}");
        });

        group.MapGet("/categories/{id:guid}/attributes", async (
            Guid id,
            IListCategoryAttributesHandler handler,
            int page = 0,
            int page_size = 0) =>
        {
            var result = await handler.ExecuteAsync(new ListCategoryAttributesQuery(id, page, page_size));
            return result.ToHttpResult();
        });

        group.MapPatch("/category-attributes/{id:guid}", async (
            Guid id,
            UpdateCategoryAttributeRequest request,
            IUpdateCategoryAttributeHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new UpdateCategoryAttributeCommand(
                id,
                request.Required,
                request.MarketplaceRequired,
                request.SortOrder));
            return result.ToHttpResult();
        });

        group.MapDelete("/category-attributes/{id:guid}", async (Guid id, IRemoveCategoryAttributeHandler handler) =>
        {
            var result = await handler.ExecuteAsync(new RemoveCategoryAttributeCommand(id));
            return result.ToHttpResult();
        });
    }
}
