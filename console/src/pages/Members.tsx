import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Users, UserPlus } from "lucide-react";
import { api, type Member } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton, SecondaryButton, Modal, Field } from "../components/ui";

function login(m: Member) { return m.githubLogin ?? m.user?.githubLogin ?? m.name ?? "—"; }

export default function Members() {
  const org = useOrg();
  const [members, setMembers] = useState<Member[]>([]);
  const [searchParams] = useSearchParams();
  const [showInvite, setShowInvite] = useState(searchParams.get("new") === "1");
  const [form, setForm] = useState({ login: "", role: "member" });
  const [error, setError] = useState<string | null>(null);

  function load() { api.members(org).then((r) => setMembers(r.members ?? [])); }
  useEffect(() => { load(); }, [org]);

  async function invite() {
    if (!form.login) return;
    setError(null);
    try {
      await api.addMember(org, form.login, form.role);
      setShowInvite(false);
      setForm({ login: "", role: "member" });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    }
  }

  return (
    <div>
      <PageHeader icon={Users} title="Members" actions={<PrimaryButton icon={UserPlus} onClick={() => setShowInvite(true)}>Invite</PrimaryButton>} />
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
          action={<PrimaryButton icon={UserPlus} onClick={() => setShowInvite(true)}>Invite member</PrimaryButton>} />}
      />

      {showInvite && (
        <Modal title="Invite a member" onClose={() => setShowInvite(false)}
          footer={<>
            <SecondaryButton onClick={() => setShowInvite(false)}>Cancel</SecondaryButton>
            <PrimaryButton onClick={invite}>Send invite</PrimaryButton>
          </>}>
          <Field label="GitHub login or email" value={form.login} onChange={(v) => setForm((f) => ({ ...f, login: v }))} placeholder="octocat" />
          <Field label="Role" value={form.role} onChange={(v) => setForm((f) => ({ ...f, role: v }))} options={["member", "admin"]} />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}
