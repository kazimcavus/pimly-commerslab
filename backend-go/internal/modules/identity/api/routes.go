// Package api, Identity modülünün HTTP uçlarını kaydeder
// (.NET Identity.Api.IdentityEndpoints karşılığı):
//
//	POST /api/v1/identity/login    (anonim)
//	POST /api/v1/identity/register (anonim)
//	GET  /api/v1/identity/me       (JWT zorunlu)
package api

import (
	"net/http"

	"github.com/go-chi/chi/v5"
	"github.com/google/uuid"

	"pimly.commerslab/backend-go/internal/modules/identity/application"
	"pimly.commerslab/backend-go/internal/platform/httpx"
	"pimly.commerslab/backend-go/internal/sharedkernel/tenancy"
)

// Handlers, rotaların ihtiyaç duyduğu kullanım senaryosu handler'larını taşır.
type Handlers struct {
	Login    *application.LoginHandler
	Register *application.RegisterUserHandler
	GetMe    *application.GetMeHandler
}

// loginRequest, POST /login isteğinin kablo biçimidir.
type loginRequest struct {
	Email    string `json:"email"`
	Password string `json:"password"`
}

// registerRequest, POST /register isteğinin kablo biçimidir; tenant_name
// gönderilmeyebilir (nil) veya boş olabilir — ikisi farklı doğrulanır.
type registerRequest struct {
	Email      string  `json:"email"`
	Password   string  `json:"password"`
	Name       string  `json:"name"`
	TenantName *string `json:"tenant_name"`
}

// Mount, Identity rotalarını verilen router'a /api/v1/identity öneki altında
// kaydeder. authMiddleware yalnızca /me ucuna uygulanır (.NET'te grubun geneli
// anonim, /me RequireAuthorization).
func Mount(r chi.Router, h Handlers, authMiddleware func(http.Handler) http.Handler) {
	r.Route("/api/v1/identity", func(g chi.Router) {
		g.Post("/login", func(w http.ResponseWriter, req *http.Request) {
			body, derr := httpx.DecodeJSON[loginRequest](req)
			if derr != nil {
				httpx.WriteProblem(w, req, derr)
				return
			}
			result := h.Login.Execute(req.Context(), application.LoginCommand{
				Email:    body.Email,
				Password: body.Password,
			})
			httpx.WriteOK(w, req, result)
		})

		g.Post("/register", func(w http.ResponseWriter, req *http.Request) {
			body, derr := httpx.DecodeJSON[registerRequest](req)
			if derr != nil {
				httpx.WriteProblem(w, req, derr)
				return
			}
			result := h.Register.Execute(req.Context(), application.RegisterUserCommand{
				Email:      body.Email,
				Password:   body.Password,
				Name:       body.Name,
				TenantName: body.TenantName,
			})
			httpx.WriteCreated(w, req, result, func(application.LoginResult) string {
				return "/api/v1/identity/me"
			})
		})

		g.Group(func(protected chi.Router) {
			protected.Use(authMiddleware)
			protected.Get("/me", func(w http.ResponseWriter, req *http.Request) {
				userID, ok := httpx.AuthUserID(req.Context())
				tenantID, tok := tenancy.FromContext(req.Context())
				if !ok || !tok || userID == uuid.Nil {
					w.WriteHeader(http.StatusUnauthorized)
					return
				}
				result := h.GetMe.Execute(req.Context(), application.GetMeQuery{
					UserID:   userID,
					TenantID: tenantID,
				})
				httpx.WriteOK(w, req, result)
			})
		})
	})
}
