import { LogLevel, PublicClientApplication, type Configuration } from "@azure/msal-browser";
import { env } from "./env";

export const msalConfig: Configuration = {
  auth: {
    clientId: env.spaClientId,
    authority: `https://login.microsoftonline.com/${env.tenantId}`,
    redirectUri: env.redirectUri,
    postLogoutRedirectUri: env.postLogoutRedirectUri,
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

export const loginRequest = {
  scopes: [env.apiScope],
};

export const msalInstance = new PublicClientApplication(msalConfig);
