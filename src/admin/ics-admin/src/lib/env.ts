export const env = {
  tenantId: process.env.NEXT_PUBLIC_TENANT_ID ?? "09131022-b785-4e6d-8d42-916975e51262",
  spaClientId: process.env.NEXT_PUBLIC_SPA_APP_CLIENT_ID ?? "471a2896-5785-4789-9c05-20077c08f75d",
  apiScope: process.env.NEXT_PUBLIC_API_SCOPE ?? "api://754ec9b6-b889-44bf-a6fe-2034a37647d4/access_as_user",
  apiBaseUrl:
    process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5179/ics-api",
  redirectUri:
    process.env.NEXT_PUBLIC_REDIRECT_URI ?? "http://localhost:3000/ics-admin/",
  postLogoutRedirectUri:
    process.env.NEXT_PUBLIC_POST_LOGOUT_REDIRECT_URI ??
    "http://localhost:3000/ics-admin/",
} as const;
