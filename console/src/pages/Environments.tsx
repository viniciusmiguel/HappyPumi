import { useEffect, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { KeyRound, Plus, Folder, ChevronDown, Search } from "lucide-react";
import { api, timeAgo, type OrgEnvironment } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, EmptyState, PrimaryButton, SecondaryButton, Modal, Field } from "../components/ui";

export default function Environments() {
  const org = useOrg();
  const navigate = useNavigate();
  const [envs, setEnvs] = useState<OrgEnvironment[]>([]);
  const [q, setQ] = useState("");
  const [searchParams] = useSearchParams();
  const [showNew, setShowNew] = useState(searchParams.get("new") === "1");
  const [form, setForm] = useState({ project: "default", name: "" });
  const [error, setError] = useState<string | null>(null);

  function load() { api.environments(org).then((r) => setEnvs(r.environments ?? [])); }
  useEffect(() => { load(); }, [org]);

  async function createEnv() {
    if (!form.name) return;
    setError(null);
    try {
      await api.createEnvironment(org, form.project, form.name);
      const dest = `/environments/${form.project}/${form.name}`;
      setShowNew(false);
      navigate(dest);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    }
  }

  const filtered = envs.filter((e) => `${e.project}/${e.name}`.includes(q));
  const groups = [...new Set(filtered.map((e) => e.project ?? "default"))].sort()
    .map((p) => ({ project: p, items: filtered.filter((e) => (e.project ?? "default") === p) }));
  const projectCount = new Set(envs.map((e) => e.project)).size;

  return (
    <div>
      <PageHeader icon={KeyRound} title="Environments"
        actions={
          <div className="flex items-center gap-3 text-sm text-ink-dim">
            <span>Projects: <span className="rounded bg-panel px-1.5 py-0.5">{projectCount}</span></span>
            <span>Environments: <span className="rounded bg-panel px-1.5 py-0.5">{envs.length}</span></span>
          </div>
        } />
      <div className="flex items-center justify-between px-6 py-3">
        <PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>Create Environment</PrimaryButton>
        <div className="flex items-center gap-2 rounded-md border border-line bg-panel px-2.5 py-1.5 text-sm">
          <Search size={14} className="text-ink-faint" />
          <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search"
            className="w-56 bg-transparent outline-none placeholder:text-ink-faint" />
        </div>
      </div>

      {envs.length === 0 ? (
        <EmptyState icon={KeyRound} title="No environments"
          description="You don't have any environments yet. Use the button above to create an environment."
          action={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>Create Environment</PrimaryButton>} />
      ) : (
        <div className="space-y-3 px-6 pb-8">
          {groups.map((g) => (
            <div key={g.project} className="overflow-hidden rounded-lg border border-line">
              <div className="flex items-center justify-between border-b border-line bg-panel px-4 py-2.5">
                <div className="flex items-center gap-2 text-sm font-semibold">
                  <ChevronDown size={14} className="text-ink-faint" />
                  <Folder size={15} className="text-ink-faint" /> {g.project}
                  <span className="rounded bg-bg px-1.5 text-xs text-ink-dim">{g.items.length}</span>
                </div>
              </div>
              {g.items.map((e) => (
                <Link key={e.id} to={`/environments/${e.project}/${e.name}`}
                  className="flex items-center justify-between border-b border-line/60 px-4 py-2.5 text-sm last:border-0 hover:bg-hover">
                  <span className="flex items-center gap-2 pl-6"><KeyRound size={14} className="text-ink-faint" />{e.name}</span>
                  <span className="text-xs text-ink-faint">Updated {timeAgo(e.modified)}</span>
                </Link>
              ))}
            </div>
          ))}
        </div>
      )}

      {showNew && (
        <Modal title="Create Environment" onClose={() => setShowNew(false)}
          footer={<>
            <SecondaryButton onClick={() => setShowNew(false)}>Cancel</SecondaryButton>
            <PrimaryButton onClick={createEnv}>Create</PrimaryButton>
          </>}>
          <Field label="Project" value={form.project} onChange={(v) => setForm((f) => ({ ...f, project: v }))} placeholder="default" />
          <Field label="Environment name" value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} placeholder="dev" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}
