package pimhttp

import (
	"errors"
	"net/http"
	"path"
	"path/filepath"
	"strings"

	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"

	"github.com/kazimcavus/pimly/internal/platform/db/tenantdb"
	"github.com/kazimcavus/pimly/internal/platform/httpx"
	"github.com/kazimcavus/pimly/internal/platform/tenant"
	"github.com/kazimcavus/pimly/internal/shared/apperr"
)

const maxUploadBytes = 32 << 20 // 32 MiB per request part

// UploadProductMedia stores a single image and attaches it to a product.
func (h *Handler) UploadProductMedia(w http.ResponseWriter, r *http.Request) {
	productID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	t, ok := h.requireStorage(w, r)
	if !ok {
		return
	}
	if err := r.ParseMultipartForm(maxUploadBytes); err != nil {
		httpx.Error(w, r, apperr.Validation("invalid multipart form"))
		return
	}
	file, header, err := r.FormFile("file")
	if err != nil {
		httpx.Error(w, r, apperr.Validation("file is required"))
		return
	}
	defer file.Close()

	// Resolve the product (and its SKU for the object key).
	prod, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Product, error) {
		return q.GetProduct(r.Context(), productID)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}

	url, err := h.storage.Upload(r.Context(),
		objectKey(t.SchemaName, prod.ProductSku, header.Filename),
		file, header.Size, header.Header.Get("Content-Type"))
	if err != nil {
		httpx.Error(w, r, apperr.Internal(err))
		return
	}

	media, err := h.insertMedia(r, productID, nil, url, r.FormValue("alt_text"))
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, media)
}

// UploadVariantMedia attaches an image as a (rare) variant-level override.
func (h *Handler) UploadVariantMedia(w http.ResponseWriter, r *http.Request) {
	variantID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	t, ok := h.requireStorage(w, r)
	if !ok {
		return
	}
	if err := r.ParseMultipartForm(maxUploadBytes); err != nil {
		httpx.Error(w, r, apperr.Validation("invalid multipart form"))
		return
	}
	file, header, err := r.FormFile("file")
	if err != nil {
		httpx.Error(w, r, apperr.Validation("file is required"))
		return
	}
	defer file.Close()

	// Resolve the variant → product for the object key and FK.
	variant, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Variant, error) {
		return q.GetVariant(r.Context(), variantID)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}

	url, err := h.storage.Upload(r.Context(),
		objectKey(t.SchemaName, variant.Barcode, header.Filename),
		file, header.Size, header.Header.Get("Content-Type"))
	if err != nil {
		httpx.Error(w, r, apperr.Internal(err))
		return
	}

	media, err := h.insertMedia(r, variant.ProductID, &variantID, url, r.FormValue("alt_text"))
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusCreated, media)
}

// BulkUploadMedia matches each file by name (filename without extension =
// product_sku) and attaches it to that product. Files whose SKU is unknown are
// reported as skipped rather than failing the whole request.
func (h *Handler) BulkUploadMedia(w http.ResponseWriter, r *http.Request) {
	t, ok := h.requireStorage(w, r)
	if !ok {
		return
	}
	if err := r.ParseMultipartForm(maxUploadBytes); err != nil {
		httpx.Error(w, r, apperr.Validation("invalid multipart form"))
		return
	}
	if r.MultipartForm == nil || len(r.MultipartForm.File["files"]) == 0 {
		httpx.Error(w, r, apperr.Validation("at least one file is required under 'files'"))
		return
	}

	type attached struct {
		Sku      string    `json:"product_sku"`
		MediaID  uuid.UUID `json:"media_id"`
		Filename string    `json:"filename"`
	}
	type skipped struct {
		Filename string `json:"filename"`
		Reason   string `json:"reason"`
	}
	var (
		ok2   []attached
		skips []skipped
	)

	for _, fh := range r.MultipartForm.File["files"] {
		sku := skuFromFilename(fh.Filename)
		prod, err := inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Product, error) {
			return q.GetProductBySku(r.Context(), sku)
		})
		if errors.Is(err, pgx.ErrNoRows) {
			skips = append(skips, skipped{Filename: fh.Filename, Reason: "no product with sku " + sku})
			continue
		} else if err != nil {
			httpx.Error(w, r, dbErr(err))
			return
		}

		f, err := fh.Open()
		if err != nil {
			skips = append(skips, skipped{Filename: fh.Filename, Reason: "cannot read file"})
			continue
		}
		url, err := h.storage.Upload(r.Context(), objectKey(t.SchemaName, sku, fh.Filename), f, fh.Size, fh.Header.Get("Content-Type"))
		f.Close()
		if err != nil {
			httpx.Error(w, r, apperr.Internal(err))
			return
		}
		media, err := h.insertMedia(r, prod.ID, nil, url, "")
		if err != nil {
			httpx.Error(w, r, dbErr(err))
			return
		}
		ok2 = append(ok2, attached{Sku: sku, MediaID: media.ID, Filename: fh.Filename})
	}

	httpx.JSON(w, http.StatusOK, map[string]any{"attached": ok2, "skipped": skips})
}

// ListProductMedia returns a product's media (variants inherit product media).
func (h *Handler) ListProductMedia(w http.ResponseWriter, r *http.Request) {
	productID, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	media, err := inTenant(h, r, func(q *tenantdb.Queries) ([]tenantdb.Medium, error) {
		return q.ListMediaByProduct(r.Context(), productID)
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	httpx.JSON(w, http.StatusOK, media)
}

// DeleteMedia removes a media row and best-effort deletes the object.
func (h *Handler) DeleteMedia(w http.ResponseWriter, r *http.Request) {
	id, err := pathUUID(r, "id")
	if err != nil {
		httpx.Error(w, r, err)
		return
	}
	url, err := inTenant(h, r, func(q *tenantdb.Queries) (string, error) {
		m, err := q.GetMedia(r.Context(), id)
		if err != nil {
			return "", err
		}
		if _, err := q.DeleteMedia(r.Context(), id); err != nil {
			return "", err
		}
		return m.Url, nil
	})
	if err != nil {
		httpx.Error(w, r, dbErr(err))
		return
	}
	if h.storage != nil {
		_ = h.storage.DeleteByURL(r.Context(), url)
	}
	w.WriteHeader(http.StatusNoContent)
}

// --- helpers ---

func (h *Handler) requireStorage(w http.ResponseWriter, r *http.Request) (tenant.Tenant, bool) {
	t, ok := tenant.FromContext(r.Context())
	if !ok {
		httpx.Error(w, r, apperr.Unauthorized("no tenant in context"))
		return tenant.Tenant{}, false
	}
	if h.storage == nil {
		httpx.Error(w, r, apperr.Internal(errors.New("media storage is not configured")))
		return tenant.Tenant{}, false
	}
	return t, true
}

func (h *Handler) insertMedia(r *http.Request, productID uuid.UUID, variantID *uuid.UUID, url, altText string) (tenantdb.Medium, error) {
	return inTenant(h, r, func(q *tenantdb.Queries) (tenantdb.Medium, error) {
		sort, err := q.NextMediaSortOrder(r.Context(), productID)
		if err != nil {
			return tenantdb.Medium{}, err
		}
		return q.CreateMedia(r.Context(), tenantdb.CreateMediaParams{
			ProductID: productID, VariantID: variantID, Url: url, AltText: altText, SortOrder: sort,
		})
	})
}

// objectKey builds a tenant-scoped object key: <schema>/<group>/<rand>-<file>.
func objectKey(schema, group, filename string) string {
	safe := sanitizeFilename(filename)
	return path.Join(schema, group, uuid.NewString()[:8]+"-"+safe)
}

func sanitizeFilename(name string) string {
	name = filepath.Base(name)
	name = strings.ReplaceAll(name, " ", "_")
	return name
}

// skuFromFilename returns the filename without its extension.
func skuFromFilename(filename string) string {
	base := filepath.Base(filename)
	return strings.TrimSuffix(base, filepath.Ext(base))
}
