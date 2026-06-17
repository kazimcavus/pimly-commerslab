# pimly — web (admin UI)

React + Vite admin interface for the pimly PIM backend. The visual design is a
faithful implementation of the **pimly Design System** handoff from Claude
Design (tokens + primitives under `src/ds/`, screens under `src/screens/`),
wired to the live API.

## Run (local dev)

Backend first (from the repo root):

```bash
docker compose up -d                       # postgres + minio
PIMLY_JWT_SECRET=devsecret PIMLY_ADMIN_TOKEN=admintok ./bin/pimly serve
# create a tenant to log in with (once):
./bin/pimly create-tenant --name "Demo Co" --owner-email demo@demo.test \
  --owner-name Demo --owner-password demo1234
```

Then the frontend:

```bash
cd web
npm install
npm run dev        # http://localhost:5173
```

Open http://localhost:5173 and sign in with `demo@demo.test` / `demo1234`.
The top-bar sun/moon button toggles light/dark.

For the **Admin** screen, paste the admin token (`admintok` above) into the
token field to load applications/tenants and toggle module flags.

## How it connects

- `vite.config.js` proxies `/api/*` → the backend on `:8080` (override with
  `PIMLY_API_TARGET`), so the browser stays same-origin (no CORS).
- `src/lib/api.js` is the typed client; it sends the JWT bearer token and the
  `X-Admin-Token` header, and decodes the API error envelope.
- `src/ds/` are the design-system primitives (verbatim from the handoff);
  `src/styles/` are the design tokens (CSS custom properties) + the UI-kit CSS.

## Build

```bash
npm run build      # → dist/
```
