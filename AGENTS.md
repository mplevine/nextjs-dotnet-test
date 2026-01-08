# AI Agent Guide - ICS System

This document provides comprehensive context for AI agents working on the International Case System (ICS) codebase.

## Agent Rules & Tool Usage

**Context7 MCP:** Always use Context7 MCP when you need library/API documentation, code generation, setup or configuration steps - proactively, without waiting for explicit requests.

## System Overview

**Architecture:** Dual-stack application deployed to IIS under sub-paths on a single origin.

**Production URL:** `https://nam-pdaparch01.americas.global-legal.com/`

**Components:**
- **Admin UI**: Next.js 15 static export (`output: "export"`) at `/ics-admin` - SPA with MSAL authentication
- **API**: ASP.NET Core 8 Web API at `/ics-api` - Entra ID JWT validation with role-based authorization
- **Auth**: Microsoft Entra ID with app roles: `Admin` and `Attorney`
- **Deployment**: Same-origin deployment (no CORS in production)

**Technology Stack:**
- Frontend: Next.js 15, TypeScript, TailwindCSS, MSAL Browser
- Backend: ASP.NET Core 8, C#, Microsoft.Identity.Web
- Hosting: Windows Server IIS
- Authentication: Microsoft Entra ID (Azure AD)

---

## Critical Architectural Decisions

### 1. Sub-Path Deployment Strategy

**Why:** IIS deployment requires both applications under a single host with distinct sub-paths.

**Implementation:**
- Next.js: `basePath: "/ics-admin"` in `next.config.ts:8`
- API: `UsePathBase("/ics-api")` in `Program.cs:43` (Development only)
- Development URLs intentionally mirror production structure

**Implications:**
- All URLs must include base paths
- Redirect URIs must match exactly (including trailing slashes)
- Client-side routing requires IIS URL Rewrite module

**Files:**
- `src/admin/ics-admin/next.config.ts`
- `src/api/IcsApi/Program.cs`

### 2. Two-App Entra ID Pattern

**Why:** Enables proper role-based authorization with roles assigned to API access tokens.

**Implementation:**
Two separate Azure AD app registrations:
1. **ICS API** (`754ec9b6-b889-44bf-a6fe-2034a37647d4`)
   - Defines app roles: `Admin` and `Attorney`
   - Exposes scope: `api://754ec9b6-b889-44bf-a6fe-2034a37647d4/access_as_user`

2. **ICS Admin Interface** (`471a2896-5785-4789-9c05-20077c08f75d`)
   - SPA client that requests API scope
   - No app roles defined here

**Token Flow:**
```
User → Login (SPA app) → Request API scope → Receive token with roles from API app
```

**Critical:** Roles MUST be assigned in the ICS API Enterprise Application, not the SPA app. The JWT `roles` claim comes from the API app registration only.

**Files:**
- `src/admin/ics-admin/src/lib/msal.ts` - SPA auth config
- `src/api/IcsApi/appsettings.json` - API auth config
- `docs/implementation-guide.md` - Complete Entra setup steps

### 3. Next.js Static Export

**Why:** IIS hosting model requires static files (no Node.js runtime available).

**Implementation:**
- `output: "export"` in `next.config.ts:7`
- Build produces static HTML/CSS/JS in `out/` folder
- All environment variables must be `NEXT_PUBLIC_*` (embedded at build time)

**Limitations:**
- No server-side rendering (SSR)
- No server components
- No API routes (`/api/*`)
- No dynamic routes with `getStaticPaths` (all routes must be known at build time)
- No Image Optimization (must use `unoptimized: true`)
- No `getServerSideProps` or server-side data fetching

**Workarounds:**
- All data fetching done client-side via API calls
- All components must be client-side (`'use client'`)
- Images served unoptimized

**Files:**
- `src/admin/ics-admin/next.config.ts`

### 4. Authorization Policy Pattern

**Why:** Type-safe, maintainable, and prevents magic strings throughout codebase.

**Implementation:**
```csharp
// Program.cs - Policy definitions
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrAttorney", policy => policy.RequireRole("Admin", "Attorney"));
});

// Controllers - Policy usage
[Authorize(Policy = "AdminOnly")]
public IActionResult CreateCase(...)
```

**Benefits:**
- Compile-time checking
- Single source of truth
- Easy to audit and modify

**Files:**
- `src/api/IcsApi/Program.cs:15-19` - Policy definitions
- `src/api/IcsApi/Controllers/CasesController.cs` - Usage examples

### 5. Environment-Specific Configuration

**Why:** Different requirements for development (CORS, PathBase) vs. production (same-origin).

**Development:**
- API: `https://localhost:7296/ics-api/`
- UI: `http://localhost:3000/ics-admin/`
- CORS enabled for cross-origin requests
- `UsePathBase` set to `true` in `appsettings.Development.json`

**Production:**
- Both: `https://nam-pdaparch01.americas.global-legal.com/` + sub-paths
- No CORS (same origin)
- IIS provides path base, `UsePathBase` not set

**Files:**
- `src/api/IcsApi/appsettings.Development.json`
- `src/api/IcsApi/appsettings.Production.json`
- `src/api/IcsApi/Program.cs:23-28` - CORS config

### 6. Auditing Middleware

**Why:** Track all API usage for compliance and debugging.

**Implementation:**
Middleware runs **after** response is sent to client:
```csharp
app.Use(async (context, next) =>
{
    await next();  // Process request first
    // Log audit entry after response
});
```

**Data Collected:**
- User identity (`oid`, `preferred_username`)
- User roles
- HTTP method and path
- Status code
- Timestamp
- Correlation ID

**Storage:** In-memory store (resets on application restart). In production, replace with persistent storage.

**Access:** `GET /audit` endpoint (Admin-only)

**Files:**
- `src/api/IcsApi/Program.cs:47-66` - Middleware implementation
- `src/api/IcsApi/Services/InMemoryAuditStore.cs`
- `src/api/IcsApi/Controllers/AuditController.cs`

---

## Repository Structure

```
nextjs-dotnet-test/
├── docs/                                    # Comprehensive documentation
│   ├── system-architecture-outline.md       # Design decisions & rationale
│   ├── implementation-guide.md              # Entra ID setup & config
│   ├── iis-deployment-guide.md             # Step-by-step IIS deployment
│   └── dev-run.md                          # Local development quick start
│
├── src/
│   ├── admin/ics-admin/                    # Next.js SPA
│   │   ├── next.config.ts                  # Static export, basePath, trailingSlash
│   │   ├── deploy/
│   │   │   └── web.config                  # IIS rewrite rules (copied to out/)
│   │   └── src/
│   │       ├── lib/
│   │       │   ├── env.ts                  # Centralized environment variables
│   │       │   └── msal.ts                 # MSAL configuration (redirect flow)
│   │       └── app/                        # Next.js App Router pages (client-side)
│   │
│   └── api/IcsApi/                         # ASP.NET Core Web API
│       ├── Program.cs                      # Auth, policies, CORS, auditing, PathBase
│       ├── appsettings.json                # Base Entra ID config
│       ├── appsettings.Development.json    # Dev-specific settings
│       ├── appsettings.Production.json     # Prod-specific settings
│       ├── Controllers/
│       │   ├── CasesController.cs          # Policy-based authorization example
│       │   ├── AuditController.cs          # Admin-only audit log endpoint
│       │   └── HealthController.cs         # Anonymous health check
│       ├── Services/
│       │   ├── InMemoryCaseStore.cs        # Sample data store
│       │   └── InMemoryAuditStore.cs       # Audit trail store
│       ├── deploy/
│       │   └── web.config                  # IIS ASP.NET Core hosting config
│       └── IcsApi.http                     # Manual API testing file
│
├── .github/
│   └── copilot-instructions.md             # GitHub Copilot coding conventions
├── AGENTS.md                               # This file
└── nextjs-dotnet-test.sln                  # .NET solution file
```

---

## Development Workflows

### Running Locally

**Prerequisites:**
- .NET 8 SDK
- Node.js 20+
- Valid Entra ID tenant and app registrations

**API:**
```powershell
cd src/api/IcsApi
dotnet run
# Runs on: https://localhost:7296/ics-api/
# Health check: https://localhost:7296/ics-api/health
# Swagger: https://localhost:7296/ics-api/swagger
```

**UI:**
```powershell
cd src/admin/ics-admin
# First time: create .env.local from .env.example
npm install
npm run dev
# Runs on: http://localhost:3000/ics-admin/
```

**Important:**
- Create `.env.local` with valid Entra ID configuration before running UI
- Both applications must be running for full functionality
- See `docs/dev-run.md` for detailed setup instructions

### Building for IIS Deployment

**API:**
```powershell
cd src/api/IcsApi
dotnet publish -c Release -o ./publish
# Output: publish/ folder contains:
#   - IcsApi.dll and dependencies
#   - deploy/web.config
#   - appsettings.json
```

**UI:**
```powershell
cd src/admin/ics-admin
npm run build
# Output: out/ folder contains:
#   - Static HTML/CSS/JS files
#   - _next/ folder with bundled assets
#   - web.config (auto-copied from deploy/)
```

**Build Process Notes:**
- UI build script automatically copies `deploy/web.config` to `out/` folder
- Environment variables must be set in `.env.production` before build
- Static export embeds all `NEXT_PUBLIC_*` variables at build time

See `docs/iis-deployment-guide.md` for complete deployment instructions.

### Adding New API Endpoints

**Steps:**
1. Create new controller in `src/api/IcsApi/Controllers/`
2. Apply authorization attribute:
   ```csharp
   [Authorize(Policy = "AdminOnly")]
   // or
   [Authorize(Policy = "AdminOrAttorney")]
   ```
3. Use `ProblemDetails` for error responses:
   ```csharp
   return Problem(
       statusCode: StatusCodes.Status404NotFound,
       title: "Resource not found"
   );
   ```
4. Test with `IcsApi.http` (requires valid auth token)

**Example:**
```csharp
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "AdminOrAttorney")]
public class DocumentsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetDocuments()
    {
        // Implementation
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]  // Override for this action
    public IActionResult CreateDocument([FromBody] DocumentDto dto)
    {
        // Implementation
    }
}
```

### Modifying UI Components

**Requirements:**
1. All components must be client-side only (no server components)
2. Add `'use client'` directive at top of file
3. Use `env.apiBaseUrl` from `src/lib/env.ts` for API calls
4. Include `Authorization: Bearer <token>` header on all API requests
5. Check user roles from decoded JWT before rendering admin features

**Example:**
```typescript
'use client';

import { env } from '@/lib/env';
import { useMsal } from '@azure/msal-react';

export default function CaseList() {
  const { instance, accounts } = useMsal();

  async function fetchCases() {
    const account = accounts[0];
    const token = await instance.acquireTokenSilent({
      scopes: [env.apiScope],
      account
    });

    const response = await fetch(`${env.apiBaseUrl}/cases`, {
      headers: {
        'Authorization': `Bearer ${token.accessToken}`
      }
    });

    return response.json();
  }

  // Component implementation
}
```

---

## Key Configuration Files

### API Configuration

**appsettings.json** - Base configuration:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "09131022-b785-4e6d-8d42-916975e51262",
    "ClientId": "754ec9b6-b889-44bf-a6fe-2034a37647d4"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

**appsettings.Development.json** - Development overrides:
```json
{
  "Ics": {
    "UsePathBase": true  // Enables PathBase middleware
  }
}
```

**appsettings.Production.json** - Production overrides:
```json
{
  "AllowedHosts": "nam-pdaparch01.americas.global-legal.com",
  "Ics": {
    "UsePathBase": false  // IIS provides path base
  }
}
```

### UI Configuration

**next.config.ts** - Next.js configuration:
```typescript
const nextConfig: NextConfig = {
  output: 'export',           // Static export for IIS
  basePath: '/ics-admin',     // Sub-path deployment
  trailingSlash: true,        // Must match redirect URIs
  images: {
    unoptimized: true         // Required for static export
  }
};
```

**src/lib/env.ts** - Environment variables:
```typescript
export const env = {
  tenantId: process.env.NEXT_PUBLIC_TENANT_ID || 'default-tenant-id',
  spaAppClientId: process.env.NEXT_PUBLIC_SPA_APP_CLIENT_ID || 'default-client-id',
  apiScope: process.env.NEXT_PUBLIC_API_SCOPE || 'api://default/access_as_user',
  apiBaseUrl: process.env.NEXT_PUBLIC_API_BASE_URL || 'https://localhost:7296/ics-api',
  redirectUri: process.env.NEXT_PUBLIC_REDIRECT_URI || 'http://localhost:3000/ics-admin/',
  postLogoutRedirectUri: process.env.NEXT_PUBLIC_POST_LOGOUT_REDIRECT_URI || 'http://localhost:3000/ics-admin/'
};
```

**src/lib/msal.ts** - MSAL configuration:
```typescript
export const msalConfig: Configuration = {
  auth: {
    clientId: env.spaAppClientId,
    authority: `https://login.microsoftonline.com/${env.tenantId}`,
    redirectUri: env.redirectUri,
    postLogoutRedirectUri: env.postLogoutRedirectUri
  },
  cache: {
    cacheLocation: 'sessionStorage',  // More secure than localStorage
    storeAuthStateInCookie: false
  }
};
```

### IIS Deployment Configuration

**UI web.config** - SPA rewrite rules:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="React Routes" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

**API web.config** - ASP.NET Core hosting:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet"
                arguments=".\IcsApi.dll"
                stdoutLogEnabled="false"
                stdoutLogFile=".\logs\stdout"
                hostingModel="inprocess" />
  </system.webServer>
</configuration>
```

---

## Environment Variables Reference

### UI Environment Variables (Next.js)

**All variables must be prefixed with `NEXT_PUBLIC_` for static export.**

**Development (.env.local):**
```env
NEXT_PUBLIC_TENANT_ID=09131022-b785-4e6d-8d42-916975e51262
NEXT_PUBLIC_SPA_APP_CLIENT_ID=471a2896-5785-4789-9c05-20077c08f75d
NEXT_PUBLIC_API_SCOPE=api://754ec9b6-b889-44bf-a6fe-2034a37647d4/access_as_user
NEXT_PUBLIC_API_BASE_URL=https://localhost:7296/ics-api
NEXT_PUBLIC_REDIRECT_URI=http://localhost:3000/ics-admin/
NEXT_PUBLIC_POST_LOGOUT_REDIRECT_URI=http://localhost:3000/ics-admin/
```

**Production (.env.production):**
```env
NEXT_PUBLIC_TENANT_ID=09131022-b785-4e6d-8d42-916975e51262
NEXT_PUBLIC_SPA_APP_CLIENT_ID=471a2896-5785-4789-9c05-20077c08f75d
NEXT_PUBLIC_API_SCOPE=api://754ec9b6-b889-44bf-a6fe-2034a37647d4/access_as_user
NEXT_PUBLIC_API_BASE_URL=https://nam-pdaparch01.americas.global-legal.com/ics-api
NEXT_PUBLIC_REDIRECT_URI=https://nam-pdaparch01.americas.global-legal.com/ics-admin/
NEXT_PUBLIC_POST_LOGOUT_REDIRECT_URI=https://nam-pdaparch01.americas.global-legal.com/ics-admin/
```

### API Environment Variables (ASP.NET Core)

Configured via `appsettings.json` hierarchy. Can also use environment variables:

```bash
# Override appsettings values
AzureAd__TenantId=09131022-b785-4e6d-8d42-916975e51262
AzureAd__ClientId=754ec9b6-b889-44bf-a6fe-2034a37647d4
Ics__UsePathBase=true
```

---

## Critical Gotchas & Troubleshooting

### 1. Trailing Slashes Matter

**Problem:** Entra ID redirect URI mismatch causes authentication to fail.

**Solution:**
- UI uses `trailingSlash: true` in `next.config.ts`
- All redirect URIs in Entra portal must include trailing slash
- Example: `https://nam-pdaparch01.americas.global-legal.com/ics-admin/` ✓
- Not: `https://nam-pdaparch01.americas.global-legal.com/ics-admin` ✗

### 2. PathBase Handling

**Problem:** API endpoints return 404 or incorrect paths.

**Solution:**
- **Development:** Set `Ics:UsePathBase=true` in `appsettings.Development.json`
- **Production:** DO NOT set `UsePathBase=true` - IIS provides the base path
- Middleware in `Program.cs:43-46` handles PathBase conditionally

### 3. Role Claims Location

**Problem:** User has roles assigned but API doesn't recognize them.

**Solution:**
- Roles must be assigned in **ICS API** Enterprise Application (not SPA app)
- Check **both** `"roles"` and `ClaimTypes.Role` claims
- Middleware in `Program.cs:50-55` handles both formats
- User must log out and log back in after role assignment changes

### 4. CORS Configuration

**Problem:** CORS errors in development or production.

**Solution:**
- **Development:** CORS enabled for `http://localhost:3000` via `DevAdminUI` policy
- **Production:** CORS not needed (same origin deployment)
- Never enable CORS in production for this architecture
- CORS policy in `Program.cs:23-28`

### 5. SPA Routing on IIS

**Problem:** Deep links (e.g., `/ics-admin/cases/123`) return 404.

**Solution:**
- Install **IIS URL Rewrite module** on server
- Ensure `web.config` exists in `out/` folder (build script auto-copies)
- Rewrite rule sends all non-file requests to `index.html`
- `deploy/web.config` in UI project

### 6. Static Export Limitations

**Problem:** Server components or API routes don't work.

**Solution:**
- This is expected with `output: "export"`
- All data fetching must be client-side
- All components must have `'use client'` directive
- No server-side features available
- Use API endpoints instead of Next.js API routes

### 7. Auditing Middleware Timing

**Problem:** Audit entries show incorrect status codes.

**Solution:**
- Middleware runs **after** response is sent (see `Program.cs:47-66`)
- This is intentional - logs actual response status
- Status code is read from `context.Response.StatusCode` after `next()` completes

### 8. Environment Variables Not Working

**Problem:** UI can't access environment variables.

**Solution:**
- All UI environment variables must be prefixed with `NEXT_PUBLIC_`
- Static export embeds variables at **build time**, not runtime
- Rebuild after changing `.env.production` file
- Check `.env.local` (development) or `.env.production` (build) exists
- Centralized access via `src/lib/env.ts` with fallback defaults

### 9. API Returns 401 for Authenticated User

**Problem:** User logged in but API rejects requests.

**Solutions:**
- Verify token audience matches API Application ID URI
- Check token is sent with `Authorization: Bearer <token>` header
- Verify API `appsettings.json` has correct `TenantId` and `ClientId`
- Check token has required scope: `api://754ec9b6-b889-44bf-a6fe-2034a37647d4/access_as_user`
- Decode JWT at jwt.ms to inspect claims

### 10. Changes Not Reflected After Deployment

**API:**
- Rebuild: `dotnet publish -c Release`
- Copy to IIS folder
- Restart IIS Application Pool: `Restart-WebAppPool -Name "IcsApiPool"`
- Or full IIS reset: `iisreset`

**UI:**
- Rebuild: `npm run build`
- Ensure `.env.production` has correct values
- Copy `out/` folder contents to IIS folder
- Restart IIS Application Pool: `Restart-WebAppPool -Name "IcsAdminPool"`
- Clear browser cache (Ctrl+Shift+R)

---

## Testing Authorization

### Manual Testing with IcsApi.http

Use `src/api/IcsApi/IcsApi.http` for manual API testing.

**Steps:**
1. Login to UI at `http://localhost:3000/ics-admin/`
2. Open browser developer tools → Network tab
3. Make a request from UI, inspect Authorization header
4. Copy Bearer token
5. Paste token in `IcsApi.http` file
6. Run HTTP requests

### Authorization Test Matrix

Test with different user roles to verify access control:

| Endpoint | Method | Admin | Attorney | Anonymous | Expected Status |
|----------|--------|-------|----------|-----------|-----------------|
| `/health` | GET | ✓ | ✓ | ✓ | 200 |
| `/me` | GET | ✓ | ✓ | ✗ | 200 / 401 |
| `/cases` | GET | ✓ | ✓ | ✗ | 200 / 401 |
| `/cases/{id}` | GET | ✓ | ✓ | ✗ | 200 / 401 |
| `/cases` | POST | ✓ | ✗ | ✗ | 200 / 403 / 401 |
| `/cases/{id}` | DELETE | ✓ | ✗ | ✗ | 204 / 403 / 401 |
| `/audit` | GET | ✓ | ✗ | ✗ | 200 / 403 / 401 |

**Response Codes:**
- `200` - Success
- `204` - Success (No Content)
- `401` - Unauthorized (not authenticated)
- `403` - Forbidden (authenticated but insufficient permissions)
- `404` - Not Found

### Creating Test Users

**In Azure Portal:**
1. Navigate to **ICS API** Enterprise Application
2. Go to **Users and groups**
3. Click **Add user/group**
4. Select user, assign `Admin` or `Attorney` role
5. Save

**Important:** User must log out and log back in for role changes to take effect.

---

## Making Changes

### Modifying Authentication/Authorization

**Changing Entra Configuration:**
- Role changes require user re-login to get updated token
- Redirect URI changes require exact match in Entra portal
- See `docs/implementation-guide.md` for complete Entra setup

**Adding New Roles:**
1. Add role to **ICS API** app manifest in Azure Portal
2. Update authorization policies in `Program.cs`
3. Assign role to users in Enterprise Application
4. Update controllers with new policies
5. Users must log out and back in

**Modifying Policies:**
```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("AdminOrAttorney", policy =>
        policy.RequireRole("Admin", "Attorney"));

    // Add new policy
    options.AddPolicy("ManagerOnly", policy =>
        policy.RequireRole("Manager"));
});
```

### Adding Dependencies

**UI (Next.js):**
```powershell
npm install <package-name>
```
- Verify package compatibility with static export (no server-side features)
- Check package size impact on bundle
- Test build: `npm run build`

**API (.NET):**
```powershell
dotnet add package <PackageName>
```
- Ensure compatibility with .NET 8
- Test on Windows Server if possible
- Update all environments after adding packages

### Changing Base Paths

**If you need to change `/ics-admin` or `/ics-api`:**

1. **UI Changes:**
   - Update `basePath` in `next.config.ts`
   - Update all environment variables (redirect URIs, etc.)

2. **API Changes:**
   - Update `UsePathBase` call in `Program.cs`

3. **Entra ID:**
   - Update redirect URIs in Azure Portal (both app registrations)

4. **IIS:**
   - Update application aliases in IIS Manager

5. **Documentation:**
   - Update all references in docs/
   - Update this file

6. **Testing:**
   - Test locally with new paths
   - Test production deployment

**Note:** This is a significant change affecting many components. Test thoroughly.

### Database Integration (Future)

Currently uses in-memory stores. To add database:

1. **Add EF Core packages:**
   ```powershell
   cd src/api/IcsApi
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   dotnet add package Microsoft.EntityFrameworkCore.Design
   ```

2. **Create DbContext:**
   ```csharp
   public class IcsDbContext : DbContext
   {
       public DbSet<Case> Cases { get; set; }
       public DbSet<AuditEntry> AuditEntries { get; set; }
   }
   ```

3. **Update Program.cs:**
   ```csharp
   builder.Services.AddDbContext<IcsDbContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("IcsDb")));
   ```

4. **Replace in-memory services with EF repositories**

5. **Update appsettings.json with connection string**

---

## Architecture Principles

This codebase follows these principles:

1. **Simplicity First** - In-memory stores, minimal dependencies, straightforward implementations
2. **Learning-Oriented** - Designed for understanding concepts, not production scale
3. **Sub-Path Consistency** - Development and production URLs mirror structure
4. **Security at API** - UI is untrusted; API enforces all authorization
5. **Static Export** - UI is pure static files, no server runtime
6. **Policy-Based Authorization** - Named policies instead of magic strings
7. **Structured Errors** - ProblemDetails for consistent error responses
8. **Comprehensive Auditing** - Middleware logs all requests after response
9. **Environment Parity** - Configuration patterns consistent across environments
10. **Documentation-First** - All decisions documented with rationale

---

## Quick Commands Reference

### Development

```powershell
# API (from src/api/IcsApi/)
dotnet run                                    # Run at https://localhost:7296/ics-api/
dotnet watch run                              # Run with hot reload
dotnet test                                   # Run tests (when added)

# UI (from src/admin/ics-admin/)
npm install                                   # Install dependencies
npm run dev                                   # Run at http://localhost:3000/ics-admin/
npm run build                                 # Build static export
npm run lint                                  # Run ESLint
```

### Building for Production

```powershell
# API
cd src/api/IcsApi
dotnet publish -c Release -o ./publish
# Output: publish/ folder

# UI
cd src/admin/ics-admin
npm run build
# Output: out/ folder (with web.config)
```

### Testing

```powershell
# Health checks
curl https://localhost:7296/ics-api/health   # API health
curl http://localhost:3000/ics-admin/        # UI (returns HTML)

# Using IcsApi.http (requires auth token)
# Open src/api/IcsApi/IcsApi.http in VS Code with REST Client extension
```

### IIS Management (on server)

```powershell
# Restart entire IIS
iisreset

# Restart specific app pool
Restart-WebAppPool -Name "IcsApiPool"
Restart-WebAppPool -Name "IcsAdminPool"

# Check app pool status
Get-WebAppPoolState -Name "IcsApiPool"
Get-WebAppPoolState -Name "IcsAdminPool"

# View IIS logs
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\*.log" -Tail 50
```

---

## Documentation References

**Read these in order for complete understanding:**

1. **[dev-run.md](docs/dev-run.md)**
   - Quick start guide for local development
   - Prerequisites and setup steps
   - Common first-time issues

2. **[system-architecture-outline.md](docs/system-architecture-outline.md)**
   - Complete design decisions and rationale
   - Token flow diagrams
   - Architecture trade-offs

3. **[implementation-guide.md](docs/implementation-guide.md)**
   - Step-by-step Entra ID setup
   - Configuration values explained
   - Common configuration mistakes

4. **[iis-deployment-guide.md](docs/iis-deployment-guide.md)**
   - Complete IIS deployment procedure
   - IIS prerequisites and configuration
   - Troubleshooting deployment issues

**Quick References:**
- `.github/copilot-instructions.md` - Coding conventions for GitHub Copilot
- `src/api/IcsApi/IcsApi.http` - API endpoint examples
- `src/admin/ics-admin/.env.example` - Environment variable template

---

## Common Questions

### Q: Why not use Next.js API routes instead of separate .NET API?

**A:** Requirements dictate .NET backend for integration with existing enterprise systems. Next.js static export can't use API routes anyway.

### Q: Why static export instead of Node.js hosting?

**A:** Windows Server infrastructure, IIS expertise, no Node.js runtime available in production environment.

### Q: Why not use Next.js middleware for authentication?

**A:** Static export doesn't support middleware. Authentication handled entirely client-side via MSAL + API validation.

### Q: Why two Entra ID app registrations?

**A:** Proper role-based authorization requires roles assigned to API access tokens. SPA app requests API scope, receives token with roles from API app.

### Q: Can I use server components?

**A:** No. Static export (`output: "export"`) requires all components to be client-side only.

### Q: How do I add a database?

**A:** See "Database Integration (Future)" section above. Currently uses in-memory stores for simplicity.

### Q: Why trailing slashes everywhere?

**A:** Next.js `trailingSlash: true` ensures consistent URLs. Entra redirect URIs must match exactly.

### Q: Can I use root paths instead of sub-paths?

**A:** No. IIS architecture requires sub-paths. Extensive configuration built around this decision.

---

## Support & Contact

**For issues with:**
- **Entra ID setup:** See `docs/implementation-guide.md`
- **IIS deployment:** See `docs/iis-deployment-guide.md`
- **Architecture questions:** See `docs/system-architecture-outline.md`
- **Local development:** See `docs/dev-run.md`
- **Coding conventions:** See `.github/copilot-instructions.md`

**This is a learning/demonstration project.** All implementation details are fully documented. Refer to the `docs/` folder for comprehensive guides.

---

## Version History

- **Initial Version** - Dual-stack sub-path deployment with Entra ID authentication
- **Current** - In-memory stores, basic CRUD operations, audit trail
- **Future** - Database integration, advanced features (TBD)

---

*Last Updated: 2025-01-08*
