// OIDC (Authorization Code + PKCE) auth against the Dex test OpenID provider (ADR-0007). The browser
// redirects to Dex to sign in, then exchanges the code for an id-token which is sent as a Bearer to
// HappyPumi (validated server-side against Dex's JWKS; the `groups` claim drives RBAC).
//
// The authorize step is a top-level navigation directly to Dex (no CORS); the token exchange is a fetch
// routed through the dev proxy (/dex -> Dex) so it stays same-origin and avoids CORS.

const ISSUER = "http://localhost:5556/dex"; // Dex issuer (browser navigates here for the login page)
const TOKEN_URL = "/dex/token"; // proxied to Dex for the same-origin code exchange
const CLIENT_ID = "happypumi-console";
const SCOPE = "openid profile email groups";
const KEY = "happypumi.token";
const VERIFIER = "happypumi.pkce_verifier";
const STATE = "happypumi.oauth_state";

function redirectUri(): string {
  return `${window.location.origin}/callback`;
}

// ── token storage ─────────────────────────────────────────────────────────────
export function getToken(): string | null {
  return localStorage.getItem(KEY);
}

export function clearToken(): void {
  localStorage.removeItem(KEY);
}

/** Authenticated when a non-expired id-token is stored. */
export function isAuthenticated(): boolean {
  const token = getToken();
  if (!token) return false;
  const exp = jwtExp(token);
  return exp === null || exp * 1000 > Date.now();
}

function jwtExp(token: string): number | null {
  const parts = token.split(".");
  if (parts.length !== 3) return null; // not a JWT (e.g. a dev token) — treat as non-expiring
  try {
    return JSON.parse(atob(parts[1].replace(/-/g, "+").replace(/_/g, "/"))).exp ?? null;
  } catch {
    return null;
  }
}

// ── PKCE ──────────────────────────────────────────────────────────────────────
function randomString(bytes = 32): string {
  return base64url(crypto.getRandomValues(new Uint8Array(bytes)));
}

function base64url(buffer: ArrayBuffer | Uint8Array): string {
  const bytes = buffer instanceof Uint8Array ? buffer : new Uint8Array(buffer);
  let s = "";
  for (const b of bytes) s += String.fromCharCode(b);
  return btoa(s).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

async function challenge(verifier: string): Promise<string> {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier));
  return base64url(digest);
}

// ── flow ────────────────────────────────────────────────────────────────────
/** Begins the OIDC login: redirects the browser to Dex's authorization endpoint. */
export async function login(): Promise<void> {
  const verifier = randomString();
  const state = randomString(16);
  sessionStorage.setItem(VERIFIER, verifier);
  sessionStorage.setItem(STATE, state);
  const params = new URLSearchParams({
    response_type: "code",
    client_id: CLIENT_ID,
    redirect_uri: redirectUri(),
    scope: SCOPE,
    state,
    code_challenge: await challenge(verifier),
    code_challenge_method: "S256",
  });
  window.location.href = `${ISSUER}/auth?${params}`;
}

/** Completes the OIDC callback: exchanges the code for an id-token and stores it. Throws on failure. */
export async function handleCallback(): Promise<void> {
  const url = new URLSearchParams(window.location.search);
  const code = url.get("code");
  const state = url.get("state");
  const error = url.get("error");
  if (error) throw new Error(url.get("error_description") || error);
  if (!code) throw new Error("Missing authorization code.");
  if (state !== sessionStorage.getItem(STATE)) throw new Error("State mismatch — possible CSRF.");

  const verifier = sessionStorage.getItem(VERIFIER);
  if (!verifier) throw new Error("Missing PKCE verifier — restart sign-in.");

  const res = await fetch(TOKEN_URL, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      grant_type: "authorization_code",
      code,
      redirect_uri: redirectUri(),
      client_id: CLIENT_ID,
      code_verifier: verifier,
    }),
  });
  if (!res.ok) throw new Error(`Token exchange failed (${res.status}).`);
  const tokens = (await res.json()) as { id_token?: string };
  if (!tokens.id_token) throw new Error("No id_token in token response.");

  localStorage.setItem(KEY, tokens.id_token);
  sessionStorage.removeItem(VERIFIER);
  sessionStorage.removeItem(STATE);
}
