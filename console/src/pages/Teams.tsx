import { useEffect, useState } from "react";
import { Users, Plus } from "lucide-react";
import { api, type Team } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton, SecondaryButton, Modal, Field, Badge } from "../components/ui";

export default function Teams() {
  const org = useOrg();
  const [teams, setTeams] = useState<Team[]>([]);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ name: "", displayName: "", description: "" });
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
    </div>
  );
}
