# ICS System Architecture (Based on Your Answers)

This is a learning-first architecture for:
- **ICS API**: ASP.NET Core Web API on IIS
- **ICS Admin Interface**: Next.js **static export** (CSR/SSG) hosted as static files on IIS
- **AuthN/AuthZ**: Microsoft Entra ID (OIDC + OAuth2) with **app roles** (`Admin`, `Attorney`)

## 1) Deployment Topology (IIS)

**Single IIS host**: `https://nam-pdaparch01.americas.global-net.com`

Recommended simplest layout (same origin, avoids CORS complexity):
- **Admin UI** hosted at: `https://nam-pdaparch01.americas.global-net.com/ics-admin/`
- **API** hosted at: `https://nam-pdaparch01.americas.global-net.com/ics-api/`

In IIS terms:
- One site bound to `nam-pdaparch01...` (HTTPS)
- Two *applications* (or virtual directories):
  - `/ics-admin` = static files (Next export output)
  - `/ics-api` = ASP.NET Core app

> Note: A static site can’t truly “prevent” a non-Admin from downloading JS/CSS. The *real enforcement* is: (1) UI hides/blocks functionality and (2) API enforces roles on every protected endpoint.

## 2) Authentication and Authorization Model

### Token strategy
- **Admin UI** uses Entra ID sign-in (OIDC) and requests an **access token** for the ICS API.
- API validates the access token (JWT) and enforces authorization.

### Roles (single source of truth)
To avoid duplicating role definitions across apps:
- Define app roles **only on the ICS API app registration**.
- Assign roles to **users and/or groups** in that API app registration.
- The **API access token** will then include a `roles` claim, which both:
  - the UI can read (for gating admin-only UI), and
  - the API can enforce (for endpoint authorization).

Roles:
- `Admin`
- `Attorney`

Authorization rules:
- **ICS Admin Interface**: allow sign-in, but require `Admin` role to use the portal.
- **ICS API**: allow `Admin` + `Attorney`, with endpoint-level restrictions.

## 3) Microsoft Entra ID Configuration (Two App Registrations)

### A) App registration: `ICS API` (Web API)
1. **Expose an API**
   - Set Application ID URI: `api://<ICS-API-CLIENT-ID>`
   - Add scope: `access_as_user`
2. **App roles**
   - Create roles:
     - `Admin` (allowed member types: Users/Groups)
     - `Attorney` (allowed member types: Users/Groups)
3. **Assignments**
   - Assign users/groups to roles **in this Enterprise Application**
4. **Token claims**
   - Confirm access tokens include `roles` claim when users are role-assigned.

### B) App registration: `ICS Admin Interface` (SPA)
1. **Platform configuration**: Single-page application
2. **Redirect URIs**
   - Dev: `http://localhost:3000/ics-admin/` (or whatever dev base path you use)
   - Prod: `https://nam-pdaparch01.americas.global-net.com/ics-admin/`
3. **API permissions**
   - Add delegated permission to ICS API: `access_as_user`
   - Grant admin consent as needed

## 4) ICS Admin Interface (Next.js) Architecture

### Rendering choice
- Use **CSR** for authenticated screens.
- Use **SSG** only for truly static, non-user-specific pages (e.g., a simple landing page). Most admin portals end up being CSR-only after sign-in.

### Auth implementation (recommended)
- Use `@azure/msal-browser` (and optionally `@azure/msal-react`).
- Use redirect flow (SSO with redirects).

High-level flow:
1. User navigates to `/ics-admin/`
2. If not signed in, redirect to Entra login
3. After redirect completes, acquire an access token for the ICS API scope
4. Decode token and check `roles` includes `Admin`
   - If not Admin: show a “Not authorized” screen
   - If Admin: render the portal

### API calls
- Use native `fetch` (sufficient and simplest for learning).
- Always attach `Authorization: Bearer <access_token>`.

### Security notes (practical)
- Prefer MSAL cache in `sessionStorage` for this learning app (reduces persistence).
- Avoid writing tokens to local storage yourself.
- Treat all UI as untrusted: API is the real security boundary.

## 5) ICS API (.NET) Architecture

### Authentication
- Validate JWTs issued by your tenant using `Microsoft.Identity.Web`.
- Configure audience to the API Application ID URI or client id (consistently with how you configured “Expose an API”).

### Authorization
Use role-based policies / attributes:
- Admin-only endpoints: require `Admin`
- Shared endpoints: allow `Admin` and `Attorney`

Suggested endpoint examples (in-memory data):
- `GET /ics-api/cases` -> Admin + Attorney
- `GET /ics-api/cases/{id}` -> Admin + Attorney
- `POST /ics-api/cases` -> Admin
- `PUT /ics-api/cases/{id}` -> Admin
- `DELETE /ics-api/cases/{id}` -> Admin

### Error handling
- Use a global exception handler and return **ProblemDetails**.
- Return `401` when missing/invalid token; `403` when authenticated but missing role.

### Logging + auditing
You asked for both:
- **Application logs**: errors, warnings, request info, correlation IDs
- **Audit events**: who did what and when

Keep it simple:
- Create an audit logger that records:
  - timestamp
  - user `oid` (object id), `preferred_username`/UPN if available
  - roles
  - HTTP method + path
  - outcome (success/forbidden/error)
  - correlation id (`TraceIdentifier`)

Store audit events initially as:
- Structured log events (JSON) to a rolling file (easiest on-prem), OR
- A dedicated in-memory list (for the pure example) with an endpoint to view them (Admin-only)

## 6) IIS Deployment Guidance (Simple)

### Admin UI (static)
- Build/export Next.js to static output
- Copy the export output folder contents into the IIS `/ics-admin` physical path
- Ensure IIS serves:
  - `.js`, `.css`, `.json`, images
  - correct MIME types (usually already fine)

### API (ASP.NET Core)
- Publish the API (`dotnet publish`) for IIS hosting
- Install/enable:
  - ASP.NET Core Hosting Bundle on the server
- Configure IIS Application Pool for `/ics-api`

### Same-origin benefits
With UI and API under the same host:
- No CORS config needed (unless you also call the API from a different dev origin)

## 7) Minimal “Done” Checklist

- Entra:
  - ICS API app has scope `access_as_user`
  - ICS API app has roles `Admin`, `Attorney`
  - Users/groups assigned roles
  - ICS Admin Interface app has redirect URIs set for `/ics-admin/`
  - SPA has delegated permission to API scope
- Admin UI:
  - MSAL redirect login
  - Acquire API token and check `roles` contains `Admin`
  - Calls API with Bearer token
- API:
  - Validates JWTs
  - Uses role-based auth per endpoint
  - Global error handling (ProblemDetails)
  - Logs requests + audit events

---

If you want, I can also write a second doc that’s *implementation-oriented* (exact config values to plug into `msalConfig`, `appsettings.json`, and IIS path/basePath conventions for static export under `/ics-admin`).
