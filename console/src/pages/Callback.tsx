import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Loader2 } from "lucide-react";
import { handleCallback } from "../lib/auth";

/** OIDC redirect target: exchanges the authorization code for an id-token, then enters the app. */
export default function Callback() {
  const navigate = useNavigate();
  const [error, setError] = useState("");
  const ran = useRef(false);

  useEffect(() => {
    if (ran.current) return; // guard StrictMode double-invoke (the code is single-use)
    ran.current = true;
    handleCallback()
      .then(() => navigate("/dashboard", { replace: true }))
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "Sign-in failed."));
  }, [navigate]);

  return (
    <div className="grid min-h-screen place-items-center bg-bg px-4 text-center">
      {error ? (
        <div className="max-w-sm">
          <h1 className="mb-2 text-lg font-semibold text-red-400">Sign-in failed</h1>
          <p className="mb-4 text-sm text-ink-dim">{error}</p>
          <Link to="/login" className="rounded-md bg-brand px-3 py-2 text-sm font-medium text-white hover:bg-brand-hover">Back to sign in</Link>
        </div>
      ) : (
        <div className="flex items-center gap-2 text-ink-dim">
          <Loader2 size={18} className="animate-spin" /> Completing sign-in…
        </div>
      )}
    </div>
  );
}
