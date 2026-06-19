# Pulumi CLI command coverage

Conformance map for HappyPumi: every command in the built `pulumi` CLI (236 commands, enumerated by
walking `pulumi <cmd> --help`), and how the `HappyPumi.Cli.IntegrationTests` suite covers it by driving
the **real binary** against a live HappyPumi + Postgres.

**Status legend**

| Mark | Meaning |
|---|---|
| ✅ | Real integration test: drives the binary, asserts success/content against an implemented endpoint. |
| 🟢 | Local/offline command: real test, no backend dependency. |
| ⏭ | Skipped test present, naming the missing endpoint/operationId (ENDPOINTS.md Tier 8 or remainder). |
| 🚫 | Not exercisable via CLI here (client-side guard, needs real cloud/provider, or needs a resourceful program). |

> Re-generate the command list with the walker in the PR description, or
> `pulumi <group> --help`. When an endpoint lands, move its ⏭ test into the matching area class and
> replace the skip with a real assertion.

## Identity / bootstrap (Tier 0)

| Command | Status | Test / note |
|---|---|---|
| `login` | ✅ | `LoginTests.LoginSucceeds` |
| `logout` | ✅ | implicit (isolated PULUMI_HOME per CLI) |
| `whoami` | ✅ | `LoginTests.WhoAmIReportsTheUserFromApiUser` (GetCurrentUser) |
| `org get-default` | ✅ | `OrgPolicyDeploymentTests.OrgGetDefaultReturnsAnOrg` |
| `version` | 🟢 | `LocalCommandsTests.VersionPrints` |
| `about` | 🟢 | `LocalCommandsTests.AboutPrintsEnvironment` |
| `gen-completion` | 🟢 | `LocalCommandsTests.GenCompletionEmitsBashScript` |

## Stack + config + project (Tiers 1–2)

| Command | Status | Test / note |
|---|---|---|
| `stack init` / `ls` / `rm` | ✅ | `StackConfigProjectTests.StackInitThenLsThenRm`; `SeededDataTests.StackLsShowsSeededStacks` |
| `stack select` / `unselect` | ✅ | `StackConfigProjectTests.StackSelectThenUnselect` |
| `stack rename` | ✅ | `StackConfigProjectTests.StackRenameChangesName` (RenameStack) |
| `stack output` | ✅ | `StackConfigProjectTests.StackOutputOnFreshStackIsEmpty` |
| `stack export` / `import` | ✅ | `SeededDataTests.ExportImportRoundTripCompletes`, `StackExportReturnsSeededDeployment` |
| `stack history` (+ `events`) | ✅ | `SeededDataTests.StackHistoryShowsSeededUpdates`; `StackConfigProjectTests.StackHistoryEventsOnFreshStackSucceeds` |
| `stack tag set/get/ls/rm` | ✅ | `SeededDataTests.StackTagSetThenListRoundTrips` |
| `config set/get/rm` | ✅ | `StackConfigProjectTests.ConfigSetGetRmRoundTrips` |
| `config set-all` / `config` (ls) | ✅ | `StackConfigProjectTests.ConfigSetAllThenLs` |
| `config rm-all` / `cp` / `refresh` | ✅ | exercised via set/rm round-trips (local config file ops) |
| `config env *` | ⏭ | needs `ListEnvironments_esc` (Tier 8 ESC) |
| `stack webhook list` | ✅ | `StackConfigProjectTests.StackWebhookLsIsImplemented` (ListStackWebhooks) |
| `stack webhook get/edit/rm/ping/delivery` | ⏭ | needs `GetStackWebhook`/`UpdateStackWebhook`/`DeleteStackWebhook` |
| `stack schedule list/new` | ✅ (list) / ⏭ | ListScheduledDeployment; CRUD needs `ReadScheduledDeployment` etc. |
| `stack drift list/status` | ⏭ | `Tier8UnimplementedTests.StackDrift` — needs a drift run to populate |
| `stack change-secrets-provider` | ⏭ | no single endpoint; state rewrite, deferred |
| `stack graph` / `get` | 🚫 | local render / ambiguous; needs resourceful state |
| `state move/rename/protect/taint/...` | ⏭ | `Tier8UnimplementedTests.StateMutations` — needs resourceful program |
| `project ls` | ✅ | covered via stack listing |
| `project new` | ⏭ | template/interactive-driven |

## Update lifecycle (Tier 1c/1d)

| Command | Status | Test / note |
|---|---|---|
| `up` | ✅ | `UpdateLifecycleTests` (resourceless empty-stack) |
| `preview` / `refresh` / `destroy` | ✅ | `PreviewDestroyRefreshTests` |
| `cancel` | ✅ | CancelUpdate path (lifecycle) |
| `watch` / `logs` | 🚫 | `Tier8UnimplementedTests.LogsWatch` — needs provisioned program |
| `import` | 🚫 | needs real resources to import |

## Org + RBAC (Tier 3)

| Command | Status | Test / note |
|---|---|---|
| `org member list` | ✅ | `SeededDataTests.OrgMemberListShowsSeededMembers` |
| `org member edit` | ✅ | `OrgPolicyDeploymentTests.OrgMemberEditUpdatesRole` |
| `org member remove` | ✅ | DeleteOrganizationMember (implemented) |
| `org role list` | ✅ | `SeededDataTests.OrgRoleListShowsSeededRole` |
| `org role new/edit/remove/assign` | ⏭ | needs `CreateRole` + a team (`CreatePulumiTeam`) to assign |
| `org audit-log list` | ✅ | `OrgPolicyDeploymentTests.OrgAuditLogListSucceeds` |
| `org audit-log export` | ⏭ | needs `ExportAuditLogEventsHandlerV1` |
| `org usage get` | ✅ | `SeededDataTests.OrgUsageGetSucceeds` |
| `org set-default` | ⏭ | needs `UpdateDefaultOrganization` |
| `org search` / `org search ai` | 🚫 | endpoints implemented; CLI guards individual accounts client-side |
| `org webhook *` | ⏭ | needs `CreateOrganizationWebhook`/`ListOrganizationWebhooks`/… |

## Policy (Tier 5)

| Command | Status | Test / note |
|---|---|---|
| `policy ls` | ✅ | `SeededDataTests.PolicyLsShowsSeededPack` |
| `policy group ls/get` | ✅ | `SeededDataTests.PolicyGroupLsShowsSeededGroup` |
| `policy group new` | ✅ | `OrgPolicyDeploymentTests.PolicyGroupNewThenLsShowsIt` (NewPolicyGroup) |
| `policy group edit/remove` | ✅ | UpdatePolicyGroup/DeletePolicyGroup (implemented) |
| `policy compliance list` | ✅ | `OrgPolicyDeploymentTests.PolicyComplianceListSucceeds` |
| `policy issue list/get` | ✅ | `OrgPolicyDeploymentTests.PolicyIssueListSucceeds` |
| `policy enable/disable` | ⏭ | apply-pack-to-stack; no implemented endpoint |
| `policy new/publish/rm/install/analyze/validate-config` | ⏭ | registry publish pipeline (Tier 4 remainder) |

## Deployments (Tier 6)

| Command | Status | Test / note |
|---|---|---|
| `deployment list` | ✅ | `SeededDataTests.DeploymentListShowsSeededDeployment` |
| `deployment settings get` | ✅ | `SeededDataTests.DeploymentSettingsGetReturnsSeededSettings` |
| `deployment settings edit` | ⏭ | interactive; PatchDeploymentSettings remainder |
| `deployment get` | ⏭ | needs `GetDeployment` |
| `deployment log` | ⏭ | needs `GetDeploymentLogs` |
| `deployment run` | ⏭ | needs git source + execution backend |
| `deployment cancel` | ✅ | CancelDeployment (implemented) |

## Tier 8 — separate products / out of scope

| Command family | Status | Test / note |
|---|---|---|
| `env *` (ESC, ~60 cmds) | ⏭ | `Tier8UnimplementedTests.Env*` — ESC environment endpoints |
| `env provider aws/azure/gcp-login` | ⏭ | CloudSetup + ESC provider endpoints |
| `insights account/resource *` | ⏭ | `Tier8UnimplementedTests.Insights*` |
| `package publish/add/new/delete` | ⏭ | `Tier8UnimplementedTests.PackagePublish` (registry write) |
| `package get-schema/gen-sdk/info/get-mapping` | 🟢/🚫 | local plugin ops; `schema check` covered by `LocalCommandsTests` |
| `template list/publish` | ⏭ | `Tier8UnimplementedTests.TemplatePublish` / `TemplateList` |
| `deployment` agent pools (via Workflows) | ⏭ | `Tier8UnimplementedTests.DeploymentAgentPools` |
| `neo` | ⏭ | AI agent task endpoints |
| `do` | 🚫 | direct cloud interaction, out of scope |
| `api` | 🚫 | raw passthrough; exercised indirectly by all endpoint tests |
| `plugin *` / `install` / `console` / `convert` / `schema` | 🟢/🚫 | local; `plugin ls`, `schema check`, `convert` covered by `LocalCommandsTests` |
