// Thin client over HappyPumi's REST API (proxied at /api in dev). The console authenticates with the
// Pulumi `token` scheme; any token resolves to the seeded admin in HappyPumi (ADR-0007). Fetchers return
// safe empties on failure so pages can render their empty states rather than crash.
//
// The endpoints mirror what the real Pulumi console calls (served by HappyPumi's dev MockConsole layer),
// so this React console renders the same data the real console did during reverse-engineering.

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

async function getText(path: string, fallback = ""): Promise<string> {
  try {
    const res = await fetch(`/api${path}`, { headers: { Authorization: `token ${TOKEN()}` } });
    if (!res.ok) return fallback;
    return await res.text();
  } catch {
    return fallback;
  }
}

async function post<T>(path: string, body: string, fallback: T): Promise<T> {
  try {
    const res = await fetch(`/api${path}`, {
      method: "POST",
      headers: { Authorization: `token ${TOKEN()}`, "Content-Type": "application/x-yaml" },
      body,
    });
    if (!res.ok) return fallback;
    return (await res.json()) as T;
  } catch {
    return fallback;
  }
}

// ── Shared shapes ────────────────────────────────────────────────────────────
export interface Organization { githubLogin: string; name?: string; avatarUrl?: string; }
export interface CurrentUser { githubLogin: string; name?: string; avatarUrl?: string; organizations?: Organization[]; }
export interface Actor { githubLogin?: string; name?: string; avatarUrl?: string; email?: string; }

export interface LastUpdate {
  version?: number; result?: string; kind?: string;
  startTime?: number; endTime?: number; time?: number; requestedBy?: Actor;
}
export interface Stack {
  orgName: string; projectName: string; stackName: string; name?: string;
  resourceCount?: number; version?: number; lastUpdate?: LastUpdate;
  tags?: Record<string, string>;
}
export interface ProjectResponse { project: { name: string; orgName: string; stacks: Stack[] }; continuationToken?: string | null; }

export interface UpdateInfo {
  version: number; updateID?: string; result?: string; kind?: string;
  message?: string; startTime?: number; endTime?: number; time?: number;
  requestedBy?: Actor; resourceChanges?: Record<string, number>; resourceCount?: number;
  info?: { environment?: Record<string, string>; resourceChanges?: Record<string, number> };
}

export interface Resource {
  urn: string; type: string; custom?: boolean; id?: string | null; parent?: string;
  provider?: string | null; created?: string; modified?: string;
  inputs?: Record<string, unknown>; outputs?: Record<string, unknown>;
}
export interface ResourcesResponse { region?: string; version?: number; resources: { resource: Resource }[]; }

export interface DeploymentJob { status: string; started?: string; lastUpdated?: string; steps?: DeploymentStep[]; }
export interface DeploymentStep { name: string; status: string; started?: string; lastUpdated?: string; isComplete?: boolean; }
export interface Deployment {
  id: string; version: number; status: string; pulumiOperation: string;
  created?: string; modified?: string; projectName?: string; stackName?: string; orgName?: string;
  requestedBy?: Actor; jobs?: DeploymentJob[];
}
export interface DeploymentLogLine { header?: string; line: string; timestamp?: string; }

export interface RegistryPackage {
  name: string; publisher?: string; source?: string; version?: string; description?: string;
  createdAt?: string; isLatest?: boolean; readmeURL?: string;
}
export interface PackageNavItem { name: Record<string, string>; typeToken: string; }
export interface PackageNavModule { name: Record<string, string>; resources?: PackageNavItem[]; functions?: PackageNavItem[]; }
export interface PackageNav { name: string; publisher: string; version: string; modules: PackageNavModule[]; }

export interface RegistryTemplate { name: string; publisher?: string; source?: string; language?: string; description?: string; }
export interface PolicyPack { name: string; displayName?: string; versions?: unknown[]; }

export interface OrgEnvironment {
  id: string; organization: string; project?: string; name: string;
  created?: string; modified?: string; ownedBy?: Actor; settings?: { deletionProtected?: boolean };
}
export interface EnvRevision { number: number; created?: string; creatorLogin?: string; creatorName?: string; tags?: string[]; }
export interface EscValue { value: unknown; secret?: boolean; }
export interface CheckEnvResponse { properties?: Record<string, EscValue>; schema?: unknown; diagnostics?: unknown[]; }

export interface Member { role?: string; user?: Actor; name?: string; githubLogin?: string; }
export interface Role { id: string; name: string; description?: string; }
export interface Team { name: string; displayName?: string; description?: string; userCount?: number; }

// ── API ──────────────────────────────────────────────────────────────────────
export const api = {
  currentUser: () => get<CurrentUser>("/user", { githubLogin: "happypumi" }),
  permissions: (org: string) => get<string[]>(`/console/orgs/${org}/permissions`, []),

  // Stacks / projects
  userStacks: () => get<{ stacks?: Stack[] }>("/user/stacks", { stacks: [] }),
  project: (org: string, project: string) =>
    get<ProjectResponse>(`/console/orgs/${org}/projects/${project}`, { project: { name: project, orgName: org, stacks: [] } }),
  stackMetadata: (org: string, project: string, stack: string) =>
    get<Stack>(`/stacks/${org}/${project}/${stack}/metadata`, { orgName: org, projectName: project, stackName: stack }),
  stackUpdates: (org: string, project: string, stack: string) =>
    get<{ updates?: UpdateInfo[] }>(`/stacks/${org}/${project}/${stack}/updates`, { updates: [] }),
  stackResources: (org: string, project: string, stack: string) =>
    get<ResourcesResponse>(`/stacks/${org}/${project}/${stack}/resources/latest`, { resources: [] }),
  stackResourceCount: (org: string, project: string, stack: string) =>
    get<{ resourceCount?: number; version?: number }>(`/stacks/${org}/${project}/${stack}/resources/count`, {}),

  // Deployments
  orgDeployments: (org: string) =>
    get<{ deployments?: Deployment[] }>(`/orgs/${org}/deployments`, { deployments: [] }),
  deployment: (org: string, project: string, stack: string, version: string) =>
    get<Deployment>(`/stacks/${org}/${project}/${stack}/deployments/version/${version}`, { id: "", version: 0, status: "", pulumiOperation: "" }),
  deploymentLogs: (org: string, project: string, stack: string, id: string) =>
    get<{ lines?: DeploymentLogLine[] }>(`/stacks/${org}/${project}/${stack}/deployments/${id}/logs`, { lines: [] }),

  // Registry / components / templates
  packages: () => get<{ packages?: RegistryPackage[] }>("/registry/packages", { packages: [] }),
  packageVersions: (source: string, publisher: string, name: string) =>
    get<{ packages?: RegistryPackage[] }>(`/registry/packages/${source}/${publisher}/${name}/versions`, { packages: [] }),
  packageVersion: (source: string, publisher: string, name: string, version = "latest") =>
    get<RegistryPackage>(`/registry/packages/${source}/${publisher}/${name}/versions/${version}`, { name }),
  packageReadme: (source: string, publisher: string, name: string, version: string) =>
    getText(`/registry/packages/${source}/${publisher}/${name}/versions/${version}/readme`),
  packageNav: (source: string, publisher: string, name: string, version = "latest") =>
    get<PackageNav>(`/console/registry/packages/${source}/${publisher}/${name}/versions/${version}/nav`, { name, publisher, version, modules: [] }),
  templates: () => get<{ templates?: RegistryTemplate[] }>("/registry/templates", { templates: [] }),

  // Environments (ESC)
  environments: (org: string) =>
    get<{ environments?: OrgEnvironment[] }>(`/esc/environments/${org}`, { environments: [] }),
  environmentYaml: (org: string, project: string, name: string) =>
    getText(`/esc/environments/${org}/${project}/${name}`),
  environmentRevisions: (org: string, project: string, name: string) =>
    get<EnvRevision[]>(`/esc/environments/${org}/${project}/${name}/versions`, []),
  checkEnvironment: (org: string, yaml: string) =>
    post<CheckEnvResponse>(`/esc/environments/${org}/yaml/check`, yaml, { properties: {} }),

  // Access management
  members: (org: string) => get<{ members?: Member[] }>(`/orgs/${org}/members`, { members: [] }),
  roles: (org: string) => get<{ roles?: Role[] }>(`/orgs/${org}/roles`, { roles: [] }),
  teams: (org: string) => get<{ teams?: Team[] }>(`/orgs/${org}/teams`, { teams: [] }),
  policyPacks: (org: string) => get<{ policyPacks?: PolicyPack[] }>(`/orgs/${org}/policypacks`, { policyPacks: [] }),
};

// Relative-time formatting matching the console's "Updated 2 days ago" style. Accepts unix seconds or ISO.
export function timeAgo(input?: number | string): string {
  if (input == null) return "—";
  const ms = typeof input === "number" ? input * 1000 : Date.parse(input);
  if (Number.isNaN(ms)) return "—";
  let value = Math.round((Date.now() - ms) / 1000);
  const steps: [number, string][] = [[60, "second"], [60, "minute"], [24, "hour"], [30, "day"], [12, "month"]];
  let name = "year";
  for (const [size, label] of steps) {
    if (Math.abs(value) < size) { name = label; break; }
    value = Math.round(value / size);
  }
  if (name === "second" && Math.abs(value) < 30) return "a few seconds ago";
  return `${value} ${name}${Math.abs(value) === 1 ? "" : "s"} ago`;
}
