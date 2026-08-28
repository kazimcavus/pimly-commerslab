// Package api, Media modülünün HTTP ucunu kaydeder (.NET Media.Api karşılığı):
//
//	POST /api/v1/media/uploads?purpose=product|swatch — multipart "file" alanı,
//	yanıt: { url, content_type, size_bytes }
package api

import (
	"io"
	"net/http"

	"github.com/go-chi/chi/v5"

	"pimly.commerslab/backend-go/internal/modules/media/application"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/sharedkernel"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// maxUploadMemory, multipart ayrıştırmada bellekte tutulacak azami boyuttur;
// aşan kısım geçici dosyaya taşar.
const maxUploadMemory = 8 << 20

// Mount, Media rotasını kaydeder; grup JWT zorunludur.
func Mount(r chi.Router, h *application.UploadHandlers, authMiddleware func(http.Handler) http.Handler) {
	r.Route("/api/v1/media", func(g chi.Router) {
		g.Use(authMiddleware)

		g.Post("/uploads", func(w http.ResponseWriter, r *http.Request) {
			if err := r.ParseMultipartForm(maxUploadMemory); err != nil {
				httpx.WriteProblem(w, r, sharedkernel.NewValidationError("File is required."))
				return
			}
			file, _, err := r.FormFile("file")
			if err != nil {
				httpx.WriteProblem(w, r, sharedkernel.NewValidationError("File is required."))
				return
			}
			defer file.Close()

			content, err := io.ReadAll(file)
			if err != nil || len(content) == 0 {
				httpx.WriteProblem(w, r, sharedkernel.NewValidationError("File is required."))
				return
			}

			purpose := application.ParsePurpose(r.URL.Query().Get("purpose"))
			result := h.Upload(r.Context(), tenancy.MustFromContext(r.Context()), content, purpose)
			httpx.WriteOK(w, r, result)
		})
	})
}
