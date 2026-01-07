# Implementation Guide (Entra ID + Next.js Static + .NET API on IIS)

Target host: `https://nam-pdaparch01.americas.global-net.com`
- Admin UI: `https://nam-pdaparch01.americas.global-net.com/ics-admin/`
- API: `https://nam-pdaparch01.americas.global-net.com/ics-api/`

This guide is intentionally **learning-first** and keeps the setup minimal.

---

## 0) Values you must choose (fill these in once)

Create these placeholders and reuse them everywhere:

- `TENANT_ID` = `<your-tenant-guid>` (Directory (tenant) ID)
- `TENANT_NAME` = `<your-tenant-name>.onmicrosoft.com` (optional, for reference)
- `API_APP_CLIENT_ID` = `<ICS-API-application-client-id>`
- `SPA_APP_CLIENT_ID` = `<ICS-Admin-application-client-id>`
- `API_SCOPE` = `api://API_APP_CLIENT_ID/access_as_user`

Notes:
- Prefer `TENANT_ID` (GUID) in config rather than tenant name.
- You can retrieve IDs in Entra Portal → App registrations → Overview.

---

## 1) Entra ID App Registration: **ICS API** (Web API)

Entra Portal → **App registrations** → **New registration**

- Name: `ICS API`
- Supported account types: **Single tenant** (your choice in answers)
- Redirect URI: (none needed for a pure Web API)

### 1.1 Expose an API
Entra Portal → App registrations → `ICS API` → **Expose an API**

1) **Application ID URI**
- Set: `api://API_APP_CLIENT_ID`

2) **Scopes**
- Add scope:
  - Scope name: `access_as_user`
  - Who can consent: `Admins and users` (fine for learning; many orgs prefer admin-only)
  - Admin consent display name: `Access ICS API`
  - Admin consent description: `Allows the app to access ICS API as the signed-in user.`
  - User consent display name: `Access ICS API`
  - User consent description: `Allows you to access ICS API as you.`

Recommended: keep **exactly one** delegated scope for this example.

### 1.2 App roles
Entra Portal → App registrations → `ICS API` → **App roles**

Create these roles (Allowed member types: **Users/Groups**):

- Display name: `Admin`
  - Value: `Admin`
  - Description: `ICS administrators`
- Display name: `Attorney`
  - Value: `Attorney`
  - Description: `ICS attorneys`

### 1.3 Token version (recommended)
Entra Portal → App registrations → `ICS API` → **Manifest**

- Ensure `accessTokenAcceptedVersion` is `2`.

If it’s missing, add:
```json
"accessTokenAcceptedVersion": 2
```

### 1.4 Assign users/groups to roles
Entra Portal → **Enterprise applications** → `ICS API` → **Users and groups**

- Assign your test user to role `Admin`.
- Optionally assign a group to `Attorney` and add users to that group.

Why Enterprise applications?
- That’s where role assignments actually occur for the service principal in your tenant.

---

## 2) Entra ID App Registration: **ICS Admin Interface** (SPA)

Entra Portal → **App registrations** → **New registration**

- Name: `ICS Admin Interface`
- Supported account types: **Single tenant**
- Redirect URI:
  - Platform: **Single-page application (SPA)**
  - URI: `https://nam-pdaparch01.americas.global-net.com/ics-admin/`

### 2.1 Add redirect URIs
Entra Portal → App registrations → `ICS Admin Interface` → **Authentication**

Add (recommended for static hosting):
- `https://nam-pdaparch01.americas.global-net.com/ics-admin/`
- `https://nam-pdaparch01.americas.global-net.com/ics-admin/index.html`

For local dev (if you use it):
- `http://localhost:3000/ics-admin/`
- `http://localhost:3000/ics-admin`

Also set:
- **Front-channel logout URL**: `https://nam-pdaparch01.americas.global-net.com/ics-admin/`

### 2.2 API permissions
Entra Portal → App registrations → `ICS Admin Interface` → **API permissions**

- Add a permission → **My APIs** → `ICS API`
- Choose **Delegated permissions** → `access_as_user`
- Click **Grant admin consent** (if required in your org)

---

## 3) Next.js (static export) configuration for `/ics-admin`

You’ll want Next.js to generate assets that work under a sub-path.

Recommended Next config (conceptual):
- `basePath`: `/ics-admin`
- `assetPrefix`: `/ics-admin/`
- `output`: `export`

This typically lives in `next.config.js`.

Also ensure your app routes assume the base path; avoid hardcoding `/`.

---

## 4) MSAL configuration (SPA) tailored to `/ics-admin`

Below is a working baseline configuration.

### 4.1 `msalConfig`
```ts
import { LogLevel, type Configuration } from "@azure/msal-browser";

const tenantId = "TENANT_ID";

export const msalConfig: Configuration = {
  auth: {
    clientId: "SPA_APP_CLIENT_ID",
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri: "https://nam-pdaparch01.americas.global-net.com/ics-admin/",
    postLogoutRedirectUri: "https://nam-pdaparch01.americas.global-net.com/ics-admin/",
    navigateToLoginRequestUrl: false,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        // optionally route to console in dev
        // console.log(message);
      },
      logLevel: LogLevel.Info,
    },
  },
};
```

### 4.2 Login + token request settings
The API scope is based on the API app registration:

- `API_SCOPE` = `api://API_APP_CLIENT_ID/access_as_user`

```ts
export const loginRequest = {
  scopes: ["API_SCOPE"],
};
```

### 4.3 Calling the API with a bearer token
Acquire a token and call the API:
- Base URL: `https://nam-pdaparch01.americas.global-net.com/ics-api`
- Example endpoint: `GET /ics-api/cases`

Attach header:
- `Authorization: Bearer <access_token>`

### 4.4 Role check in the UI
Once you have an access token for the API, decode it and check:
- `roles` includes `Admin`

If missing:
- show “Not authorized” and avoid rendering admin features.

Important: UI checks are convenience only; API enforces real security.

---

## 5) ASP.NET Core API configuration (Microsoft.Identity.Web)

This section assumes:
- API hosted at `https://nam-pdaparch01.americas.global-net.com/ics-api/`
- Auth is **Entra only** (no Windows Integrated)

### 5.1 `appsettings.json` (example)
Use these values in the API project’s configuration:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "TENANT_ID",
    "ClientId": "API_APP_CLIENT_ID"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Why `ClientId` here?
- For Web APIs, `Microsoft.Identity.Web` uses this to validate the token audience depending on how you wire it in Program.cs.

### 5.2 Program.cs (conceptual wiring)
Typical setup (conceptual):
- Add JWT bearer auth via `Microsoft.Identity.Web`
- Add authorization
- Use role-based attributes like `[Authorize(Roles = "Admin")]`

Notes:
- Ensure you validate **audience** consistent with your `api://...` Application ID URI.
- Keep the API behind HTTPS in IIS.

---

## 6) Local development vs IIS production

Because your production URLs are sub-paths (`/ics-admin`, `/ics-api`), keep them consistent in dev to avoid surprises.

Recommended:
- Run Next dev server so your app is reachable at `http://localhost:3000/ics-admin/`
- Run API at `https://localhost:<port>/` but configure the SPA to call the correct base URL depending on environment.

---

## 7) Auditing (simple, but useful)

For an in-memory learning app, the simplest “audit trail” is **structured logs** plus an Admin-only endpoint.

Audit event fields (recommended):
- timestamp (UTC)
- user object id (`oid` claim)
- username/UPN (`preferred_username`)
- roles (`roles`)
- method + path
- status code
- correlation id (ASP.NET `TraceIdentifier`)

---

## 8) Common gotchas on IIS (static SPA)

- Ensure IIS serves `.js`/`.css`/`.map` correctly.
- SPA routing: because you have client-side routes like `/ics-admin/cases/123`, you need an IIS rewrite rule to send unknown paths back to the SPA entrypoint.
- Redirect URIs must exactly match (including trailing slashes).

### 8.1 IIS rewrite rule for deep links (required)

Prerequisite: install/enable the **IIS URL Rewrite** module on the server.

Create a `web.config` file in the **physical folder that IIS serves for** `/ics-admin` (i.e., the folder containing the exported `index.html`). Use this minimal rule:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="SPA Deep Links" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="index.html" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

What it does:
- Requests for real files (e.g., `main.js`, images) are served normally.
- All other paths under `/ics-admin/*` rewrite to `index.html`, letting the client router handle the route.

---

## 9) Quick verification steps

1) Sign in to the UI and confirm MSAL obtains a token for `API_SCOPE`.
2) Decode the API access token (JWT) and verify it contains:
   - `aud` = API
   - `roles` includes `Admin` when assigned
3) Call a protected API endpoint:
   - Unauthenticated → `401`
   - Authenticated but missing role → `403`
   - Correct role → `200`

---

If you want, I can also add a concrete IIS `web.config` rewrite example for the SPA routes under `/ics-admin` and a minimal example `Program.cs` using `Microsoft.Identity.Web` with role-based authorization.
