import { useCallback, useEffect, useState } from "react";
import { Settings } from "lucide-react";
import { api, type OrganizationMetadata, type RbacUserInfo, type Role, type UpdateOrgSettingsBody } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Badge, Card, Field, PageHeader, PrimaryButton, SecondaryButton, Table } from "../components/ui";

const VCS_OPTIONS = ["github", "gitlab", "azuredevops", "bitbucket"];
const AI_OPTIONS = ["disabled", "opt-in", "enabled"];

// The member-can-* toggles, mapping the request set* field to the returned metadata field.
const TOGGLES: { set: keyof UpdateOrgSettingsBody; field: keyof OrganizationMetadata; label: string }[] = [
  { set: "setMembersCanCreateStacks", field: "membersCanCreateStacks", label: "Members can create stacks" },
  { set: "setMembersCanDeleteStacks", field: "membersCanDeleteStacks", label: "Members can delete stacks" },
  { set: "setMembersCanCreateTeams", field: "membersCanCreateTeams", label: "Members can create teams" },
  { set: "setMembersCanTransferStacks", field: "membersCanTransferStacks", label: "Members can transfer stacks" },
  { set: "setMembersCanCreateAccounts", field: "membersCanCreateAccounts", label: "Members can create accounts" },
];

// Org Settings (org-admin PR1): edit the org-wide member-can-* / VCS / AI toggles and manage members by role
// (set sole admin, set default role). The current settings are read via a no-op PATCH that returns the
// persisted OrganizationMetadata (GetOrganization only returns the public org shape).
export default function OrgSettings() {
  const org = useOrg();
  const [meta, setMeta] = useState<OrganizationMetadata | null>(null);
  const [roles, setRoles] = useState<Role[]>([]);

  const load = useCallback(() => {
    api.updateOrgSettings(org, {}).then(setMeta).catch(() => setMeta(null));
    api.roles(org).then((r) => setRoles(r.roles ?? []));
  }, [org]);
  useEffect(() => { load(); }, [load]);

  return (
    <div>
      <PageHeader icon={Settings} title="Organization settings" />
      <div className="mt-4 grid gap-4">
        <SettingsCard org={org} meta={meta} onSaved={setMeta} />
        <MembersByRoleCard org={org} roles={roles} defaultRoleId={meta?.defaultRoleId} onChanged={load} />
      </div>
    </div>
  );
}

function SettingsCard({ org, meta, onSaved }: {
  org: string; meta: OrganizationMetadata | null; onSaved: (m: OrganizationMetadata) => void;
}) {
  const [draft, setDraft] = useState<UpdateOrgSettingsBody>({});
  const [saving, setSaving] = useState(false);

  const value = <K extends keyof OrganizationMetadata>(field: K): OrganizationMetadata[K] | undefined =>
    meta?.[field];

  const save = async () => {
    setSaving(true);
    try {
      onSaved(await api.updateOrgSettings(org, draft));
      setDraft({});
    } finally {
      setSaving(false);
    }
  };

  if (!meta) return <Card title="Settings"><p className="px-6 py-4 text-sm text-ink-dim">Loading…</p></Card>;

  return (
    <Card title="Settings" actions={<PrimaryButton onClick={saving ? undefined : save}>Save changes</PrimaryButton>}>
      <div className="grid gap-3 px-6 py-4">
        {TOGGLES.map((t) => (
          <label key={t.set} className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={draft[t.set] as boolean ?? Boolean(value(t.field))}
              onChange={(e) => setDraft((d) => ({ ...d, [t.set]: e.target.checked }))}
            />
            {t.label}
          </label>
        ))}
        <Field
          label="Preferred VCS"
          value={draft.setPreferredVCS ?? meta.preferredVCS}
          onChange={(v) => setDraft((d) => ({ ...d, setPreferredVCS: v }))}
          options={VCS_OPTIONS}
        />
        <Field
          label="AI enablement"
          value={draft.setAiEnablement ?? meta.aiEnablement}
          onChange={(v) => setDraft((d) => ({ ...d, setAiEnablement: v }))}
          options={AI_OPTIONS}
        />
        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={draft.setNeoEnabled ?? meta.neoEnabled}
            onChange={(e) => setDraft((d) => ({ ...d, setNeoEnabled: e.target.checked }))}
          />
          Neo AI agent enabled
        </label>
        <p className="text-xs text-ink-dim">Members: {meta.memberCount}</p>
      </div>
    </Card>
  );
}

function MembersByRoleCard({ org, roles, defaultRoleId, onChanged }: {
  org: string; roles: Role[]; defaultRoleId?: string; onChanged: () => void;
}) {
  const [roleId, setRoleId] = useState("");
  const [users, setUsers] = useState<RbacUserInfo[]>([]);

  useEffect(() => {
    if (!roleId) return;
    api.usersWithRole(org, roleId).then((r) => setUsers(r.users ?? []));
  }, [org, roleId]);

  const selectRole = (id: string) => {
    setRoleId(id);
    if (!id) setUsers([]);
  };

  const makeDefault = async () => {
    if (!roleId) return;
    await api.setDefaultRole(org, roleId);
    onChanged();
  };

  const makeAdmin = async (login: string) => {
    await api.setSoleAdmin(org, login);
    onChanged();
  };

  return (
    <Card
      title="Members by role"
      actions={roleId
        ? <SecondaryButton onClick={makeDefault}>{roleId === defaultRoleId ? "Default role" : "Set as default role"}</SecondaryButton>
        : undefined}
    >
      <div className="px-6 py-4">
        <select
          value={roleId}
          onChange={(e) => selectRole(e.target.value)}
          className="mb-3 w-full max-w-sm rounded-md border border-line bg-bg px-2.5 py-1.5 text-sm outline-none focus:border-brand"
        >
          <option value="">Select a role…</option>
          {roles.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
        </select>
        <Table<RbacUserInfo>
          rows={users}
          empty={<p className="text-sm text-ink-dim">{roleId ? "No members hold this role." : "Select a role to list its members."}</p>}
          columns={[
            { header: "User", cell: (u) => <span className="font-medium">{u.githubLogin}</span> },
            {
              header: "", className: "text-right",
              cell: (u) => (
                <button onClick={() => makeAdmin(u.githubLogin)} className="text-brand hover:underline">
                  Set as sole admin
                </button>
              ),
            },
          ]}
        />
        {defaultRoleId && (
          <p className="mt-3 text-xs text-ink-dim">
            Default role: <Badge tone="brand">{roles.find((r) => r.id === defaultRoleId)?.name ?? defaultRoleId}</Badge>
          </p>
        )}
      </div>
    </Card>
  );
}
