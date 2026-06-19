// Thin client over HappyPumi's REST API (proxied at /api in dev). The console authenticates with the
// Pulumi `token` scheme; any token resolves to the seeded admin in HappyPumi (ADR-0007). Fetchers return
// safe empties on failure so pages can render their empty states rather than crash.
//
// The endpoints mirror what the real Pulumi console calls (served by HappyPumi's dev MockConsole layer),
// so this React console renders the same data the real console did during reverse-engineering.

import { getToken } from "./auth";

// OIDC id-tokens (JWTs) go out as Bearer (validated against Dex); legacy/dev tokens use the `token` scheme.
const authHeader = (): string => {
  const t = getToken() ?? "";
  return t.split(".").length === 3 ? `Bearer ${t}` : `token ${t}`;
};

async function get<T>(path: string, fallback: T): Promise<T> {
  try {
    const res = await fetch(`/api${path}`, {
      headers: { Authorization: authHeader(), Accept: "application/json" },
    });
    if (!res.ok) return fallback;
    return (await res.json()) as T;
  } catch {
    return fallback;
  }
}

async function getText(path: string, fallback = ""): Promise<string> {
  try {
    const res = await fetch(`/api${path}`, { headers: { Authorization: authHeader() } });
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
      headers: { Authorization: authHeader(), "Content-Type": "application/x-yaml" },
      body,
    });
    if (!res.ok) return fallback;
    return (await res.json()) as T;
  } catch {
    return fallback;
  }
}

// JSON POST that surfaces failure (unlike the read fetchers, write actions must report errors to the user).
async function postJson<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`/api${path}`, {
    method: "POST",
    headers: { Authorization: authHeader(), "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`POST ${path} failed: ${res.status} ${res.statusText}`);
  return (await res.json()) as T;
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

export interface PackageUsageStats { onLatest?: number; onOlder?: number; totalStacks?: number; }
export interface RegistryPackage {
  name: string; publisher?: string; source?: string; version?: string; description?: string; usageStats?: PackageUsageStats;
  createdAt?: string; isLatest?: boolean; readmeURL?: string;
}
export interface PackageNavItem { name: Record<string, string>; typeToken: string; }
export interface PackageNavModule { name: Record<string, string>; resources?: PackageNavItem[]; functions?: PackageNavItem[]; }
export interface PackageNav { name: string; publisher: string; version: string; modules: PackageNavModule[]; }

export interface RegistryTemplate { name: string; publisher?: string; source?: string; language?: string; description?: string; version?: string; }
export interface PolicyPack { name: string; displayName?: string; versions?: unknown[]; }
export interface Service { name: string; description?: string; organizationName?: string; created?: string; }
export interface AuditEvent { event: string; description: string; actorName?: string; sourceIP?: string; timestamp?: number; }
export interface CloudAccount { name: string; provider: string; description?: string; created?: string; }
export interface VcsConnection { name: string; kind: string; created?: string; }
export interface OidcIssuer { name: string; url: string; created?: string; }
export interface ApprovalRule { name: string; stackPattern: string; requiredApprovals: number; enabled?: boolean; created?: string; }
export interface PolicyViolation {
  id: string; policyName: string; policyPack: string; policyPackTag?: string; level: string;
  message: string; observedAt?: string; projectName?: string; stackName?: string;
  resourceName?: string; resourceType?: string; resourceURN?: string;
}

export interface OrgEnvironment {
  id: string; organization: string; project?: string; name: string;
  created?: string; modified?: string; ownedBy?: Actor; settings?: { deletionProtected?: boolean };
}
export interface EnvRevision { number: number; created?: string; creatorLogin?: string; creatorName?: string; tags?: string[]; }
export interface EscValue { value: unknown; secret?: boolean; }
export interface CheckEnvResponse { properties?: Record<string, EscValue>; schema?: unknown; diagnostics?: unknown[]; }

export interface Member { role?: string; user?: Actor; name?: string; githubLogin?: string; }
export interface Role { id: string; name: string; description?: string; }
export interface TeamMember { githubLogin?: string; name?: string; role?: string; }
export interface Team {
  name: string; displayName?: string; description?: string; kind?: string;
  members?: TeamMember[]; roleIds?: string[];
}

// ── API ──────────────────────────────────────────────────────────────────────
export const api = {
  currentUser: () => get<CurrentUser>("/user", { githubLogin: "happypumi" }),
  permissions: (org: string) => get<string[]>(`/console/orgs/${org}/permissions`, []),

  // Stacks / projects
  userStacks: () => get<{ stacks?: Stack[] }>("/user/stacks", { stacks: [] }),
  // Creates an (empty) stack; the project is created implicitly on first stack (CreateStack).
  createStack: (org: string, project: string, stackName: string) =>
    postJson<unknown>(`/stacks/${org}/${project}`, { stackName }),
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
  // Initiates a managed deployment. templateRef ("source/publisher/name/version") makes the runner fetch and
  // deploy a published template; omit it for an operation against the stack's existing program.
  createDeployment: (org: string, project: string, stack: string, operation: string, templateRef?: string) =>
    postJson<{ id: string; version: number; consoleUrl?: string }>(
      `/stacks/${org}/${project}/${stack}/deployments`, { operation, templateRef }),

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
  createEnvironment: (org: string, project: string, name: string) =>
    postJson<unknown>(`/esc/environments/${org}`, { project, name }),
  environmentYaml: (org: string, project: string, name: string) =>
    getText(`/esc/environments/${org}/${project}/${name}`),
  environmentRevisions: (org: string, project: string, name: string) =>
    get<EnvRevision[]>(`/esc/environments/${org}/${project}/${name}/versions`, []),
  checkEnvironment: (org: string, yaml: string) =>
    post<CheckEnvResponse>(`/esc/environments/${org}/yaml/check`, yaml, { properties: {} }),

  // Access management
  members: (org: string) => get<{ members?: Member[] }>(`/orgs/${org}/members`, { members: [] }),
  // Adds an existing user to the org with a built-in role (AddOrganizationMember).
  addMember: (org: string, userLogin: string, role: string) =>
    postJson<Member>(`/orgs/${org}/members/${userLogin}`, { role }),
  roles: (org: string) => get<{ roles?: Role[] }>(`/orgs/${org}/roles`, { roles: [] }),
  createRole: (org: string, name: string, description: string) =>
    postJson<Role>(`/orgs/${org}/roles`, { name, description, uxPurpose: "organization" }),
  teams: (org: string) => get<{ teams?: Team[] }>(`/orgs/${org}/teams`, { teams: [] }),
  createTeam: (org: string, name: string, displayName: string, description: string) =>
    postJson<Team>(`/orgs/${org}/teams/pulumi`, { name, displayName, description }),
  policyPacks: (org: string) => get<{ policyPacks?: PolicyPack[] }>(`/orgs/${org}/policypacks`, { policyPacks: [] }),

  // Platform / management surfaces
  services: (org: string) => get<{ services?: Service[] }>(`/orgs/${org}/services`, { services: [] }),
  createService: (org: string, name: string, description: string) =>
    postJson<Service>(`/orgs/${org}/services`, { name, description }),
  auditLogs: (org: string) => get<{ auditLogEvents?: AuditEvent[] }>(`/orgs/${org}/auditlogs`, { auditLogEvents: [] }),
  cloudAccounts: (org: string) => get<{ accounts?: CloudAccount[] }>(`/orgs/${org}/cloud-accounts`, { accounts: [] }),
  createCloudAccount: (org: string, name: string, provider: string, description: string) =>
    postJson<unknown>(`/orgs/${org}/cloud-accounts`, { name, provider, description }),
  vcsConnections: (org: string) => get<{ connections?: VcsConnection[] }>(`/orgs/${org}/vcs-connections`, { connections: [] }),
  createVcsConnection: (org: string, name: string, kind: string) =>
    postJson<unknown>(`/orgs/${org}/vcs-connections`, { name, kind }),
  oidcIssuers: (org: string) => get<{ issuers?: OidcIssuer[] }>(`/orgs/${org}/oidc-issuers`, { issuers: [] }),
  createOidcIssuer: (org: string, name: string, url: string) =>
    postJson<unknown>(`/orgs/${org}/oidc-issuers`, { name, url }),
  approvalRules: (org: string) => get<{ rules?: ApprovalRule[] }>(`/orgs/${org}/approval-rules`, { rules: [] }),
  createApprovalRule: (org: string, name: string, stackPattern: string, requiredApprovals: number) =>
    postJson<unknown>(`/orgs/${org}/approval-rules`, { name, stackPattern, requiredApprovals }),
  policyViolations: (org: string) =>
    get<{ policyViolations?: PolicyViolation[] }>(`/orgs/${org}/policyresults/violationsv2`, { policyViolations: [] }),
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
