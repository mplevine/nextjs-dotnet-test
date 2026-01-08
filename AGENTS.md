# AGENTS.md - AI Agent Guide for ICS System

This document provides AI agents with essential context for working on the ICS (International Case System) codebase.

## Rules for AI Agents

1. Always use Context7 MCP when I need library/API documentation, code generation, setup or configuration steps without me having to explicitly ask.

## Quick Overview

**Dual-stack application deployed to IIS under sub-paths:**
- **Admin UI**: Next.js static export at `/ics-admin` (SPA with MSAL authentication)
- **API**: ASP.NET Core 8 Web API at `/ics-api` (Entra ID JWT validation)
- **Auth**: Microsoft Entra ID with role-based authorization (`Admin`, `Attorney`)
- **Deployment**: Single IIS host with both apps under same origin (no CORS in production)

**Production URL**: `https://nam-pdaparch01.americas.global-legal.com/`

---

## Critical Architectural Decisions

### 1. Sub-Path Deployment (NOT Root Paths)
All components run under sub-paths, not root:
- Next.js: `basePath: "/ics-admin"` configured in [next.config.ts](src/admin/ics-admin/next.config.ts)
- API: Uses `UsePathBase("/ics-api")` in Development only (see [Program.cs](src/api/IcsApi/Program.cs))
- Development URLs mirror production structure

### 2. Two-App Entra ID Pattern
**Critical for role enforcement:**
- **ICS API** (`754ec9b6-b889-44bf-a6fe-2034a37647d4`): Defines app roles and exposes scope `access_as_user`
- **ICS Admin Interface** (`471a2896-5785-4789-9c05-20077c08f75d`): SPA that requests API scope
- **Roles live in API app registration only** - JWT `roles` claim comes from API app

### 3. Next.js Static Export
- Uses `output: "export"` for IIS static file hosting
- No server-side features available (no SSR, no API routes, no server components)
- All routing is client-side
- Images must be unoptimized

### 4. Authorization Policies
Use named policies, not raw role strings:
```csharp
[Authorize(Policy = "AdminOnly")]        // Admin only
[Authorize(Policy = "AdminOrAttorney")]  // Admin + Attorney
```
Policies defined in [Program.cs](src/api/IcsApi/Program.cs#L15-L19)

### 5. Environment-Specific Configuration
- **Development**: API on `https://localhost:7296/ics-api/`, UI on `http://localhost:3000/ics-admin/`
- **Production**: Both under same origin (no CORS)
- CORS policy `DevAdminUI` only enabled in Development

---

## Repository Structure

```
docs/                          # Comprehensive documentation
├── system-architecture-outline.md  # Design decisions & rationale
├── implementation-guide.md         # Entra ID setup & config values
├── iis-deployment-guide.md        # Step-by-step IIS deployment
└── dev-run.md                     # Local development quick start

src/
├── admin/ics-admin/          # Next.js SPA
│   ├── next.config.ts        # Static export, basePath, trailingSlash
│   ├── deploy/web.config     # IIS rewrite rules (copied to out/ on build)
│   └── src/
│       ├── lib/
│       │   ├── env.ts        # Centralized environment variables
│       │   └── msal.ts       # MSAL configuration (redirect flow)
│       └── app/              # Next.js App Router pages
│
└── api/IcsApi/               # ASP.NET Core Web API
    ├── Program.cs            # Auth, policies, CORS, auditing, PathBase
    ├── appsettings*.json     # Entra ID config (per environment)
    ├── Controllers/
    │   ├── CasesController.cs    # Example policy-based authorization
    │   ├── AuditController.cs    # Admin-only audit log endpoint
    │   └── HealthController.cs   # Anonymous health check
    ├── Services/
    │   ├── InMemoryCaseStore.cs  # In-memory case data
    │   └── InMemoryAuditStore.cs # In-memory audit trail
    └── deploy/web.config     # IIS ASP.NET Core hosting config
```

---

## Common Workflows

### Running Locally

**API:**
```powershell
cd src/api/IcsApi
dotnet run
# Runs on https://localhost:7296/ics-api/
# Health check: https://localhost:7296/ics-api/health
# Swagger: https://localhost:7296/ics-api/swagger
```

**UI:**
```powershell
cd src/admin/ics-admin
npm run dev
# Runs on http://localhost:3000/ics-admin/
```

**Important**: Create `.env.local` from example before running UI.

### Building for IIS Deployment

**API:**
```powershell
cd src/api/IcsApi
dotnet publish -c Release -o ./publish
# Output in publish/ folder, includes web.config
```

**UI:**
```powershell
cd src/admin/ics-admin
npm run build
# Output in out/ folder
# Build script auto-copies deploy/web.config to out/
```

### Adding New API Endpoints

1. Create controller in `src/api/IcsApi/Controllers/`
2. Apply authorization attribute:
   ```csharp
   [Authorize(Policy = "AdminOnly")]
   // or
   [Authorize(Policy = "AdminOrAttorney")]
   ```
3. Use `ProblemDetails` for error responses
4. Test with [IcsApi.http](src/api/IcsApi/IcsApi.http) (requires auth token)

### Modifying UI Components

1. All components must be client-side only (no server components with static export)
2. Use `env.apiBaseUrl` from [env.ts](src/admin/ics-admin/src/lib/env.ts) for API calls
3. Include `Authorization: Bearer <token>` header on all API requests
4. Check user roles via decoded JWT before rendering admin features

---

## Key Configuration Files

### API Configuration
- [appsettings.json](src/api/IcsApi/appsettings.json): Base Entra ID config (TenantId, ClientId)
- [appsettings.Development.json](src/api/IcsApi/appsettings.Development.json): Sets `Ics:UsePathBase=true`
- [appsettings.Production.json](src/api/IcsApi/appsettings.Production.json): Production AllowedHosts

### UI Configuration
- [next.config.ts](src/admin/ics-admin/next.config.ts): Static export, basePath, trailingSlash
- [src/lib/env.ts](src/admin/ics-admin/src/lib/env.ts): Environment variables with fallback defaults
- [src/lib/msal.ts](src/admin/ics-admin/src/lib/msal.ts): MSAL browser config (redirect flow)
- `.env.local`: Local development overrides (not committed)
- `.env.production`: Production environment variables (for build-time embedding)

### IIS Deployment
- [src/admin/ics-admin/deploy/web.config](src/admin/ics-admin/deploy/web.config): SPA rewrite rules + security headers
- [src/api/IcsApi/deploy/web.config](src/api/IcsApi/deploy/web.config): ASP.NET Core hosting config

---

## Critical Gotchas & Conventions

### 1. Trailing Slashes Matter
- UI uses `trailingSlash: true` in Next.js config
- Entra redirect URIs must match exactly (include trailing slash)
- Example: `https://nam-pdaparch01.americas.global-legal.com/ics-admin/`

### 2. PathBase Handling
- API uses `UsePathBase("/ics-api")` **only when** `Ics:UsePathBase=true` in appsettings
- In IIS production, do **not** set `UsePathBase=true` (IIS provides the base path)
- In Development, set it to `true` for consistent URLs

### 3. Role Claims Location
- Check **both** `"roles"` and `ClaimTypes.Role` claims
- Middleware in [Program.cs](src/api/IcsApi/Program.cs#L50-L55) handles both formats
- Roles must be assigned in **ICS API** Enterprise Application (not the SPA app)

### 4. CORS Configuration
- CORS only needed in Development for `http://localhost:3000`
- Policy `DevAdminUI` enabled only when `app.Environment.IsDevelopment()`
- Production uses same origin, no CORS required

### 5. SPA Routing on IIS
- Requires **IIS URL Rewrite module** installed
- [deploy/web.config](src/admin/ics-admin/deploy/web.config) rewrites unknown paths to `index.html`
- Without this, deep links (e.g., `/ics-admin/cases/123`) return 404

### 6. Static Export Limitations
- No Next.js API routes
- No server-side rendering (SSR)
- No server components
- No dynamic routes with `getStaticPaths` required at build time
- No Image Optimization (use `unoptimized: true`)
- Environment variables must be `NEXT_PUBLIC_*` to be embedded at build time

### 7. Auditing Middleware
- Runs **after** response in [Program.cs](src/api/IcsApi/Program.cs#L47-L66)
- Records: user identity, roles, status code, method, path, correlation ID
- View via `GET /audit` (Admin-only endpoint)
- Currently uses in-memory store (resets on app restart)

---

## Testing Authorization

**Role Test Matrix** (use [IcsApi.http](src/api/IcsApi/IcsApi.http) with auth token):

| Endpoint | Admin | Attorney | Anonymous |
|----------|-------|----------|-----------|
| `GET /health` | ✓ | ✓ | ✓ |
| `GET /me` | ✓ | ✓ | 401 |
| `GET /cases` | ✓ | ✓ | 401 |
| `GET /cases/{id}` | ✓ | ✓ | 401 |
| `POST /cases` | ✓ | 403 | 401 |
| `DELETE /cases/{id}` | ✓ | 403 | 401 |
| `GET /audit` | ✓ | 403 | 401 |

---

## Environment Variables

### UI (Next.js)
All must be prefixed with `NEXT_PUBLIC_` for static export:

```env
NEXT_PUBLIC_TENANT_ID=09131022-b785-4e6d-8d42-916975e51262
NEXT_PUBLIC_SPA_APP_CLIENT_ID=471a2896-5785-4789-9c05-20077c08f75d
NEXT_PUBLIC_API_SCOPE=api://754ec9b6-b889-44bf-a6fe-2034a37647d4/access_as_user
NEXT_PUBLIC_API_BASE_URL=https://nam-pdaparch01.americas.global-legal.com/ics-api
NEXT_PUBLIC_REDIRECT_URI=https://nam-pdaparch01.americas.global-legal.com/ics-admin/
NEXT_PUBLIC_POST_LOGOUT_REDIRECT_URI=https://nam-pdaparch01.americas.global-legal.com/ics-admin/
```

### API (ASP.NET Core)
Configured via `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "09131022-b785-4e6d-8d42-916975e51262",
    "ClientId": "754ec9b6-b889-44bf-a6fe-2034a37647d4"
  },
  "Ics": {
    "UsePathBase": true  // Only in Development
  }
}
```

---

## Making Changes

### When Modifying Authentication
- API role changes require user re-login to get updated token
- Redirect URI changes require exact match in Entra portal
- See [implementation-guide.md](docs/implementation-guide.md) for Entra setup steps

### When Adding Dependencies
- **UI**: Add to `package.json`, but verify compatibility with static export
- **API**: Add NuGet packages, ensure they work with .NET 8 on Windows Server

### When Changing Base Paths
If you need to change `/ics-admin` or `/ics-api`:
1. Update `basePath` in [next.config.ts](src/admin/ics-admin/next.config.ts)
2. Update `UsePathBase` call in [Program.cs](src/api/IcsApi/Program.cs)
3. Update all environment variables (redirect URIs, API base URL)
4. Update Entra ID redirect URIs in Azure Portal
5. Update IIS application aliases
6. Update documentation

---

## Documentation References

For deeper understanding, read these documents in order:

1. **[dev-run.md](docs/dev-run.md)** - Quick start for local development
2. **[system-architecture-outline.md](docs/system-architecture-outline.md)** - Design decisions and rationale
3. **[implementation-guide.md](docs/implementation-guide.md)** - Entra ID setup and configuration values
4. **[iis-deployment-guide.md](docs/iis-deployment-guide.md)** - Complete IIS deployment steps

---

## Common Issues & Solutions

### Issue: "Not authorized" after login
**Solution**: Ensure user has `Admin` role assigned in **ICS API** Enterprise Application (not the SPA app).

### Issue: API returns 401 for authenticated user
**Solutions**:
- Verify token audience matches API Application ID URI
- Check token is being sent with `Authorization: Bearer <token>` header
- Verify API `appsettings.json` has correct `TenantId` and `ClientId`

### Issue: Deep links return 404 on IIS
**Solution**: Ensure IIS URL Rewrite module is installed and `web.config` exists in UI deployment folder.

### Issue: API changes not reflected
**Solution**: 
- Rebuild and republish: `dotnet publish -c Release`
- Restart IIS Application Pool or run `iisreset`

### Issue: UI environment variables not working
**Solutions**:
- Ensure variables are prefixed with `NEXT_PUBLIC_`
- Static export embeds variables at **build time** - rebuild after changes
- Check `.env.production` or `.env.local` exists and is correctly formatted

### Issue: CORS errors in Development
**Solution**: Verify `DevAdminUI` policy in [Program.cs](src/api/IcsApi/Program.cs) includes your dev UI origin.

---

## Architecture Principles

This codebase follows these principles for clarity and maintainability:

1. **Simplicity First**: In-memory stores, no database, minimal dependencies
2. **Learning-Oriented**: Designed for understanding concepts, not production scale
3. **Sub-Path Consistency**: Dev and prod URLs use same structure
4. **Security at API**: UI is untrusted; API enforces all authorization
5. **Static Export**: UI is static files only, no server components
6. **Role-Based Authorization**: Policies, not magic strings
7. **Structured Errors**: ProblemDetails for API errors
8. **Audit Trail**: Middleware logs all requests after response

---

## Quick Commands Reference

```powershell
# API - Development
cd src/api/IcsApi
dotnet run                                    # Run locally
dotnet publish -c Release -o ./publish        # Build for IIS

# UI - Development  
cd src/admin/ics-admin
npm install                                   # Install dependencies
npm run dev                                   # Run dev server
npm run build                                 # Build static export for IIS

# Testing
# Use IcsApi.http with valid auth token
# Test health endpoint (anonymous): GET /health
# Test protected endpoint: GET /cases (requires Admin or Attorney)
# Test admin-only endpoint: POST /cases (requires Admin)

# IIS Management (on server)
# Restart IIS:
iisreset
# Restart API pool:
Restart-WebAppPool -Name "IcsApiPool"
# Restart Admin UI pool:
Restart-WebAppPool -Name "IcsAdminPool"
```

---

## Contact & Support

For questions about:
- **Entra ID setup**: See [implementation-guide.md](docs/implementation-guide.md)
- **IIS deployment**: See [iis-deployment-guide.md](docs/iis-deployment-guide.md)
- **Architecture decisions**: See [system-architecture-outline.md](docs/system-architecture-outline.md)
- **Local development**: See [dev-run.md](docs/dev-run.md)

This is a learning project - all implementation details are documented in the `docs/` folder.
