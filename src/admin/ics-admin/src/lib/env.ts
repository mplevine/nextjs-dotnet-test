export const env = {
  tenantId: process.env.NEXT_PUBLIC_TENANT_ID ?? "TENANT_ID",
  spaClientId: process.env.NEXT_PUBLIC_SPA_APP_CLIENT_ID ?? "SPA_APP_CLIENT_ID",
  apiScope: process.env.NEXT_PUBLIC_API_SCOPE ?? "API_SCOPE",
  apiBaseUrl:
    process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7296/ics-api",
  redirectUri:
    process.env.NEXT_PUBLIC_REDIRECT_URI ?? "http://localhost:3000/ics-admin/",
  postLogoutRedirectUri:
    process.env.NEXT_PUBLIC_POST_LOGOUT_REDIRECT_URI ??
    "http://localhost:3000/ics-admin/",
} as const;
