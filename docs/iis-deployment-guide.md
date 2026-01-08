# IIS Deployment Guide

This guide covers deploying the ICS Admin UI (Next.js) and ICS API (.NET 8) to an on-premise IIS server.

**Target Server:** `nam-pdaparch01.americas.global-legal.com`

## Overview

| Component | Technology | IIS Path | URL |
|-----------|------------|----------|-----|
| Admin UI | Next.js (Static Export) | `/ics-admin` | `https://nam-pdaparch01.americas.global-legal.com/ics-admin/` |
| API | ASP.NET Core 8 | `/ics-api` | `https://nam-pdaparch01.americas.global-legal.com/ics-api/` |

---

## Prerequisites

### On the IIS Server

1. **Windows Server** with IIS enabled
2. **ASP.NET Core Hosting Bundle for .NET 8**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Install the "Hosting Bundle" (not just the runtime)
   - Restart IIS after installation: `iisreset`
3. **IIS URL Rewrite Module** (for Next.js routing)
   - Download from: https://www.iis.net/downloads/microsoft/url-rewrite
4. **SSL Certificate** configured for `nam-pdaparch01.americas.global-legal.com`

### On Your Development Machine

1. .NET 8 SDK
2. Node.js 18+ and npm
3. Access to deploy files to the IIS server (file share or remote desktop)

---

## Part 1: Deploy the ICS API

### Step 1.1: Configure Production Settings

Create or update `appsettings.Production.json` in `src/api/IcsApi/`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "09131022-b785-4e6d-8d42-916975e51262",
    "ClientId": "754ec9b6-b889-44bf-a6fe-2034a37647d4"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "nam-pdaparch01.americas.global-legal.com"
}
```

### Step 1.2: Publish the API

From the `src/api/IcsApi` directory, run:

```powershell
dotnet publish -c Release -o ./publish
```

This creates a `publish` folder with all required files.

### Step 1.3: Create the IIS Application Pool

1. Open **IIS Manager** on the server
2. Right-click **Application Pools** → **Add Application Pool**
3. Configure:
   - **Name:** `IcsApiPool`
   - **.NET CLR Version:** `No Managed Code`
   - **Managed Pipeline Mode:** `Integrated`
4. Click **OK**
5. Select the new pool → **Advanced Settings**:
   - **Identity:** Set to a service account with appropriate permissions (or use `ApplicationPoolIdentity`)
   - **Start Mode:** `AlwaysRunning` (optional, for faster cold starts)

### Step 1.4: Create the IIS Application

1. In IIS Manager, expand **Sites** → select your site (e.g., `Default Web Site` or your custom site bound to `nam-pdaparch01.americas.global-legal.com`)
2. Right-click the site → **Add Application**
3. Configure:
   - **Alias:** `ics-api`
   - **Application Pool:** `IcsApiPool`
   - **Physical Path:** `D:\inetpub\ics-api` (or your preferred location)
4. Click **OK**

### Step 1.5: Deploy the API Files

Copy the contents of the `publish` folder to the physical path:

```powershell
# From your development machine (adjust paths as needed)
Copy-Item -Path ".\publish\*" -Destination "\\nam-pdaparch01\D$\inetpub\ics-api\" -Recurse -Force
```

### Step 1.6: Create web.config for the API

The publish process should create a `web.config`, but verify it exists in the deployment folder with content similar to:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\IcsApi.dll" 
                  stdoutLogEnabled="true" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="InProcess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

### Step 1.7: Create Logs Directory

Create a `logs` folder in the API deployment directory for stdout logs:

```powershell
New-Item -Path "D:\inetpub\ics-api\logs" -ItemType Directory -Force
```

Ensure the Application Pool identity has write permissions to this folder.

### Step 1.8: Verify API Deployment

1. Open a browser and navigate to: `https://nam-pdaparch01.americas.global-legal.com/ics-api/health`
2. You should see: `{"status":"ok"}`

---

## Part 2: Deploy the ICS Admin UI

### Step 2.1: Configure Production Environment

Create `.env.production` in `src/admin/ics-admin/`:

```env
NEXT_PUBLIC_TENANT_ID=09131022-b785-4e6d-8d42-916975e51262
NEXT_PUBLIC_SPA_APP_CLIENT_ID=471a2896-5785-4789-9c05-20077c08f75d
NEXT_PUBLIC_API_SCOPE=api://754ec9b6-b889-44bf-a6fe-2034a37647d4/access_as_user
NEXT_PUBLIC_API_BASE_URL=https://nam-pdaparch01.americas.global-legal.com/ics-api
NEXT_PUBLIC_REDIRECT_URI=https://nam-pdaparch01.americas.global-legal.com/ics-admin/
NEXT_PUBLIC_POST_LOGOUT_REDIRECT_URI=https://nam-pdaparch01.americas.global-legal.com/ics-admin/
```

### Step 2.2: Update next.config.ts for Static Export

Update `src/admin/ics-admin/next.config.ts`:

```typescript
import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "export",
  basePath: "/ics-admin",
  trailingSlash: true,
};

export default nextConfig;
```

### Step 2.3: Build the UI

From the `src/admin/ics-admin` directory:

```powershell
npm run build
```

This creates an `out` folder with the static export.

### Step 2.4: Create the IIS Application Pool (Optional)

Since this is static content, you can use an existing pool or the DefaultAppPool. If you want a dedicated pool:

1. Open **IIS Manager**
2. Right-click **Application Pools** → **Add Application Pool**
3. Configure:
   - **Name:** `IcsAdminPool`
   - **.NET CLR Version:** `No Managed Code`
   - **Managed Pipeline Mode:** `Integrated`

### Step 2.5: Create the IIS Application

1. In IIS Manager, expand **Sites** → select your site
2. Right-click the site → **Add Application**
3. Configure:
   - **Alias:** `ics-admin`
   - **Application Pool:** `IcsAdminPool` (or `DefaultAppPool`)
   - **Physical Path:** `D:\inetpub\ics-admin`
4. Click **OK**

### Step 2.6: Deploy the UI Files

Copy the contents of the `out` folder to the physical path:

```powershell
# From your development machine
Copy-Item -Path ".\out\*" -Destination "\\nam-pdaparch01\D$\inetpub\ics-admin\" -Recurse -Force
```

### Step 2.7: Configure web.config for SPA Routing

Create/update `web.config` in `D:\inetpub\ics-admin\`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <staticContent>
      <remove fileExtension=".json" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
    </staticContent>
    <rewrite>
      <rules>
        <rule name="SPA Routes" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="index.html" />
        </rule>
      </rules>
    </rewrite>
    <httpProtocol>
      <customHeaders>
        <add name="X-Content-Type-Options" value="nosniff" />
        <add name="X-Frame-Options" value="DENY" />
        <add name="X-XSS-Protection" value="1; mode=block" />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
</configuration>
```

### Step 2.8: Verify UI Deployment

1. Open a browser and navigate to: `https://nam-pdaparch01.americas.global-legal.com/ics-admin/`
2. You should see the login page

---

## Part 3: Update Entra ID App Registration

### Update Redirect URIs for Production

1. Go to **Azure Portal** → **Microsoft Entra ID** → **App registrations**
2. Select the **ICS Admin Interface** app (`471a2896-5785-4789-9c05-20077c08f75d`)
3. Go to **Authentication**
4. Under **Single-page application** redirect URIs, add:
   - `https://nam-pdaparch01.americas.global-legal.com/ics-admin/`
5. Under **Front-channel logout URL**, optionally set:
   - `https://nam-pdaparch01.americas.global-legal.com/ics-admin/`
6. Click **Save**

---

## Part 4: Troubleshooting

### API Issues

**500 Internal Server Error:**
- Check stdout logs in `D:\inetpub\ics-api\logs\`
- Verify the ASP.NET Core Hosting Bundle is installed
- Ensure the Application Pool identity has read access to the deployment folder

**502.5 Process Failure:**
- Verify .NET 8 runtime is installed: `dotnet --list-runtimes`
- Check Event Viewer → Windows Logs → Application for errors
- Ensure `web.config` points to the correct DLL name

**401 Unauthorized:**
- Verify the `AzureAd` settings in `appsettings.Production.json`
- Ensure the API app registration is correctly configured in Entra ID

### UI Issues

**404 on Page Refresh:**
- Ensure URL Rewrite module is installed
- Verify `web.config` exists with the rewrite rules

**MSAL Redirect Errors:**
- Verify the redirect URI in Entra ID exactly matches the URL (including trailing slash)
- Check browser console for CORS or other errors

**API Calls Failing:**
- Verify `NEXT_PUBLIC_API_BASE_URL` points to the correct production URL
- Check browser Network tab for the actual request URL

### Common IIS Commands

```powershell
# Restart IIS
iisreset

# Restart specific Application Pool
Restart-WebAppPool -Name "IcsApiPool"

# Check if site is running
Get-Website | Where-Object {$_.Name -eq "Default Web Site"}

# Check Application Pool status
Get-WebAppPoolState -Name "IcsApiPool"
```

---

## Part 5: Deployment Checklist

### Pre-Deployment
- [ ] ASP.NET Core Hosting Bundle installed on server
- [ ] IIS URL Rewrite Module installed on server
- [ ] SSL certificate configured for the domain
- [ ] Entra ID redirect URIs updated for production

### API Deployment
- [ ] Created `appsettings.Production.json` with correct settings
- [ ] Published API with `dotnet publish -c Release`
- [ ] Created Application Pool (`IcsApiPool`)
- [ ] Created IIS Application (`/ics-api`)
- [ ] Copied publish files to server
- [ ] Created logs directory with write permissions
- [ ] Verified health endpoint returns `{"status":"ok"}`

### UI Deployment
- [ ] Created `.env.production` with production URLs
- [ ] Updated `next.config.ts` for static export
- [ ] Built UI with `npm run build`
- [ ] Created IIS Application (`/ics-admin`)
- [ ] Copied out folder contents to server
- [ ] Created/verified `web.config` with rewrite rules
- [ ] Verified login page loads

### Post-Deployment
- [ ] Tested login flow end-to-end
- [ ] Verified API calls work from UI
- [ ] Tested role-based access (Admin vs Attorney)
- [ ] Verified logout flow works correctly

---

## Quick Reference Commands

### Build and Deploy API

```powershell
# From src/api/IcsApi
dotnet publish -c Release -o ./publish
Copy-Item -Path ".\publish\*" -Destination "\\nam-pdaparch01\D$\inetpub\ics-api\" -Recurse -Force
```

### Build and Deploy UI

```powershell
# From src/admin/ics-admin
npm run build
Copy-Item -Path ".\out\*" -Destination "\\nam-pdaparch01\D$\inetpub\ics-admin\" -Recurse -Force
```
