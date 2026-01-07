# Dev Run Guide

This repo hosts:
- **ICS API**: ASP.NET Core Web API (IIS path `/ics-api`)
- **ICS Admin Interface**: Next.js static export (IIS path `/ics-admin`)

This is the local dev workflow that keeps URLs consistent with the IIS sub-path deployment.

---

## Prereqs

- .NET SDK 8.x
- Node.js + npm
- Entra app registrations set up per [docs/implementation-guide.md](docs/implementation-guide.md)

---

## 1) Run the API

From the repo root:

- `cd src/api/IcsApi`
- `dotnet run`

In Development, the API enables `PathBase=/ics-api`, so the health endpoint is:

- `https://localhost:7296/ics-api/health`

Swagger is also under:

- `https://localhost:7296/ics-api/swagger`

Notes:
- Update Entra placeholders in [src/api/IcsApi/appsettings.json](src/api/IcsApi/appsettings.json) (`AzureAd.TenantId`, `AzureAd.ClientId`).
- In IIS, do **not** enable `Ics:UsePathBase` (IIS will supply the path base).

---

## 2) Run the Admin UI

From the repo root:

- `cd src/admin/ics-admin`
- Copy `.env.local.example` to `.env.local` and set values
- `npm run dev`

Open:

- `http://localhost:3000/ics-admin/`

---

## 3) Static export build (for IIS)

From `src/admin/ics-admin`:

- `npm run build`

This produces a static export in `out/` that is intended to be served by IIS as `/ics-admin`.

---

## Troubleshooting

- If you sign in but see "Not authorized", ensure your user has the `Admin` app role assignment on the **ICS API** Enterprise Application.
- If API calls fail with `401`, verify the SPA is requesting the correct API scope: `api://API_APP_CLIENT_ID/access_as_user`.
