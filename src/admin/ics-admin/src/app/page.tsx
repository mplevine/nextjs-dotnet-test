"use client";

import { useEffect, useMemo, useState } from "react";
import { acquireApiToken, ensureSignedIn, fetchApi, hasAdminRole } from "@/lib/auth";
import { env } from "@/lib/env";
import { msalInstance } from "@/lib/msal";

type MeResponse = {
  oid?: string;
  username?: string;
  roles: string[];
};

type CaseItem = {
  id: string;
  title: string;
  status: string;
  createdUtc: string;
};

export default function Home() {
  const [status, setStatus] = useState<"starting" | "ready" | "not-authorized" | "error">(
    "starting"
  );
  const [error, setError] = useState<string | null>(null);
  const [me, setMe] = useState<MeResponse | null>(null);
  const [cases, setCases] = useState<CaseItem[] | null>(null);

  const helpText = useMemo(() => {
    return {
      tenantId: env.tenantId,
      spaClientId: env.spaClientId,
      apiScope: env.apiScope,
      apiBaseUrl: env.apiBaseUrl,
      redirectUri: env.redirectUri,
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function boot() {
      try {
        const account = await ensureSignedIn();
        if (cancelled) return;

        const tokenResult = await acquireApiToken(account);
        if (cancelled) return;

        const isAdmin = hasAdminRole(tokenResult.accessToken);
        if (!isAdmin) {
          setStatus("not-authorized");
          return;
        }

        setStatus("ready");
        setError(null);

        const meResponse = await fetchApi<MeResponse>("/me", tokenResult.accessToken);
        const casesResponse = await fetchApi<CaseItem[]>("/cases", tokenResult.accessToken);

        if (cancelled) return;
        setMe(meResponse);
        setCases(casesResponse);
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        if (msg.includes("Redirecting")) return;
        setStatus("error");
        setError(msg);
      }
    }

    boot();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <main style={{ maxWidth: 960, margin: "0 auto", padding: 24, fontFamily: "system-ui" }}>
      <h1 style={{ marginBottom: 8 }}>ICS Admin Interface</h1>
      <p style={{ marginTop: 0, color: "#666" }}>
        Base path: <code>/ics-admin/</code> | API: <code>{env.apiBaseUrl}</code>
      </p>

      <div style={{ display: "flex", gap: 12, marginTop: 16, marginBottom: 16 }}>
        <button
          onClick={async () => {
            await msalInstance.logoutRedirect();
          }}
        >
          Sign out
        </button>
      </div>

      {status === "starting" && <p>Signing in...</p>}

      {status === "not-authorized" && (
        <section>
          <h2>Not authorized</h2>
          <p>You're signed in, but your API access token does not contain the Admin role.</p>
          <p>
            Fix: assign your user (or group) the <code>Admin</code> app role on the <code>ICS API</code> Enterprise
            Application.
          </p>
        </section>
      )}

      {status === "error" && (
        <section>
          <h2>Boot error</h2>
          <pre style={{ background: "#f6f6f6", padding: 12, overflow: "auto" }}>{error}</pre>
          <h3>Config (dev defaults)</h3>
          <pre style={{ background: "#f6f6f6", padding: 12, overflow: "auto" }}>
            {JSON.stringify(helpText, null, 2)}
          </pre>
        </section>
      )}

      {status === "ready" && (
        <section>
          <h2>Session</h2>
          <pre style={{ background: "#f6f6f6", padding: 12, overflow: "auto" }}>{JSON.stringify(me, null, 2)}</pre>

          <h2>Cases</h2>
          {!cases ? (
            <p>Loading cases...</p>
          ) : (
            <ul>
              {cases.map((c) => (
                <li key={c.id}>
                  <strong>{c.id}</strong> - {c.title} ({c.status})
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </main>
  );
}
