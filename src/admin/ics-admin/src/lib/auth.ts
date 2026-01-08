import type { AccountInfo, AuthenticationResult, InteractionStatus } from "@azure/msal-browser";
import { InteractionRequiredAuthError, BrowserAuthError } from "@azure/msal-browser";
import { jwtDecode } from "jwt-decode";
import { env } from "./env";
import { loginRequest, msalInstance } from "./msal";

type JwtRoles = {
  roles?: string[];
};

// Singleton promise to ensure initialize() and handleRedirectPromise() are only called once
let initPromise: Promise<void> | null = null;

async function ensureInitialized(): Promise<void> {
  if (!initPromise) {
    initPromise = (async () => {
      await msalInstance.initialize();
      // handleRedirectPromise must be called once after initialize to process any redirect response
      const result = await msalInstance.handleRedirectPromise();
      if (result?.account) {
        msalInstance.setActiveAccount(result.account);
      }
    })();
  }
  return initPromise;
}

export function getActiveAccount(): AccountInfo | null {
  const active = msalInstance.getActiveAccount();
  if (active) return active;

  const accounts = msalInstance.getAllAccounts();
  return accounts.length > 0 ? accounts[0] : null;
}

export async function ensureSignedIn(): Promise<AccountInfo> {
  await ensureInitialized();

  const existing = getActiveAccount();
  if (existing) {
    msalInstance.setActiveAccount(existing);
    return existing;
  }

  // Check if an interaction is already in progress
  // @ts-expect-error - accessing internal state for safety check
  const inProgress = msalInstance.interactionInProgress?.();
  if (inProgress) {
    throw new Error("Redirecting to login");
  }

  await msalInstance.loginRedirect(loginRequest);
  throw new Error("Redirecting to login");
}

export async function acquireApiToken(account: AccountInfo): Promise<AuthenticationResult> {
  await ensureInitialized();

  try {
    return await msalInstance.acquireTokenSilent({
      ...loginRequest,
      account,
    });
  } catch (e) {
    // Only redirect for interaction-required errors, not for in-progress errors
    if (e instanceof InteractionRequiredAuthError) {
      await msalInstance.acquireTokenRedirect({
        ...loginRequest,
        account,
      });
      throw new Error("Redirecting to acquire token");
    }
    // If interaction is in progress, just throw to avoid duplicate redirects
    if (e instanceof BrowserAuthError && e.errorCode === "interaction_in_progress") {
      throw new Error("Redirecting to acquire token");
    }
    throw e;
  }
}

export function hasAdminRole(accessToken: string): boolean {
  try {
    const decoded = jwtDecode<JwtRoles>(accessToken);
    return (decoded.roles ?? []).some((r) => r.toLowerCase() === "admin");
  } catch {
    return false;
  }
}

export async function fetchApi<T>(
  path: string,
  accessToken: string,
  init?: RequestInit
): Promise<T> {
  const url = `${env.apiBaseUrl}${path.startsWith("/") ? "" : "/"}${path}`;
  const response = await fetch(url, {
    ...init,
    headers: {
      ...(init?.headers ?? {}),
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json",
    },
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(`${response.status} ${response.statusText}: ${text}`);
  }

  return (await response.json()) as T;
}
