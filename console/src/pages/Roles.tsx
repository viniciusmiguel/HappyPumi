import { useEffect, useState } from "react";
import { KeyRound, Plus } from "lucide-react";
import { api, type Role } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton } from "../components/ui";

export default function Roles() {
  const org = useOrg();
  const [roles, setRoles] = useState<Role[]>([]);
  useEffect(() => { api.roles(org).then((r) => setRoles(r.roles ?? [])); }, [org]);

  return (
    <div>
      <PageHeader icon={KeyRound} title="Roles" actions={<PrimaryButton icon={Plus}>New role</PrimaryButton>} />
      <Table
        rows={roles}
        columns={[
          { header: "Name", cell: (r) => <span className="font-medium">{r.name}</span> },
          { header: "Description", cell: (r) => <span className="text-ink-dim">{r.description ?? "—"}</span> },
        ]}
        empty={<EmptyState icon={KeyRound} title="No custom roles"
          description="Create custom roles to grant fine-grained permissions across your organization."
          action={<PrimaryButton icon={Plus}>New role</PrimaryButton>} />}
      />
    </div>
  );
}
