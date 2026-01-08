# ICS System - AI Agent Instructions

## Rules for AI Agents

1. Always use Context7 MCP when I need library/API documentation, code generation, setup or configuration steps without me having to explicitly ask.

## Architecture Overview

This is a **dual-stack, sub-path deployment** to IIS:
- **Admin UI**: Next.js static export (`output: "export"`) at `/ics-admin`
- **API**: ASP.NET Core 8 Web API at `/ics-api`
- **Auth**: Microsoft Entra ID with app role-based authorization (`Admin`, `Attorney`)
- **Deployment**: Single IIS host with applications at sub-paths (same origin, no CORS in production)

```
https://nam-pdaparch01.americas.global-legal.com/
├── /ics-admin/  → Next.js static files (SPA)
└── /ics-api/    → .NET API
```

## Critical Conventions

### 1. Sub-Path Architecture
All components must work under sub-paths, not root:
- Next.js: `basePath: "/ics-admin"` in [next.config.ts](../src/admin/ics-admin/next.config.ts)
- API: Uses `UsePathBase("/ics-api")` in Development only (IIS provides base path in production)
- URLs: Always include base path - dev URLs mirror production structure

### 2. Environment-Specific Configuration
- **Development**: API runs on `https://localhost:7296/ics-api/`, UI on `http://localhost:3000/ics-admin/`
- **Production**: Both under same origin (no CORS needed)
- API enables CORS policy `DevAdminUI` only in Development mode (see [Program.cs](../src/api/IcsApi/Program.cs#L23-L28))

### 3. Entra ID Two-App Pattern
**Two separate app registrations** (critical for role enforcement):
- **ICS API** (`754ec9b6-b889-44bf-a6fe-2034a37647d4`): Defines app roles (`Admin`, `Attorney`) and exposes scope `access_as_user`
- **ICS Admin Interface** (`471a2896-5785-4789-9c05-20077c08f75d`): SPA that requests API scope
- **Roles in JWT**: API access token contains `roles` claim from API app registration only

### 4. Authorization Pattern
Use named policies defined in [Program.cs](../src/api/IcsApi/Program.cs#L15-L19):
```csharp
options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
options.AddPolicy("AdminOrAttorney", policy => policy.RequireRole("Admin", "Attorney"));
```
Apply to endpoints: `[Authorize(Policy = "AdminOnly")]` (see [CasesController.cs](../src/api/IcsApi/Controllers/CasesController.cs))

### 5. Auditing Middleware
All requests logged **after response** in [Program.cs](../src/api/IcsApi/Program.cs#L47-L66):
- Records user identity (`oid`, `preferred_username`), roles, status code
- Uses in-memory store ([InMemoryAuditStore.cs](../src/api/IcsApi/Services/InMemoryAuditStore.cs))
- View via `GET /audit` (Admin-only endpoint)

## Development Workflows

### Running Locally
```powershell
# API (from src/api/IcsApi)
dotnet run  # Runs on https://localhost:7296/ics-api/

# UI (from src/admin/ics-admin)
npm run dev  # Runs on http://localhost:3000/ics-admin/
```

**Key files for local config:**
- API: [appsettings.Development.json](../src/api/IcsApi/appsettings.Development.json) - sets `Ics:UsePathBase=true`
- UI: `.env.local` (create from example) - sets localhost URLs

### Building for IIS Deployment
```powershell
# API: Creates publish folder
dotnet publish -c Release -o ./publish

# UI: Creates out folder with web.config copied
npm run build  # Runs next build + copies deploy/web.config to out/
```

**Critical build details:**
- UI build script auto-copies [deploy/web.config](../src/admin/ics-admin/deploy/web.config) for IIS rewrite rules
- Production environment variables must be set as `NEXT_PUBLIC_*` for static export
- See full deployment steps in [iis-deployment-guide.md](../docs/iis-deployment-guide.md)

## Key Files Reference

### Configuration
- [next.config.ts](../src/admin/ics-admin/next.config.ts): Static export settings, `basePath`, `trailingSlash`
- [src/admin/ics-admin/src/lib/env.ts](../src/admin/ics-admin/src/lib/env.ts): Centralized environment variables with defaults
- [src/admin/ics-admin/src/lib/msal.ts](../src/admin/ics-admin/src/lib/msal.ts): MSAL browser configuration (redirect flow, sessionStorage)
- [appsettings.json](../src/api/IcsApi/appsettings.json): Entra ID tenant and client IDs

### Implementation Patterns
- [Program.cs](../src/api/IcsApi/Program.cs): Auth, policies, CORS, auditing middleware, PathBase handling
- [CasesController.cs](../src/api/IcsApi/Controllers/CasesController.cs): Policy-based authorization example
- [deploy/web.config](../src/admin/ics-admin/deploy/web.config): IIS URL rewrite for SPA routing + security headers

### Documentation
- [system-architecture-outline.md](../docs/system-architecture-outline.md): Design decisions and token strategy
- [implementation-guide.md](../docs/implementation-guide.md): Entra ID setup, config values, common gotchas
- [dev-run.md](../docs/dev-run.md): Quick start for local development

## Common Gotchas

1. **SPA routing on IIS**: Requires URL Rewrite module + `web.config` rewrite rule to send unknown paths to `index.html`
2. **PathBase in Development**: API uses `UsePathBase("/ics-api")` only when `Ics:UsePathBase=true` in appsettings
3. **Trailing slashes**: UI uses `trailingSlash: true` in Next.js config - redirect URIs must match exactly
4. **Role claims**: Check both `"roles"` and `ClaimTypes.Role` claims (middleware handles both in [Program.cs](../src/api/IcsApi/Program.cs#L50-L55))
5. **Static export limitations**: Next.js uses `output: "export"` - no server-side features, images must be unoptimized
6. **CORS**: Only needed in development for localhost cross-origin calls

## Making Changes

### Adding API Endpoints
1. Add controller in `src/api/IcsApi/Controllers/`
2. Apply `[Authorize(Policy = "...")]` attribute
3. Use `ProblemDetails` for error responses (global exception handler enabled)

### Modifying UI
1. All components client-side only (no server components with static export)
2. Use `env.apiBaseUrl` from [env.ts](../src/admin/ics-admin/src/lib/env.ts) for API calls
3. Include `Authorization: Bearer <token>` header on all API requests
4. Check user roles via decoded JWT before rendering admin features

### Changing Entra Configuration
- API role changes require user re-login to get updated token
- Redirect URI changes require exact match in Entra portal (including trailing slash)
- See [implementation-guide.md](../docs/implementation-guide.md) for complete Entra setup

## Testing Authorization
Use [IcsApi.http](../src/api/IcsApi/IcsApi.http) for manual API testing (requires auth token).

**Role test matrix:**
- `GET /cases`: Accessible to Admin + Attorney
- `POST /cases`: Admin only (returns 403 for Attorney)
- `GET /audit`: Admin only
