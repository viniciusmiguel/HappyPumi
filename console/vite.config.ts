import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

// The console talks to HappyPumi's REST API. In dev we proxy /api to the local HappyPumi server so
// the browser hits the same origin (no CORS); override with HAPPYPUMI_URL.
const happyPumi = process.env.HAPPYPUMI_URL ?? "http://localhost:5118";
// The OIDC token exchange (PKCE) is a same-origin fetch routed to the Dex test server via this proxy to
// avoid CORS; the authorize step is a top-level browser navigation straight to Dex.
const dex = process.env.DEX_URL ?? "http://localhost:5556";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      "/api": { target: happyPumi, changeOrigin: true },
      "/dex": { target: dex, changeOrigin: true },
    },
  },
});
