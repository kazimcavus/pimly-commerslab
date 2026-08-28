// Package api, Catalog modülünün HTTP uçlarını /api/v1/catalog öneki altında
// kaydeder (.NET Catalog.Api.CatalogEndpoints karşılığı). Tüm uçlar JWT
// zorunludur; tenant kimliği auth middleware'inin koyduğu context'ten alınıp
// handler'lara açıkça geçirilir. Kaynak başına bir rota dosyası vardır
// (brand_routes.go, category_routes.go, ...); bu dosya yalnızca grubu kurar.
package api

import (
	"net/http"

	"github.com/go-chi/chi/v5"

	"pimly.commerslab/backend-go/internal/modules/catalog/application"
)

// Handlers, Catalog rotalarının ihtiyaç duyduğu kullanım senaryosu
// handler'larını taşır; fazlar ilerledikçe alanlar eklenir.
type Handlers struct {
	Brands       *application.BrandHandlers
	Categories   *application.CategoryHandlers
	Attributes   *application.AttributeHandlers
	Variants     *application.VariantHandlers
	Products     *application.ProductHandlers
	SkuGenerator *application.SkuGeneratorHandlers
}

// Mount, Catalog rotalarını verilen router'a kaydeder; authMiddleware tüm
// gruba uygulanır (.NET MapGroup(...).RequireAuthorization() karşılığı).
func Mount(r chi.Router, h Handlers, authMiddleware func(http.Handler) http.Handler) {
	r.Route("/api/v1/catalog", func(g chi.Router) {
		g.Use(authMiddleware)
		mountCategoryRoutes(g, h.Categories)
		mountBrandRoutes(g, h.Brands)
		mountAttributeRoutes(g, h.Attributes)
		mountVariantRoutes(g, h.Variants)
		mountProductRoutes(g, h.Products)
		mountSkuGeneratorRoutes(g, h.SkuGenerator)
	})
}
