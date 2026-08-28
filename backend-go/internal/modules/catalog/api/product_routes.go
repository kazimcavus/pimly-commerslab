package api

import (
	"net/http"

	"github.com/go-chi/chi/v5"
	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/catalog/application"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// productItemRequest, kalem oluşturma girdisinin kablo biçimidir.
type productItemRequest struct {
	Sku              *string                          `json:"sku"`
	Barcode          string                           `json:"barcode"`
	Gtin             *string                          `json:"gtin"`
	Mpn              *string                          `json:"mpn"`
	AxisValueEntryID *uuid.UUID                       `json:"axis_value_entry_id"`
	AxisValue        *string                          `json:"axis_value"`
	AttributeValues  []application.AttributeValueInput `json:"attribute_values"`
	VariantValues    []application.VariantValueInput   `json:"variant_values"`
}

// toItemInput, kablo kalemini komut girdisine çevirir.
func (r productItemRequest) toItemInput() application.CreateProductItemInput {
	return application.CreateProductItemInput{
		Sku: r.Sku, Barcode: r.Barcode, Gtin: r.Gtin, Mpn: r.Mpn,
		AxisValueEntryID: r.AxisValueEntryID, AxisValue: r.AxisValue,
		AttributeValues: r.AttributeValues, VariantValues: r.VariantValues,
	}
}

// createProductRequest, tek ürün oluşturma isteğinin kablo biçimidir.
type createProductRequest struct {
	GroupID         uuid.UUID                         `json:"group_id"`
	CategoryID      uuid.UUID                         `json:"category_id"`
	ModelCode       string                            `json:"model_code"`
	Name            string                            `json:"name"`
	Status          string                            `json:"status"`
	CodeInputs      []string                          `json:"code_inputs"`
	AttributeValues []application.AttributeValueInput `json:"attribute_values"`
	Variants        []application.VariantRefInput     `json:"variants"`
	Items           []productItemRequest              `json:"items"`
	BrandID         *uuid.UUID                        `json:"brand_id"`
	Description     *string                           `json:"description"`
}

// productSplitRequest, slicer değeri geçersiz kılmalarının kablo biçimidir.
type productSplitRequest struct {
	ValueName       string                            `json:"value_name"`
	ModelCode       *string                           `json:"model_code"`
	Name            *string                           `json:"name"`
	Description     *string                           `json:"description"`
	AttributeValues []application.AttributeValueInput `json:"attribute_values"`
}

// batchProductRequest, toplu istekteki tek ürün tanımının kablo biçimidir.
type batchProductRequest struct {
	CategoryID      uuid.UUID                         `json:"category_id"`
	ModelCode       string                            `json:"model_code"`
	Name            string                            `json:"name"`
	Status          string                            `json:"status"`
	CodeInputs      []string                          `json:"code_inputs"`
	AttributeValues []application.AttributeValueInput `json:"attribute_values"`
	Variants        []application.VariantRefInput     `json:"variants"`
	Items           []productItemRequest              `json:"items"`
	Splits          []productSplitRequest             `json:"splits"`
	BrandID         *uuid.UUID                        `json:"brand_id"`
	Description     *string                           `json:"description"`
}

// createProductsBatchRequest, toplu oluşturma isteğinin kablo biçimidir.
type createProductsBatchRequest struct {
	GroupID  uuid.UUID             `json:"group_id"`
	Products []batchProductRequest `json:"products"`
}

// updateProductRequest, ürün güncelleme isteğinin kablo biçimidir;
// attribute_values gönderilmezse mevcut değerler korunur.
type updateProductRequest struct {
	CategoryID      uuid.UUID                          `json:"category_id"`
	Name            string                             `json:"name"`
	Status          string                             `json:"status"`
	AttributeValues *[]application.AttributeValueInput `json:"attribute_values"`
	BrandID         *uuid.UUID                         `json:"brand_id"`
	Description     *string                            `json:"description"`
}

// itemsToInputs, kablo kalemlerini komut girdilerine çevirir.
func itemsToInputs(items []productItemRequest) []application.CreateProductItemInput {
	out := make([]application.CreateProductItemInput, len(items))
	for i, item := range items {
		out[i] = item.toItemInput()
	}
	return out
}

// mountProductRoutes, ürün uçlarını kaydeder (.NET ProductEndpoints karşılığı):
// POST /products, POST /products:batch, GET /products, GET/PATCH/DELETE /products/{id}.
func mountProductRoutes(g chi.Router, h *application.ProductHandlers) {
	g.Post("/products", func(w http.ResponseWriter, r *http.Request) {
		body, derr := httpx.DecodeJSON[createProductRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.Create(r.Context(), tenancy.MustFromContext(r.Context()), application.CreateProductCommand{
			GroupID: body.GroupID, CategoryID: body.CategoryID, ModelCode: body.ModelCode,
			Name: body.Name, Status: body.Status, CodeInputs: body.CodeInputs,
			Attributes: body.AttributeValues, Variants: body.Variants,
			Items: itemsToInputs(body.Items), BrandID: body.BrandID, Description: body.Description,
		})
		httpx.WriteCreated(w, r, result, func(dto application.ProductDto) string {
			return "/api/v1/catalog/products/" + dto.ID.String()
		})
	})

	g.Post("/products:batch", func(w http.ResponseWriter, r *http.Request) {
		body, derr := httpx.DecodeJSON[createProductsBatchRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		items := make([]application.CreateProductsBatchItem, len(body.Products))
		for i, product := range body.Products {
			splits := make([]application.BatchSplitInput, len(product.Splits))
			for j, split := range product.Splits {
				splits[j] = application.BatchSplitInput{
					ValueName: split.ValueName, ModelCode: split.ModelCode,
					Name: split.Name, Description: split.Description,
					AttributeValues: split.AttributeValues,
				}
			}
			items[i] = application.CreateProductsBatchItem{
				CategoryID: product.CategoryID, ModelCode: product.ModelCode,
				Name: product.Name, Status: product.Status, CodeInputs: product.CodeInputs,
				Attributes: product.AttributeValues, Variants: product.Variants,
				Items: itemsToInputs(product.Items), Splits: splits,
				BrandID: product.BrandID, Description: product.Description,
			}
		}
		result := h.CreateBatch(r.Context(), tenancy.MustFromContext(r.Context()),
			application.CreateProductsBatchCommand{
				GroupID: body.GroupID, Products: items, EnforceRequiredAttributes: true})
		httpx.WriteCreated(w, r, result, func(dto application.CreateProductsBatchResultDto) string {
			return "/api/v1/catalog/products/" + dto.Products[0].ID.String()
		})
	})

	g.Get("/products", func(w http.ResponseWriter, r *http.Request) {
		pr := httpx.QueryPagination(r)
		if pr.IsFailure() {
			httpx.WriteProblem(w, r, pr.Err())
			return
		}
		result := h.List(r.Context(), tenancy.MustFromContext(r.Context()),
			pr.Value().Page, pr.Value().PageSize)
		httpx.WriteOK(w, r, result)
	})

	g.Get("/products/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteOK(w, r, h.Get(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})

	g.Patch("/products/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[updateProductRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		var attributeValues []application.AttributeValueInput
		if body.AttributeValues != nil {
			attributeValues = *body.AttributeValues
			if attributeValues == nil {
				attributeValues = []application.AttributeValueInput{}
			}
		}
		result := h.Update(r.Context(), tenancy.MustFromContext(r.Context()), application.UpdateProductCommand{
			ID: id, CategoryID: body.CategoryID, Name: body.Name, Status: body.Status,
			Attributes: attributeValues, BrandID: body.BrandID, Description: body.Description,
		})
		httpx.WriteOK(w, r, result)
	})

	g.Delete("/products/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteResult(w, r, h.Delete(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})

	// Kalem uçları (.NET ProductItemEndpoints): GET /items/{id},
	// POST /products/{productId}/items, PATCH/DELETE /items/{id}.
	g.Get("/items/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteOK(w, r, h.GetItem(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})

	g.Post("/products/{id}/items", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[productItemRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.AddItem(r.Context(), tenancy.MustFromContext(r.Context()),
			application.AddProductItemCommand{ProductID: id, Item: body.toItemInput()})
		httpx.WriteCreated(w, r, result, func(dto application.ProductItemDto) string {
			return "/api/v1/catalog/items/" + dto.ID.String()
		})
	})

	g.Patch("/items/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		type updateItemRequest struct {
			Gtin             *string                            `json:"gtin"`
			Mpn              *string                            `json:"mpn"`
			AxisValueEntryID *uuid.UUID                         `json:"axis_value_entry_id"`
			AxisValue        *string                            `json:"axis_value"`
			AttributeValues  *[]application.AttributeValueInput `json:"attribute_values"`
			Sku              *string                            `json:"sku"`
			Barcode          *string                            `json:"barcode"`
		}
		body, derr := httpx.DecodeJSON[updateItemRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		var attributeValues []application.AttributeValueInput
		if body.AttributeValues != nil {
			attributeValues = *body.AttributeValues
			if attributeValues == nil {
				attributeValues = []application.AttributeValueInput{}
			}
		}
		result := h.UpdateItem(r.Context(), tenancy.MustFromContext(r.Context()),
			application.UpdateProductItemCommand{
				ID: id, Gtin: body.Gtin, Mpn: body.Mpn, AxisValueEntryID: body.AxisValueEntryID,
				AxisValue: body.AxisValue, Attributes: attributeValues,
				Sku: body.Sku, Barcode: body.Barcode,
			})
		httpx.WriteOK(w, r, result)
	})

	g.Delete("/items/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteResult(w, r, h.DeleteItem(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})

	// Görsel uçları (.NET ProductImageEndpoints): POST /products/{id}/images,
	// PATCH/DELETE /product-images/{id}.
	type imageRequest struct {
		URL            string     `json:"url"`
		SortOrder      int        `json:"sort_order"`
		AltText        *string    `json:"alt_text"`
		IsPrimary      bool       `json:"is_primary"`
		VariantValueID *uuid.UUID `json:"variant_value_id"`
	}

	g.Post("/products/{id}/images", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[imageRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.AddImage(r.Context(), tenancy.MustFromContext(r.Context()), application.ProductImageCommand{
			ProductID: id, URL: body.URL, SortOrder: body.SortOrder, AltText: body.AltText,
			IsPrimary: body.IsPrimary, VariantValueID: body.VariantValueID,
		})
		httpx.WriteCreated(w, r, result, func(dto application.ProductImageDto) string {
			return "/api/v1/catalog/product-images/" + dto.ID.String()
		})
	})

	g.Patch("/product-images/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		body, derr := httpx.DecodeJSON[imageRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.UpdateImage(r.Context(), tenancy.MustFromContext(r.Context()), application.ProductImageCommand{
			ImageID: id, URL: body.URL, SortOrder: body.SortOrder, AltText: body.AltText,
			IsPrimary: body.IsPrimary, VariantValueID: body.VariantValueID,
		})
		httpx.WriteOK(w, r, result)
	})

	g.Delete("/product-images/{id}", func(w http.ResponseWriter, r *http.Request) {
		id, ok := pathUUID(w, r, "id")
		if !ok {
			return
		}
		httpx.WriteResult(w, r, h.RemoveImage(r.Context(), tenancy.MustFromContext(r.Context()), id))
	})
}

// mountSkuGeneratorRoutes, SKU yapılandırma uçlarını kaydeder
// (.NET SkuGeneratorEndpoints karşılığı): GET/PUT /sku-config.
func mountSkuGeneratorRoutes(g chi.Router, h *application.SkuGeneratorHandlers) {
	g.Get("/sku-config", func(w http.ResponseWriter, r *http.Request) {
		httpx.WriteOK(w, r, h.GetConfig(r.Context(), tenancy.MustFromContext(r.Context())))
	})

	g.Put("/sku-config", func(w http.ResponseWriter, r *http.Request) {
		type updateRequest struct {
			Enabled          bool                        `json:"enabled"`
			Segments         []application.SkuSegmentDto `json:"segments"`
			CounterNextValue *int64                      `json:"counter_next_value"`
		}
		body, derr := httpx.DecodeJSON[updateRequest](r)
		if derr != nil {
			httpx.WriteProblem(w, r, derr)
			return
		}
		result := h.UpdateConfig(r.Context(), tenancy.MustFromContext(r.Context()),
			application.UpdateSkuGeneratorConfigCommand{
				Enabled: body.Enabled, Segments: body.Segments, CounterNextValue: body.CounterNextValue})
		httpx.WriteOK(w, r, result)
	})
}
