import type { AccountInfo, AuthenticationResult } from "@azure/msal-browser";
import { jwtDecode } from "jwt-decode";
import { env } from "./env";
import { loginRequest, msalInstance } from "./msal";

type JwtRoles = {
  roles?: string[];
};

export function getActiveAccount(): AccountInfo | null {
  const active = msalInstance.getActiveAccount();
  if (active) return active;

  const accounts = msalInstance.getAllAccounts();
  return accounts.length > 0 ? accounts[0] : null;
}

export async function ensureSignedIn(): Promise<AccountInfo> {
  await msalInstance.initialize();

  const result = await msalInstance.handleRedirectPromise();
  if (result?.account) {
    msalInstance.setActiveAccount(result.account);
    return result.account;
  }

  const existing = getActiveAccount();
  if (existing) {
    msalInstance.setActiveAccount(existing);
    return existing;
  }

  await msalInstance.loginRedirect(loginRequest);
  throw new Error("Redirecting to login");
}

export async function acquireApiToken(account: AccountInfo): Promise<AuthenticationResult> {
  await msalInstance.initialize();

  try {
    return await msalInstance.acquireTokenSilent({
      ...loginRequest,
      account,
    });
  } catch {
    await msalInstance.acquireTokenRedirect({
      ...loginRequest,
      account,
    });
    throw new Error("Redirecting to acquire token");
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
