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

// PATCH a raw body (e.g. the ESC definition YAML; HappyPumi reads the body verbatim). Surfaces failures.
async function patchRaw(path: string, body: string): Promise<void> {
  const res = await fetch(`/api${path}`, {
    method: "PATCH",
    headers: { Authorization: authHeader(), "Content-Type": "application/json" },
    body,
  });
  if (!res.ok) throw new Error(`PATCH ${path} failed: ${res.status} ${res.statusText}`);
}

async function patchJson(path: string, body: unknown): Promise<void> {
  const res = await fetch(`/api${path}`, {
    method: "PATCH",
    headers: { Authorization: authHeader(), "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`PATCH ${path} failed: ${res.status} ${res.statusText}`);
}

async function del(path: string): Promise<void> {
  const res = await fetch(`/api${path}`, { method: "DELETE", headers: { Authorization: authHeader() } });
  if (!res.ok) throw new Error(`DELETE ${path} failed: ${res.status} ${res.statusText}`);
}

// JSON POST whose endpoint replies 204/empty (no body to parse). Surfaces failures like the write helpers.
async function postVoid(path: string, body: unknown): Promise<void> {
  const res = await fetch(`/api${path}`, {
    method: "POST",
    headers: { Authorization: authHeader(), "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`POST ${path} failed: ${res.status} ${res.statusText}`);
}

// ── Shared shapes ────────────────────────────────────────────────────────────
export interface Organization { githubLogin: string; name?: string; avatarUrl?: string; }
export interface CurrentUser { githubLogin: string; name?: string; avatarUrl?: string; organizations?: Organization[]; }
export interface Actor { githubLogin?: string; name?: string; avatarUrl?: string; email?: string; }

export interface LastUpdate {
  version?: number; result?: string; kind?: string;
  startTime?: number; endTime?: number; time?: number; requestedBy?: Actor;
}
export interface NotificationSettings { notifyUpdateSuccess?: boolean; notifyUpdateFailure?: boolean; }
export interface Stack {
  orgName: string; projectName: string; stackName: string; name?: string;
  resourceCount?: number; version?: number; lastUpdate?: LastUpdate;
  tags?: Record<string, string>; ownedBy?: Actor; notificationSettings?: NotificationSettings;
}
export interface ProjectResponse { project: { name: string; orgName: string; stacks: Stack[] }; continuationToken?: string | null; }

export interface UpdateInfo {
  version: number; updateID?: string; result?: string; kind?: string;
  message?: string; startTime?: number; endTime?: number; time?: number;
  requestedBy?: Actor; resourceChanges?: Record<string, number>; resourceCount?: number;
  info?: { environment?: Record<string, string>; resourceChanges?: Record<string, number> };
}

// Update + preview detail (PR2). The engine-event stream is a discriminated union; the console reads the
// few payloads it renders as a log/timeline (diagnostics, stdout, resource steps, the final summary).
export interface EngineEvent {
  type?: string; timestamp?: number;
  diagnosticEvent?: { message?: string; prefix?: string };
  stdoutEvent?: { message?: string };
  resourcePreEvent?: { metadata?: { op?: string; urn?: string; type?: string } };
  resOutputsEvent?: { metadata?: { op?: string; urn?: string; type?: string } };
  summaryEvent?: { resourceChanges?: Record<string, number> };
}
export interface UpdateSummaryDetail { result?: string; resourceCount?: number; startTime?: number; endTime?: number; }
export interface UpdateTimeline { update?: UpdateInfo; previews?: UpdateInfo[]; collatedUpdateEvents?: UpdateInfo[]; }
export interface UpdateEventsResponse { events?: EngineEvent[]; continuationToken?: string; }

export interface Resource {
  urn: string; type: string; custom?: boolean; id?: string | null; parent?: string;
  provider?: string | null; created?: string; modified?: string;
  inputs?: Record<string, unknown>; outputs?: Record<string, unknown>;
}
export interface ResourcesResponse { region?: string; version?: number; resources: { resource: Resource }[]; }
export interface StackRef { name: string; organization: string; routingProject: string; version: number; }
export interface StackOverview { referencedStacks?: StackRef[]; resources?: ResourcesResponse; tags?: Record<string, string>; }

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
export interface VcsIntegrationSummary {
  id: string;
  name?: string;
  vcsProvider: string;
  baseUrl?: string;
  host?: string;
  avatarUrl?: string;
  hasIndividualAccess: boolean;
}
export interface GitHubSetupResponse { installationUrl?: string; }
export interface InitiateOAuthResponse { sessionID: string; url: string; }
export interface AzureDevOpsAccessResponse {
  hasIntegration: boolean;
  hasUserToken: boolean;
  availableOrgs?: AzureDevOpsOrganization[];
}
export interface AzureDevOpsOrganization { id?: string; name: string; accountUrl?: string; hasRequiredPermissions?: boolean; }
export interface VcsRepo { id: string; name: string; owner: string; }
export interface VcsBranch { name: string; isProtected: boolean; }
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
export interface EnvTag { name: string; value: string; editorLogin?: string; modified?: string; }
export interface EnvWebhook { name: string; displayName?: string; payloadUrl: string; active?: boolean; format?: string; filters?: string[]; }
export interface StackWebhook { name: string; displayName?: string; payloadUrl: string; active?: boolean; format?: string; filters?: string[]; hasSecret?: boolean; }
export interface StackWebhookInput { name?: string; displayName?: string; payloadUrl?: string; active?: boolean; format?: string; secret?: string; }
export interface WebhookDeliveryLog { id: string; kind: string; payload?: string; requestUrl?: string; responseCode?: number; responseBody?: string; duration?: number; timestamp?: number; }
export interface EnvSchedule { id: string; kind: string; scheduleCron?: string; nextExecution?: string; lastExecuted?: string; paused?: boolean; }
export interface RotationEvent { id: string; created?: string; completed?: string; errorMessage?: string; preRotationRevision?: number; postRotationRevision?: number; }
export interface EnvReferrer { environment?: { project?: string; name?: string }; stack?: { projectName?: string; stackName?: string }; insightsAccount?: { name?: string }; }
export interface EnvSettings { deletionProtected?: boolean; }
export interface EscSchema { type?: string; description?: string; secret?: boolean; required?: string[]; properties?: Record<string, EscSchema>; additionalProperties?: EscSchema; }
export interface ProviderSchema { name: string; description?: string; inputs?: EscSchema; outputs?: EscSchema; }

export interface Member { role?: string; user?: Actor; name?: string; githubLogin?: string; }
export interface Role { id: string; name: string; description?: string; }
export interface TeamMember { githubLogin?: string; name?: string; role?: string; }
export interface Team {
  name: string; displayName?: string; description?: string; kind?: string;
  members?: TeamMember[]; roleIds?: string[];
}
// Stack access (collaborators + team grants). Permission levels: 0 none, 101 read, 102 write, 103 admin.
export interface UserPermission { permission: number; user: Actor; }
export interface StackCollaborators { stackCreatorUserName?: string; users: UserPermission[]; }
export interface StackTeamGrant { name: string; displayName?: string; description?: string; isMember?: boolean; permission: number; }
export interface StackTeams { projectName: string; teams: StackTeamGrant[]; }

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
  // Paginated activity feed (updates projected newest-first).
  stackActivity: (org: string, project: string, stack: string, page = 1, pageSize = 25) =>
    get<{ activity: { update?: UpdateInfo }[]; total: number; itemsPerPage: number }>(
      `/stacks/${org}/${project}/${stack}/activity?page=${page}&pageSize=${pageSize}`,
      { activity: [], total: 0, itemsPerPage: pageSize }),
  stackResources: (org: string, project: string, stack: string) =>
    get<ResourcesResponse>(`/stacks/${org}/${project}/${stack}/resources/latest`, { resources: [] }),
  stackResourceCount: (org: string, project: string, stack: string) =>
    get<{ resourceCount?: number; version?: number }>(`/stacks/${org}/${project}/${stack}/resources/count`, {}),
  // Resources as of a specific update version (the per-version resource view).
  stackResourcesAtVersion: (org: string, project: string, stack: string, version: number) =>
    get<ResourcesResponse>(`/stacks/${org}/${project}/${stack}/resources/${version}`, { resources: [], version }),
  // Single resource by URN from the latest checkpoint — backs the resource detail dialog.
  stackResource: (org: string, project: string, stack: string, urn: string) =>
    get<{ resource: { resource: Resource }; version: number } | null>(
      `/stacks/${org}/${project}/${stack}/resources/latest/${encodeURIComponent(urn)}`, null),
  // Console overview aggregation (resources + tags + referenced stacks).
  stackOverview: (org: string, project: string, stack: string) =>
    get<StackOverview | null>(`/console/stacks/${org}/${project}/${stack}/overview`, null),
  // Stack references (PR5): upstream = stacks this one reads; downstream = stacks that read this one.
  stackUpstreamRefs: (org: string, project: string, stack: string) =>
    get<{ referencedStacks: StackRef[] }>(`/stacks/${org}/${project}/${stack}/upstreamreferences`, { referencedStacks: [] }),
  stackDownstreamRefs: (org: string, project: string, stack: string) =>
    get<{ referencedStacks: StackRef[] }>(`/stacks/${org}/${project}/${stack}/downstreamreferences`, { referencedStacks: [] }),
  // Update detail (PR2): per-version summary, timeline (focal update + previews), and the engine-event stream.
  stackUpdateSummary: (org: string, project: string, stack: string, version: number) =>
    get<UpdateSummaryDetail>(`/stacks/${org}/${project}/${stack}/updates/${version}/summary`, {}),
  stackUpdateTimeline: (org: string, project: string, stack: string, version: number) =>
    get<UpdateTimeline>(`/stacks/${org}/${project}/${stack}/updates/${version}/timeline`, {}),
  stackUpdateEvents: (org: string, project: string, stack: string, updateId: string) =>
    get<UpdateEventsResponse>(`/stacks/${org}/${project}/${stack}/update/${updateId}/events`, { events: [] }),
  // Preview history for the stack (dry-run updates).
  stackPreviews: (org: string, project: string, stack: string) =>
    get<{ updates?: UpdateInfo[] }>(`/stacks/${org}/${project}/${stack}/updates/latest/previews`, { updates: [] }),

  // Stack access (collaborators + team grants)
  stackCollaborators: (org: string, project: string, stack: string) =>
    get<StackCollaborators>(`/stacks/${org}/${project}/${stack}/collaborators`, { users: [] }),
  removeStackCollaborator: (org: string, project: string, stack: string, userName: string) =>
    del(`/stacks/${org}/${project}/${stack}/collaborators/${userName}`),
  stackTeams: (org: string, project: string, stack: string) =>
    get<StackTeams>(`/stacks/${org}/${project}/${stack}/teams`, { projectName: project, teams: [] }),
  // Team grant updates are console-namespaced; permission null removes the team's grant.
  updateStackTeamPermission: (org: string, project: string, stack: string, team: string, permission: number | null) =>
    patchJson(`/console/stacks/${org}/${project}/${stack}/teams/${team}`, { permissions: permission }),

  // Stack settings actions (PR6)
  // Updates the value of an existing tag (the tag must already exist on the stack).
  updateStackTag: (org: string, project: string, stack: string, name: string, value: string) =>
    patchJson(`/stacks/${org}/${project}/${stack}/tags/${name}`, { name, value }),
  // Toggles the per-stack notification preferences; returns void (the caller re-reads metadata).
  updateStackNotifications: (org: string, project: string, stack: string, settings: NotificationSettings) =>
    patchJson(`/stacks/${org}/${project}/${stack}/notifications/settings`, settings),
  // Reassigns ownership to the given login; returns the new owner's identity.
  reassignStackOwner: (org: string, project: string, stack: string, owner: string) =>
    postJson<Actor>(`/stacks/${org}/${project}/${stack}/ownership`, { githubLogin: owner, name: owner }),
  // Moves the stack to another organization (replies 204 on success).
  transferStack: (org: string, project: string, stack: string, toOrg: string) =>
    postVoid(`/stacks/${org}/${project}/${stack}/transfer`, { toOrg }),

  // Stack webhooks (PR1): CRUD + delivery history, ping, and redeliver. The secret is write-only
  // (only `hasSecret` is returned). Ping/redeliver perform real POSTs and return the delivery result.
  stackWebhooks: (org: string, project: string, stack: string) =>
    get<StackWebhook[]>(`/stacks/${org}/${project}/${stack}/hooks`, []),
  createStackWebhook: (org: string, project: string, stack: string, hook: StackWebhookInput) =>
    postJson<StackWebhook>(`/stacks/${org}/${project}/${stack}/hooks`, { active: true, format: "raw", ...hook }),
  updateStackWebhook: (org: string, project: string, stack: string, name: string, patch: StackWebhookInput) =>
    patchJson(`/stacks/${org}/${project}/${stack}/hooks/${name}`, patch),
  deleteStackWebhook: (org: string, project: string, stack: string, name: string) =>
    del(`/stacks/${org}/${project}/${stack}/hooks/${name}`),
  stackWebhookDeliveries: (org: string, project: string, stack: string, name: string) =>
    get<WebhookDeliveryLog[]>(`/stacks/${org}/${project}/${stack}/hooks/${name}/deliveries`, []),
  pingStackWebhook: (org: string, project: string, stack: string, name: string) =>
    postJson<WebhookDeliveryLog>(`/stacks/${org}/${project}/${stack}/hooks/${name}/ping`, {}),
  redeliverStackWebhookEvent: (org: string, project: string, stack: string, name: string, event: string) =>
    postJson<WebhookDeliveryLog>(`/stacks/${org}/${project}/${stack}/hooks/${name}/deliveries/${event}/redeliver`, {}),

  // Organization webhooks (PR2): same shape as stack webhooks, scoped to the org. The secret is write-only
  // (only `hasSecret` is returned). Org webhooks also fire on org-wide stack activity. Ping/redeliver POST for real.
  orgWebhooks: (org: string) =>
    get<StackWebhook[]>(`/orgs/${org}/hooks`, []),
  createOrgWebhook: (org: string, hook: StackWebhookInput) =>
    postJson<StackWebhook>(`/orgs/${org}/hooks`, { active: true, format: "raw", ...hook }),
  updateOrgWebhook: (org: string, name: string, patch: StackWebhookInput) =>
    patchJson(`/orgs/${org}/hooks/${name}`, patch),
  deleteOrgWebhook: (org: string, name: string) =>
    del(`/orgs/${org}/hooks/${name}`),
  orgWebhookDeliveries: (org: string, name: string) =>
    get<WebhookDeliveryLog[]>(`/orgs/${org}/hooks/${name}/deliveries`, []),
  pingOrgWebhook: (org: string, name: string) =>
    postJson<WebhookDeliveryLog>(`/orgs/${org}/hooks/${name}/ping`, {}),
  redeliverOrgWebhookEvent: (org: string, name: string, event: string) =>
    postJson<WebhookDeliveryLog>(`/orgs/${org}/hooks/${name}/deliveries/${event}/redeliver`, {}),

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
  updateEnvironment: (org: string, project: string, name: string, yaml: string) =>
    patchRaw(`/esc/environments/${org}/${project}/${name}`, yaml),
  // Environment tags (CRUD)
  environmentTags: (org: string, project: string, name: string) =>
    get<{ tags?: Record<string, EnvTag> }>(`/esc/environments/${org}/${project}/${name}/tags`, { tags: {} }),
  createEnvironmentTag: (org: string, project: string, name: string, tag: string, value: string) =>
    postJson<unknown>(`/esc/environments/${org}/${project}/${name}/tags`, { name: tag, value }),
  deleteEnvironmentTag: (org: string, project: string, name: string, tag: string) =>
    del(`/esc/environments/${org}/${project}/${name}/tags/${tag}`),
  // Webhooks
  environmentWebhooks: (org: string, project: string, name: string) =>
    get<EnvWebhook[]>(`/esc/environments/${org}/${project}/${name}/hooks`, []),
  createEnvironmentWebhook: (org: string, project: string, name: string, hook: { name: string; payloadUrl: string; displayName?: string; format?: string }) =>
    postJson<unknown>(`/esc/environments/${org}/${project}/${name}/hooks`, { active: true, format: "raw", ...hook }),
  deleteEnvironmentWebhook: (org: string, project: string, name: string, hook: string) =>
    del(`/esc/environments/${org}/${project}/${name}/hooks/${hook}`),
  // Scheduled actions (rotation / deletion)
  environmentSchedules: (org: string, project: string, name: string) =>
    get<{ schedules?: EnvSchedule[] }>(`/esc/environments/${org}/${project}/${name}/schedules`, { schedules: [] }),
  createEnvironmentSchedule: (org: string, project: string, name: string, body: { kind: string; scheduleCron?: string; scheduleOnce?: string }) =>
    postJson<unknown>(`/esc/environments/${org}/${project}/${name}/schedules`, body),
  deleteEnvironmentSchedule: (org: string, project: string, name: string, id: string) =>
    del(`/esc/environments/${org}/${project}/${name}/schedules/${id}`),
  // Secret rotation
  rotateEnvironment: (org: string, project: string, name: string) =>
    postJson<unknown>(`/esc/environments/${org}/${project}/${name}/rotate`, {}),
  rotationHistory: (org: string, project: string, name: string) =>
    get<{ events?: RotationEvent[] }>(`/esc/environments/${org}/${project}/${name}/rotate/history`, { events: [] }),
  // Open (reveal resolved values, including decrypted secrets). Gated environments throw 403 until granted.
  openEnvironment: (org: string, project: string, name: string) =>
    postJson<{ id: string }>(`/esc/environments/${org}/${project}/${name}/open`, {}),
  openSession: (org: string, project: string, name: string, id: string) =>
    get<{ properties?: Record<string, EscValue> }>(`/esc/environments/${org}/${project}/${name}/open/${id}`, { properties: {} }),
  // Open-request / approval workflow (separation of duties: requester ≠ approver)
  requestEnvironmentAccess: (org: string, project: string, name: string) =>
    postJson<{ changeRequests?: { changeRequestId?: string }[] }>(
      `/esc/environments/${org}/${project}/${name}/open/request`, { accessDurationSeconds: 3600, grantExpirationSeconds: 7200 }),
  approveChangeRequest: (org: string, id: string) =>
    postJson<{ granted?: boolean; approvals?: number; required?: number }>(`/change-requests/${org}/${id}/approve`, {}),
  unapproveChangeRequest: (org: string, id: string) =>
    del(`/change-requests/${org}/${id}/approve`),
  // Provider / rotator catalog (fn::open / fn::rotate) — names + per-integration JSON schema
  escProviders: () => get<{ providers?: string[] }>("/esc/providers", { providers: [] }),
  escRotators: () => get<{ rotators?: string[] }>("/esc/rotators", { rotators: [] }),
  escProviderSchema: (name: string) => get<ProviderSchema>(`/esc/providers/${name}/schema`, { name }),
  escRotatorSchema: (name: string) => get<ProviderSchema>(`/esc/rotators/${name}/schema`, { name }),
  // Referrers (imported by)
  environmentReferrers: (org: string, project: string, name: string) =>
    get<{ referrers?: Record<string, EnvReferrer[]> }>(`/esc/environments/${org}/${project}/${name}/referrers`, { referrers: {} }),
  // Settings
  environmentSettings: (org: string, project: string, name: string) =>
    get<EnvSettings>(`/esc/environments/${org}/${project}/${name}/settings`, {}),
  updateEnvironmentSettings: (org: string, project: string, name: string, deletionProtected: boolean) =>
    patchJson(`/esc/environments/${org}/${project}/${name}/settings`, { deletionProtected }),
  reassignEnvironmentOwner: (org: string, project: string, name: string, owner: string) =>
    postJson<unknown>(`/esc/environments/${org}/${project}/${name}/ownership`, { githubLogin: owner, name: owner }),

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
  vcsIntegrations: (org: string) =>
    get<{ integrations?: VcsIntegrationSummary[] }>(`/console/orgs/${org}/integrations`, { integrations: [] }),
  deleteVcsIntegration: (org: string, provider: string, id: string) =>
    del(`/console/orgs/${org}/integrations/${provider}/${id}`),
  // GitHub App install: returns the install URL the browser is sent to.
  startGitHubSetup: (org: string) =>
    postJson<GitHubSetupResponse>(`/console/orgs/${org}/integrations/github`, {}),
  // Azure DevOps connect flow: create the record, then run OAuth (initiate -> browser -> complete).
  createAzureDevOpsSetup: (org: string, organizationName: string, projectId: string) =>
    postVoid(`/console/orgs/${org}/integrations/azure-devops`, { organizationName, projectId }),
  initiateAzureDevOpsOAuth: (org: string) =>
    postJson<InitiateOAuthResponse>(`/console/orgs/${org}/integrations/azure-devops/oauth/initiate`,
      { provider: { name: "azure-devops" } }),
  completeAzureDevOpsOAuth: (org: string, code: string, sessionID: string) =>
    postVoid(`/console/orgs/${org}/integrations/azure-devops/oauth/complete`,
      { code, sessionID, provider: { name: "azure-devops" } }),
  azureDevOpsAccessStatus: (org: string) =>
    get<AzureDevOpsAccessResponse>(`/console/orgs/${org}/integrations/azure-devops/access-status`,
      { hasIntegration: false, hasUserToken: false, availableOrgs: [] }),
  // Per-integration repo browser (generic dispatch by provider kind).
  vcsRepos: (org: string, provider: string, id: string) =>
    get<{ repos?: VcsRepo[] }>(`/console/orgs/${org}/integrations/${provider}/${id}/repos`, { repos: [] }),
  vcsBranches: (org: string, provider: string, id: string, repoId: string) =>
    get<{ branches?: VcsBranch[] }>(
      `/console/orgs/${org}/integrations/${provider}/${id}/repos/${encodeURIComponent(repoId)}/branches`,
      { branches: [] }),
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
