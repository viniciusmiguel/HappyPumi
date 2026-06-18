import { useState } from "react";
import { LogIn, Loader2 } from "lucide-react";
import { login } from "../lib/auth";

export default function Login() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  async function signIn() {
    setBusy(true);
    setError("");
    try {
      await login(); // redirects the browser to Dex
    } catch (e) {
      setBusy(false);
      setError(e instanceof Error ? e.message : "Could not start sign-in.");
    }
  }

  return (
    <div className="grid min-h-screen place-items-center bg-bg px-4">
      <div className="w-full max-w-sm">
        <div className="mb-8 flex flex-col items-center gap-3">
          <div className="grid size-11 place-items-center rounded-xl bg-gradient-to-br from-violet-500 to-fuchsia-500 text-lg font-bold text-white">P</div>
          <h1 className="text-xl font-semibold">Sign in to HappyPumi</h1>
          <p className="text-center text-sm text-ink-dim">Continue with your organization's identity provider.</p>
        </div>

        <div className="space-y-3 rounded-xl border border-line bg-panel p-5">
          {error && <p className="rounded-md border border-red-500/40 bg-red-500/10 px-3 py-2 text-sm text-red-400">{error}</p>}
          <button onClick={signIn} disabled={busy}
            className="flex w-full items-center justify-center gap-2 rounded-md bg-brand px-3 py-2.5 text-sm font-medium text-white transition-colors hover:bg-brand-hover disabled:opacity-60">
            {busy ? <Loader2 size={16} className="animate-spin" /> : <LogIn size={16} />} Sign in with Dex (OIDC)
          </button>
        </div>

        <p className="mt-4 text-center text-xs text-ink-faint">
          Demo users: <span className="text-ink-dim">admin@happypumi.dev</span> / <span className="text-ink-dim">member@happypumi.dev</span> · password <span className="text-ink-dim">password</span>
        </p>
      </div>
    </div>
  );
}
