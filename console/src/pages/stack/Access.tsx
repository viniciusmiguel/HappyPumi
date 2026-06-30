import { useCallback, useEffect, useState } from "react";
import { Users } from "lucide-react";
import {
  api, type StackCollaborators, type StackTeams, type StackTeamGrant,
} from "../../lib/api";
import { Avatar, Badge, Card, EmptyState, Table, SecondaryButton, Modal } from "../../components/ui";

// Pulumi stack permission levels (numeric on the wire) ↔ their display labels.
const LEVELS: { value: number; label: string }[] = [
  { value: 101, label: "read" },
  { value: 102, label: "write" },
  { value: 103, label: "admin" },
];
const levelLabel = (p: number): string => LEVELS.find((l) => l.value === p)?.label ?? "none";

type Props = { org: string; project: string; stack: string };

// The Access tab: user collaborators (with their permission, removable) and the teams granted access to
// the stack (with an editable permission level). Backed by the collaborators/teams endpoints (PR4).
export function Access({ org, project, stack }: Props) {
  const [collab, setCollab] = useState<StackCollaborators | null>(null);
  const [teams, setTeams] = useState<StackTeams | null>(null);

  const reload = useCallback(() => {
    api.stackCollaborators(org, project, stack).then(setCollab);
    api.stackTeams(org, project, stack).then(setTeams);
  }, [org, project, stack]);

  useEffect(() => { reload(); }, [reload]);

  return (
    <div className="max-w-3xl space-y-4">
      <Collaborators org={org} project={project} stack={stack} data={collab} onChange={reload} />
      <Teams org={org} project={project} stack={stack} data={teams} onChange={reload} />
    </div>
  );
}

function Collaborators({ org, project, stack, data, onChange }: Props & {
  data: StackCollaborators | null; onChange: () => void;
}) {
  const [removing, setRemoving] = useState<string | null>(null);

  const confirmRemove = async () => {
    if (!removing) return;
    await api.removeStackCollaborator(org, project, stack, removing);
    setRemoving(null);
    onChange();
  };

  const users = data?.users ?? [];
  return (
    <Card title="Collaborators">
      {users.length === 0 ? (
        <EmptyState icon={Users} title="No collaborators" description="Users with direct access to this stack appear here." />
      ) : (
        <Table
          rows={users}
          columns={[
            { header: "User", cell: (u) => (
              <span className="flex items-center gap-2">
                <Avatar name={u.user.name ?? u.user.githubLogin} size={20} />
                <span className="font-medium">{u.user.githubLogin ?? u.user.name}</span>
                {u.user.githubLogin === data?.stackCreatorUserName && <Badge tone="brand">creator</Badge>}
              </span>
            ) },
            { header: "Permission", cell: (u) => <Badge>{levelLabel(u.permission)}</Badge> },
            { header: "", cell: (u) => (
              <SecondaryButton onClick={() => setRemoving(u.user.githubLogin ?? u.user.name ?? "")}>Remove</SecondaryButton>
            ) },
          ]}
        />
      )}
      {removing && (
        <Modal title="Remove collaborator" onClose={() => setRemoving(null)}
          footer={<>
            <SecondaryButton onClick={() => setRemoving(null)}>Cancel</SecondaryButton>
            <button onClick={confirmRemove}
              className="rounded-md bg-red-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-red-500">Remove</button>
          </>}>
          <p className="text-sm text-ink-dim">
            Remove <b className="text-ink">{removing}</b>'s direct access to this stack? Team and
            organization-inherited access is unaffected.
          </p>
        </Modal>
      )}
    </Card>
  );
}

function Teams({ org, project, stack, data, onChange }: Props & {
  data: StackTeams | null; onChange: () => void;
}) {
  const setPermission = async (team: StackTeamGrant, permission: number) => {
    await api.updateStackTeamPermission(org, project, stack, team.name, permission);
    onChange();
  };

  const teams = data?.teams ?? [];
  if (teams.length === 0) {
    return (
      <Card title="Teams">
        <EmptyState icon={Users} title="No teams" description="Teams granted access to this stack appear here." />
      </Card>
    );
  }
  return (
    <Card title="Teams">
      <Table
        rows={teams}
        columns={[
          { header: "Team", cell: (t) => (
            <span className="font-medium">{t.displayName || t.name}{t.isMember && <Badge tone="brand">member</Badge>}</span>
          ) },
          { header: "Permission", cell: (t) => (
            <select value={t.permission} onChange={(e) => setPermission(t, Number(e.target.value))}
              className="rounded-md border border-line bg-bg px-2.5 py-1 text-sm outline-none focus:border-brand">
              {LEVELS.map((l) => <option key={l.value} value={l.value}>{l.label}</option>)}
            </select>
          ) },
        ]}
      />
    </Card>
  );
}
