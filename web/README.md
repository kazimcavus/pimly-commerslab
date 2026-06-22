# pimly — web (admin UI)

React + Vite admin interface for the pimly PIM backend. The visual design is a
faithful implementation of the **pimly Design System** handoff from Claude
Design (tokens + primitives under `src/ds/`, screens under `src/screens/`),
wired to the live API.

> **Backend:** the API is now the **.NET** backend under [`../backend`](../backend)
> (ASP.NET Core, listens on `:7000`). The legacy Go backend is being retired.

## Run (local dev)

Backend first (from the repo root):

```bash
docker compose up -d                       # postgres (+ minio)

cd backend
dotnet run --project src/Pimly.Api         # http://localhost:7000 (Swagger at /swagger)
```

You need a user to sign in with. Auth lives in the Identity module
(`POST /api/v1/identity/login`); seed/create a user via the backend (see
[`../backend/README.md`](../backend/README.md)).

Then the frontend:

```bash
cd web
npm install
npm run dev        # http://localhost:5173
```

Open http://localhost:5173 and sign in. The top-bar sun/moon button toggles
light/dark.

## How it connects

- `vite.config.js` proxies `/api/*` → the .NET backend on `:7000` (override with
  `PIMLY_API_TARGET`), forwarding the `/api/v1/...` prefix as-is, so the browser
  stays same-origin (no CORS).
- `src/lib/api.js` is the API client. It targets the versioned module prefixes
  (`/api/v1/identity`, `/api/v1/catalog`), sends the JWT bearer token, and decodes
  the RFC 7807 `ProblemDetails` error shape returned by the API.
- The wire format is **snake_case** in both directions (the .NET host is
  configured with a snake_case JSON naming policy), matching the client.
- `src/ds/` are the design-system primitives (verbatim from the handoff);
  `src/styles/` are the design tokens (CSS custom properties) + the UI-kit CSS.

## Migration status (Go → .NET)

The frontend has been repointed to the .NET backend. Endpoint coverage so far:

| Area | Status |
|---|---|
| Auth — login, me | ✅ wired (`/api/v1/identity`) |
| Categories, Category attributes | ✅ wired (`/api/v1/catalog`) |
| Attributes (+ values) | ✅ wired |
| Variant types & values | ✅ wired (`/variant-types` → `/variants`) |
| Products create (`products:batch`) | ⚠️ endpoint wired, **payload model differs** — see below |
| Settings, MetaObjects, Media, Admin, Groups | ⏳ not yet on .NET — calls reject with a clear "not migrated" error (`code: not_migrated`) |

**Product model difference:** the frontend speaks *groups → products → variants*;
the .NET Catalog speaks *products → items* and `products:batch` expects an
existing `group_id`. Reconciling this is the next migration step, after which
ProductBuilder save and the Group/Product list screens will work.

Screens depending on not-yet-migrated endpoints are expected to surface the
"henüz .NET backend'e taşınmadı" error until those modules land.

## Build

```bash
npm run build      # → dist/
```
