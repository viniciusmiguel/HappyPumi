# HappyPumi Console

The IDP web UI for HappyPumi — a React + TypeScript + Tailwind reimplementation of the Pulumi Cloud
console, talking to HappyPumi's REST API.

## Develop

```bash
npm install
npm run dev          # http://localhost:5173, proxies /api -> HappyPumi (HAPPYPUMI_URL, default :5118)
```

Run HappyPumi (seeded) alongside it, e.g. the docker-compose demo or `dotnet run --project ../HappyPumi.Api`
with `Seed__Enabled=true`. The console authenticates with the Pulumi `token` scheme (stored in
`localStorage["happypumi.token"]`, default `dev`).

## Build

```bash
npm run build        # tsc + vite build -> dist/
```

## Layout

- `src/lib/nav.ts` — sidebar information architecture (home items + Platform / Management / Access drill-ins).
- `src/lib/api.ts` — typed client over HappyPumi (`/api/...`).
- `src/components/` — `Layout`, `Sidebar`, shared `ui` (PageHeader, Table, EmptyState, buttons).
- `src/pages/` — one component per route; data-backed pages (Dashboard, Stacks, Registry, Templates,
  Members, Roles, Policies) and empty-state pages (`empties.tsx`) for surfaces not yet API-backed.
