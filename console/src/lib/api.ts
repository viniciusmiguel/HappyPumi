// Thin client over HappyPumi's REST API (proxied at /api in dev). The console authenticates with the
// Pulumi `token` scheme; any token resolves to the seeded admin in HappyPumi (ADR-0007). Fetchers return
// safe empties on failure so pages can render their empty states rather than crash.

const TOKEN = () => localStorage.getItem("happypumi.token") || "dev";

async function get<T>(path: string, fallback: T): Promise<T> {
  try {
    const res = await fetch(`/api${path}`, {
      headers: { Authorization: `token ${TOKEN()}`, Accept: "application/json" },
    });
    if (!res.ok) return fallback;
    return (await res.json()) as T;
  } catch {
    return fallback;
  }
}

export interface Organization { githubLogin: string; name?: string; }
export interface CurrentUser { githubLogin: string; name?: string; organizations?: Organization[]; }

export interface StackSummary {
  orgName: string; projectName: string; stackName: string;
  lastUpdate?: number; resourceCount?: number;
}
export interface Member { name?: string; githubLogin?: string; role?: string; user?: { githubLogin?: string; name?: string }; }
export interface Role { id: string; name: string; description?: string; }
export interface RegistryPackage {
  name: string; publisher?: string; source?: string; version?: string; title?: string;
}
export interface RegistryTemplate { name: string; publisher?: string; source?: string; language?: string; }
export interface PolicyPack { name: string; displayName?: string; versions?: unknown[]; }

export const api = {
  currentUser: () => get<CurrentUser>("/user", { githubLogin: "happypumi" }),
  defaultOrg: () => get<{ organization?: string }>("/user/organizations/default", {}),
  userStacks: () => get<{ stacks?: StackSummary[] }>("/user/stacks", { stacks: [] }),
  members: (org: string) => get<{ members?: Member[] }>(`/orgs/${org}/members`, { members: [] }),
  roles: (org: string) => get<{ roles?: Role[] }>(`/orgs/${org}/roles`, { roles: [] }),
  packages: () => get<{ packages?: RegistryPackage[] }>("/registry/packages", { packages: [] }),
  templates: () => get<{ templates?: RegistryTemplate[] }>("/registry/templates", { templates: [] }),
  policyPacks: (org: string) => get<{ policyPacks?: PolicyPack[] }>(`/orgs/${org}/policypacks`, { policyPacks: [] }),
  policyGroups: (org: string) => get<{ policyGroups?: { name: string }[] }>(`/orgs/${org}/policygroups`, { policyGroups: [] }),
};
