# GitHub Copilot Instructions - ICS System

## MCP Tool Usage

Always use Context7 MCP when you need library/API documentation, code generation, setup or configuration steps without being explicitly asked.

## Project Type & Stack

**Dual-stack sub-path deployment:**
- Next.js 15 static export at `/ics-admin` (client-side only, no SSR)
- ASP.NET Core 8 Web API at `/ics-api`
- Microsoft Entra ID authentication with role-based authorization

**Production:** `https://nam-pdaparch01.americas.global-legal.com/`

## Critical Coding Conventions

### 1. Sub-Path Requirements
ALL URLs must include base paths:
```typescript
// UI - Always use env.apiBaseUrl
import { env } from '@/lib/env';
const response = await fetch(`${env.apiBaseUrl}/cases`);

// API - basePath handled automatically via UsePathBase
```

Configuration:
- Next.js: `basePath: "/ics-admin"` in `next.config.ts`
- API: `UsePathBase("/ics-api")` when `Ics:UsePathBase=true` (Development only)

### 2. Authorization Pattern
ALWAYS use named policies, NEVER raw role strings:
```csharp
// ✓ Correct
[Authorize(Policy = "AdminOnly")]
[Authorize(Policy = "AdminOrAttorney")]

// ✗ Wrong
[Authorize(Roles = "Admin")]
```

Policies defined: `src/api/IcsApi/Program.cs:15-19`

### 3. Next.js Static Export Constraints
This project uses `output: "export"` - the following are NOT available:
- ❌ Server components
- ❌ API routes (`/api/...`)
- ❌ Server-side rendering (SSR)
- ❌ Image optimization
- ❌ `getServerSideProps`, `getStaticPaths`

ALL components must be client-side only:
```typescript
'use client';  // Required for all interactive components
```

### 4. Environment Variables
UI variables MUST be prefixed with `NEXT_PUBLIC_`:
```bash
NEXT_PUBLIC_API_BASE_URL=...  # ✓ Works
API_BASE_URL=...              # ✗ Not accessible
```

Centralized in: `src/admin/ics-admin/src/lib/env.ts`

### 5. API Response Pattern
Use `ProblemDetails` for all error responses:
```csharp
return Problem(
    statusCode: StatusCodes.Status404NotFound,
    title: "Case not found",
    detail: $"Case with ID {id} does not exist"
);
```

## File Structure Quick Reference

```
src/
├── admin/ics-admin/
│   ├── next.config.ts           # basePath, static export config
│   ├── deploy/web.config        # IIS rewrite rules
│   └── src/
│       ├── lib/
│       │   ├── env.ts           # Environment variables (USE THIS)
│       │   └── msal.ts          # Auth config
│       └── app/                 # Pages (all client-side)
│
└── api/IcsApi/
    ├── Program.cs               # Auth, policies, CORS, auditing
    ├── appsettings*.json        # Per-environment config
    ├── Controllers/             # Add new endpoints here
    └── Services/                # Business logic
```

## Common Development Tasks

### Adding a New API Endpoint
1. Create controller in `Controllers/` folder
2. Apply authorization: `[Authorize(Policy = "AdminOnly")]`
3. Return `ProblemDetails` for errors
4. Test with `IcsApi.http` file

### Adding a New UI Page
1. Create in `src/app/` folder
2. Add `'use client'` directive at top
3. Use `env.apiBaseUrl` for API calls
4. Include `Authorization: Bearer ${token}` header
5. Check user roles from decoded JWT before rendering admin features

### Environment Configuration
**Local development:**
1. API: Uses `appsettings.Development.json` (sets `UsePathBase=true`)
2. UI: Create `.env.local` from `.env.example`

**Production build:**
1. API: `dotnet publish -c Release -o ./publish`
2. UI: `npm run build` (auto-copies `deploy/web.config`)

## Critical Don'ts

1. ❌ Don't use root paths - always include `/ics-admin/` or `/ics-api/`
2. ❌ Don't use server components or API routes in Next.js
3. ❌ Don't use raw role strings in `[Authorize]` attributes
4. ❌ Don't set `UsePathBase=true` in production appsettings
5. ❌ Don't enable CORS in production (same-origin deployment)
6. ❌ Don't forget trailing slashes - redirect URIs must match exactly

## Quick Commands

```powershell
# API Development (from src/api/IcsApi/)
dotnet run                                # Run at https://localhost:7296/ics-api/
dotnet publish -c Release -o ./publish    # Build for IIS

# UI Development (from src/admin/ics-admin/)
npm run dev                               # Run at http://localhost:3000/ics-admin/
npm run build                             # Build static export

# Health checks
curl https://localhost:7296/ics-api/health
```

## Where to Find Detailed Information

For comprehensive documentation, see `AGENTS.md`:
- Architecture rationale and design decisions
- Entra ID configuration details
- Troubleshooting common issues
- Complete environment variable examples
- IIS deployment procedures
- Testing authorization matrix

For step-by-step guides, see `docs/` folder:
- `dev-run.md` - Local development setup
- `system-architecture-outline.md` - Design decisions
- `implementation-guide.md` - Entra ID configuration
- `iis-deployment-guide.md` - Production deployment
