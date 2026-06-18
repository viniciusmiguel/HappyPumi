import { useEffect, useState } from "react";
import { Users, UserPlus } from "lucide-react";
import { api, type Member } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton } from "../components/ui";

function login(m: Member) { return m.githubLogin ?? m.user?.githubLogin ?? m.name ?? "—"; }

export default function Members() {
  const org = useOrg();
  const [members, setMembers] = useState<Member[]>([]);
  useEffect(() => { api.members(org).then((r) => setMembers(r.members ?? [])); }, [org]);

  return (
    <div>
      <PageHeader icon={Users} title="Members" actions={<PrimaryButton icon={UserPlus}>Invite</PrimaryButton>} />
      <Table
        rows={members}
        columns={[
          { header: "Member", cell: (m) => (
            <span className="flex items-center gap-2">
              <span className="grid size-6 place-items-center rounded-full bg-panel text-xs">{login(m)[0]?.toUpperCase()}</span>
              <span className="font-medium">{login(m)}</span>
            </span>
          ) },
          { header: "Role", cell: (m) => <span className="rounded bg-panel px-2 py-0.5 text-xs text-ink-dim">{m.role ?? "member"}</span> },
        ]}
        empty={<EmptyState icon={Users} title="No members"
          description="Invite people to your organization and manage their roles in one place."
          action={<PrimaryButton icon={UserPlus}>Invite member</PrimaryButton>} />}
      />
    </div>
  );
}
