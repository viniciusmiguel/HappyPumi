import { useEffect, useState } from "react";
import { Users, Plus, MoreHorizontal, Pencil, Trash2, ShieldCheck } from "lucide-react";
import { api, type Team, type Role } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import {
  PageHeader, Table, EmptyState, PrimaryButton, SecondaryButton, Modal, Field, Badge, Dropdown,
} from "../components/ui";

type EditForm = { name: string; newName: string; newDisplayName: string; newDescription: string };

export default function Teams() {
  const org = useOrg();
  const [teams, setTeams] = useState<Team[]>([]);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ name: "", displayName: "", description: "" });
  const [edit, setEdit] = useState<EditForm | null>(null);
  const [roles, setRoles] = useState<{ team: string; roles: Role[] } | null>(null);
  const [error, setError] = useState<string | null>(null);

  function load() { api.teams(org).then((r) => setTeams(r.teams ?? [])); }
  useEffect(() => { load(); }, [org]);

  async function create() {
    if (!form.name) return;
    setError(null);
    try {
      await api.createTeam(org, form.name, form.displayName || form.name, form.description);
      setShowNew(false);
      setForm({ name: "", displayName: "", description: "" });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    }
  }

  async function saveEdit() {
    if (!edit) return;
    setError(null);
    try {
      await api.updateTeam(org, edit.name, {
        newName: edit.newName && edit.newName !== edit.name ? edit.newName : undefined,
        newDisplayName: edit.newDisplayName,
        newDescription: edit.newDescription,
      });
      setEdit(null);
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    }
  }

  async function remove(team: Team) {
    if (!confirm(`Delete team "${team.displayName || team.name}"? This cannot be undone.`)) return;
    await api.deleteTeam(org, team.name);
    load();
  }

  async function viewRoles(team: Team) {
    const r = await api.teamRoles(org, team.name);
    setRoles({ team: team.name, roles: r.roles ?? [] });
  }

  async function enableRoles(team: Team) {
    setError(null);
    try {
      await api.enableTeamRoles(org, team.name);
      load();
      await viewRoles(team);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    }
  }

  return (
    <div>
      <PageHeader icon={Users} title="Teams"
        actions={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>New team</PrimaryButton>} />
      <Table
        rows={teams}
        columns={[
          { header: "Team", cell: (t) => (
            <div>
              <span className="font-medium">{t.displayName || t.name}</span>
              {t.description && <div className="text-xs text-ink-faint">{t.description}</div>}
            </div>
          ) },
          { header: "Kind", cell: (t) => <Badge tone={t.kind === "github" ? "brand" : "default"}>{t.kind ?? "pulumi"}</Badge> },
          { header: "Members", cell: (t) => <span className="text-ink-dim">{t.members?.length ?? 0}</span> },
          { header: "Roles", cell: (t) => <span className="text-ink-dim">{t.roleIds?.length ?? 0}</span> },
          { header: "", cell: (t) => (
            <div className="flex justify-end">
              <Dropdown
                trigger={<button className="rounded p-1 text-ink-dim hover:bg-hover hover:text-ink"><MoreHorizontal size={16} /></button>}
                items={[
                  { label: "Edit team", icon: Pencil, onSelect: () => setEdit({ name: t.name, newName: t.name, newDisplayName: t.displayName || "", newDescription: t.description || "" }) },
                  { label: "View roles", icon: ShieldCheck, onSelect: () => viewRoles(t) },
                  { label: "Enable team roles", icon: ShieldCheck, onSelect: () => enableRoles(t) },
                  { label: "Delete team", icon: Trash2, onSelect: () => remove(t), danger: true },
                ]}
              />
            </div>
          ) },
        ]}
        empty={<EmptyState icon={Users} title="No teams"
          description="Teams group members so you can grant stack and environment access to many people at once."
          action={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>New team</PrimaryButton>} />}
      />

      {showNew && (
        <Modal title="Create a team" onClose={() => setShowNew(false)}
          footer={<>
            <SecondaryButton onClick={() => setShowNew(false)}>Cancel</SecondaryButton>
            <PrimaryButton onClick={create}>Create</PrimaryButton>
          </>}>
          <Field label="Name" value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} placeholder="platform-eng" />
          <Field label="Display name" value={form.displayName} onChange={(v) => setForm((f) => ({ ...f, displayName: v }))} placeholder="Platform Engineering" />
          <Field label="Description" value={form.description} onChange={(v) => setForm((f) => ({ ...f, description: v }))} placeholder="Owns shared platform stacks" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}

      {edit && (
        <Modal title="Edit team" onClose={() => setEdit(null)}
          footer={<>
            <SecondaryButton onClick={() => setEdit(null)}>Cancel</SecondaryButton>
            <PrimaryButton onClick={saveEdit}>Save</PrimaryButton>
          </>}>
          <Field label="Name" value={edit.newName} onChange={(v) => setEdit((e) => e && ({ ...e, newName: v }))} placeholder="platform-eng" />
          <Field label="Display name" value={edit.newDisplayName} onChange={(v) => setEdit((e) => e && ({ ...e, newDisplayName: v }))} placeholder="Platform Engineering" />
          <Field label="Description" value={edit.newDescription} onChange={(v) => setEdit((e) => e && ({ ...e, newDescription: v }))} placeholder="Owns shared platform stacks" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}

      {roles && (
        <Modal title={`Roles for ${roles.team}`} onClose={() => setRoles(null)}
          footer={<SecondaryButton onClick={() => setRoles(null)}>Close</SecondaryButton>}>
          {roles.roles.length === 0
            ? <p className="text-sm text-ink-dim">No fine-grained roles are assigned. Use “Enable team roles” to grant the org’s default role.</p>
            : <ul className="space-y-1">{roles.roles.map((r) => (
                <li key={r.id} className="flex items-center gap-2 text-sm">
                  <Badge tone="brand">{r.name || r.id}</Badge>
                  {r.description && <span className="text-ink-faint">{r.description}</span>}
                </li>
              ))}</ul>}
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}
