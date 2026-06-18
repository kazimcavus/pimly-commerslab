package pimhttp

import "net/http"

// RegisterRoutes mounts the PIM module routes on mux, each wrapped with the
// provided middleware (authentication + tenant resolution).
func (h *Handler) RegisterRoutes(mux *http.ServeMux, wrap func(http.Handler) http.Handler) {
	route := func(method, pattern string, fn http.HandlerFunc) {
		mux.Handle(method+" "+pattern, wrap(fn))
	}

	// Categories
	route("POST", "/categories", h.CreateCategory)
	route("GET", "/categories", h.ListCategories)
	route("GET", "/categories/{id}", h.GetCategory)
	route("PATCH", "/categories/{id}", h.UpdateCategory)
	route("DELETE", "/categories/{id}", h.DeleteCategory)

	// Category ↔ attribute assignments
	route("POST", "/categories/{id}/attributes", h.AssignCategoryAttribute)
	route("GET", "/categories/{id}/attributes", h.ListCategoryAttributes)
	route("PATCH", "/category-attributes/{id}", h.UpdateCategoryAttribute)
	route("DELETE", "/category-attributes/{id}", h.DeleteCategoryAttribute)

	// Attributes
	route("POST", "/attributes", h.CreateAttribute)
	route("GET", "/attributes", h.ListAttributes)
	route("GET", "/attributes/{id}", h.GetAttribute)
	route("PATCH", "/attributes/{id}", h.UpdateAttribute)
	route("DELETE", "/attributes/{id}", h.DeleteAttribute)

	// Metaobject definitions
	route("POST", "/metaobject-definitions", h.CreateMetaobjectDefinition)
	route("GET", "/metaobject-definitions", h.ListMetaobjectDefinitions)
	route("GET", "/metaobject-definitions/{id}", h.GetMetaobjectDefinition)
	route("DELETE", "/metaobject-definitions/{id}", h.DeleteMetaobjectDefinition)

	// Metaobject fields
	route("POST", "/metaobject-definitions/{id}/fields", h.CreateMetaobjectField)
	route("GET", "/metaobject-definitions/{id}/fields", h.ListMetaobjectFields)
	route("DELETE", "/metaobject-fields/{id}", h.DeleteMetaobjectField)

	// Metaobject entries
	route("POST", "/metaobject-definitions/{id}/entries", h.CreateMetaobjectEntry)
	route("GET", "/metaobject-definitions/{id}/entries", h.ListMetaobjectEntries)
	route("GET", "/metaobject-entries/{id}", h.GetMetaobjectEntry)
	route("PATCH", "/metaobject-entries/{id}", h.UpdateMetaobjectEntry)
	route("DELETE", "/metaobject-entries/{id}", h.DeleteMetaobjectEntry)

	// Tenant settings (sku generator, barcode config, …)
	route("GET", "/settings", h.ListSettings)
	route("PUT", "/settings/{key}", h.PutSetting)

	// Variant types & values (option axes: Renk, Beden, Ölçü)
	route("POST", "/variant-types", h.CreateVariantType)
	route("GET", "/variant-types", h.ListVariantTypes)
	route("GET", "/variant-types/{id}", h.GetVariantType)
	route("PATCH", "/variant-types/{id}", h.UpdateVariantType)
	route("DELETE", "/variant-types/{id}", h.DeleteVariantType)
	route("POST", "/variant-types/{id}/values", h.CreateVariantValue)
	route("GET", "/variant-types/{id}/values", h.ListVariantValues)
	route("PATCH", "/variant-values/{id}", h.UpdateVariantValue)
	route("DELETE", "/variant-values/{id}", h.DeleteVariantValue)

	// Marketplace category map
	route("POST", "/categories/{id}/marketplace-category-map", h.UpsertMarketplaceCategoryMap)
	route("GET", "/categories/{id}/marketplace-category-map", h.ListMarketplaceCategoryMaps)
	route("DELETE", "/marketplace-category-map/{id}", h.DeleteMarketplaceCategoryMap)

	// Marketplace attribute map
	route("POST", "/categories/{id}/marketplace-attribute-map", h.UpsertMarketplaceAttributeMap)
	route("GET", "/categories/{id}/marketplace-attribute-map", h.ListMarketplaceAttributeMaps)
	route("DELETE", "/marketplace-attribute-map/{id}", h.DeleteMarketplaceAttributeMap)

	// Products — single write path + groups/products/variants
	route("POST", "/products:batch", h.CreateProductsBatch)
	route("GET", "/groups", h.ListGroups)
	route("GET", "/groups/{id}", h.GetGroup)
	route("PATCH", "/groups/{id}", h.UpdateGroup)
	route("DELETE", "/groups/{id}", h.DeleteGroup)
	route("GET", "/products/{id}", h.GetProduct)
	route("PATCH", "/products/{id}", h.UpdateProduct)
	route("DELETE", "/products/{id}", h.DeleteProduct)
	route("GET", "/variants/{id}", h.GetVariant)
	route("PATCH", "/variants/{id}", h.UpdateVariant)
	route("DELETE", "/variants/{id}", h.DeleteVariant)

	// Media — product-level, with bulk-by-SKU and rare variant override
	route("POST", "/products/{id}/media", h.UploadProductMedia)
	route("GET", "/products/{id}/media", h.ListProductMedia)
	route("POST", "/variants/{id}/media", h.UploadVariantMedia)
	route("POST", "/media:bulk", h.BulkUploadMedia)
	route("POST", "/uploads", h.UploadImage)
	route("DELETE", "/media/{id}", h.DeleteMedia)
}
