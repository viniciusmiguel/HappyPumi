import { useEffect, useState } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { KeyRound, Play, Save, Plus } from "lucide-react";
import { api, timeAgo, type EnvRevision, type EscValue, type Actor } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Breadcrumb, Tabs, Table, Card, Avatar, EmptyState, PrimaryButton, SecondaryButton, Modal, Field } from "../components/ui";

const TABS = [
  { key: "editor", label: "Editor" },
  { key: "versions", label: "Versions" },
  { key: "tags", label: "Environment Tags" },
  { key: "imported", label: "Imported By" },
  { key: "settings", label: "Settings" },
];

// Recursively resolve an EscValue tree into plain JSON, masking secrets like the console's preview.
function resolve(v: EscValue): unknown {
  if (v?.secret) return "[secret]";
  const val = v?.value;
  if (val && typeof val === "object" && !Array.isArray(val)) {
    const out: Record<string, unknown> = {};
    for (const [k, child] of Object.entries(val)) out[k] = resolve(child as EscValue);
    return out;
  }
  return val;
}

export default function EnvironmentDetail() {
  const org = useOrg();
  const { project = "", name = "" } = useParams();
  const [params, setParams] = useSearchParams();
  const active = params.get("tab") || "editor";

  const [yaml, setYaml] = useState("");
  const [preview, setPreview] = useState<string>("");
  const [revisions, setRevisions] = useState<EnvRevision[]>([]);
  const [owner, setOwner] = useState<Actor | undefined>();

  useEffect(() => {
    api.environmentYaml(org, project, name).then((y) => {
      setYaml(y);
      api.checkEnvironment(org, y).then((r) => {
        const props = r.properties ?? {};
        const resolved: Record<string, unknown> = {};
        for (const [k, v] of Object.entries(props)) resolved[k] = resolve(v);
        setPreview(JSON.stringify(resolved, null, 2));
      });
    });
    api.environmentRevisions(org, project, name).then(setRevisions);
    api.environments(org).then((r) => setOwner(r.environments?.find((e) => e.project === project && e.name === name)?.ownedBy));
  }, [org, project, name]);

  return (
    <div className="pb-10">
      <div className="px-6 pt-5">
        <div className="mb-2 flex items-center gap-2.5">
          <span className="grid size-7 place-items-center rounded-md border border-line bg-panel text-ink-dim"><KeyRound size={16} /></span>
          <h1 className="text-xl font-semibold">{name}</h1>
        </div>
        <Breadcrumb items={[{ label: "Environments", to: "/environments" }, { label: project }, { label: name }]} />
      </div>

      <div className="mt-4">
        <Tabs tabs={TABS} active={active} onChange={(k) => setParams({ tab: k }, { replace: true })} />
      </div>

      <div className="px-6 py-5">
        {active === "editor" && <Editor org={org} project={project} name={name} yaml={yaml} preview={preview} />}
        {active === "versions" && <Versions revisions={revisions} />}
        {active === "tags" && <TagsEditor />}
        {active === "imported" && <EmptyState icon={KeyRound} title="No import references"
          description="No other environments, stacks, or Insights accounts import this environment." />}
        {active === "settings" && <Settings owner={owner} />}
      </div>
    </div>
  );
}

function Editor({ org, project, name, yaml, preview }: { org: string; project: string; name: string; yaml: string; preview: string }) {
  const [toast, setToast] = useState("");
  function flash(msg: string) { setToast(msg); setTimeout(() => setToast(""), 2500); }
  return (
    <div>
      <div className="mb-3 flex items-center justify-end gap-2">
        <SecondaryButton icon={Play} onClick={() => flash("Environment opened — resolved values shown in the preview.")}>Open</SecondaryButton>
        <PrimaryButton icon={Save} onClick={() => flash("Environment saved as a new revision.")}>Save</PrimaryButton>
      </div>
      {toast && <div className="mb-3 rounded-md border border-brand/40 bg-brand/10 px-3 py-2 text-sm text-ink">{toast}</div>}
      <p className="mb-3 text-sm text-ink-dim">
        To view the resolved value of this environment, select <b>Open</b>, or run{" "}
        <code className="rounded bg-bg px-1">pulumi env open {org}/{project}/{name}</code> on the command line.
      </p>
      <div className="grid gap-3 lg:grid-cols-2">
        <div className="rounded-lg border border-line">
          <div className="border-b border-line px-3 py-2 text-sm font-medium">Environment definition</div>
          <pre className="max-h-[60vh] overflow-auto p-3 font-mono text-xs leading-relaxed">{yaml || "# (empty)"}</pre>
        </div>
        <div className="rounded-lg border border-line">
          <div className="border-b border-line px-3 py-2 text-sm font-medium">Environment preview</div>
          <pre className="max-h-[60vh] overflow-auto p-3 font-mono text-xs leading-relaxed text-ink-dim">{preview || "{}"}</pre>
        </div>
      </div>
    </div>
  );
}

function Versions({ revisions }: { revisions: EnvRevision[] }) {
  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-sm font-semibold">All Revisions</h2>
        <span className="text-sm text-ink-dim">Revisions: <span className="rounded bg-panel px-1.5 py-0.5 text-xs">{revisions.length}</span></span>
      </div>
      <Table
        rows={revisions}
        columns={[
          { header: "Revision", cell: (r) => <span className="font-medium text-brand">Revision {r.number}</span> },
          { header: "Created by", cell: (r) => <span className="flex items-center gap-2"><Avatar name={r.creatorName} size={18} />{r.creatorLogin}</span> },
          { header: "Created", cell: (r) => <span className="text-ink-dim">created {timeAgo(r.created)}</span> },
          { header: "Revision tags", cell: (r) => (r.tags ?? []).map((t) => <span key={t} className="mr-1 rounded bg-panel px-1.5 py-0.5 text-xs">{t}</span>) },
        ]}
      />
    </div>
  );
}

function TagsEditor() {
  const [tags, setTags] = useState<{ name: string; value: string }[]>([]);
  const [draft, setDraft] = useState<{ name: string; value: string } | null>(null);

  function commit() {
    if (draft?.name) setTags((t) => [...t, draft]);
    setDraft(null);
  }

  return (
    <div className="max-w-3xl">
      <SecondaryButton icon={Plus} onClick={() => setDraft({ name: "", value: "" })}>New Tag</SecondaryButton>
      <div className="mt-3 grid grid-cols-[1fr_1fr_auto] gap-4 border-b border-line pb-2 text-xs font-medium uppercase tracking-wider text-ink-faint">
        <div>Name</div><div>Value</div><div></div>
      </div>
      {tags.map((t, i) => (
        <div key={i} className="grid grid-cols-[1fr_1fr_auto] items-center gap-4 border-b border-line/60 py-2 text-sm">
          <span className="font-mono text-xs">{t.name}</span><span>{t.value}</span>
          <button onClick={() => setTags((ts) => ts.filter((_, j) => j !== i))} className="text-xs text-red-400 hover:underline">Remove</button>
        </div>
      ))}
      {draft && (
        <div className="grid grid-cols-[1fr_1fr_auto] items-end gap-4 py-3">
          <Field label="Name" value={draft.name} onChange={(v) => setDraft((d) => ({ ...d!, name: v }))} placeholder="environment" />
          <Field label="Value" value={draft.value} onChange={(v) => setDraft((d) => ({ ...d!, value: v }))} placeholder="production" />
          <div className="flex gap-2 pb-0.5">
            <PrimaryButton onClick={commit}>Done</PrimaryButton>
            <SecondaryButton onClick={() => setDraft(null)}>Cancel</SecondaryButton>
          </div>
        </div>
      )}
      {tags.length === 0 && !draft && <p className="py-4 text-sm text-ink-dim">No environment tags.</p>}
    </div>
  );
}

function Settings({ owner }: { owner?: Actor }) {
  // Local override wins once the user transfers ownership; otherwise show the loaded owner prop.
  const [override, setOverride] = useState<Actor | undefined>(undefined);
  const current = override ?? owner;
  const [show, setShow] = useState(false);
  const [newOwner, setNewOwner] = useState("");

  function changeOwner() {
    if (newOwner) setOverride({ githubLogin: newOwner, name: newOwner });
    setShow(false);
    setNewOwner("");
  }

  return (
    <div className="flex gap-8">
      <nav className="w-48 shrink-0 space-y-0.5 text-sm">
        {["Access", "Deletion protection", "Notifications"].map((s, i) => (
          <div key={s} className={`rounded-md px-3 py-2 ${i === 0 ? "bg-active text-ink" : "text-ink-dim hover:bg-hover"}`}>{s}</div>
        ))}
      </nav>
      <div className="flex-1">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-base font-semibold">Team Access</h2>
          <SecondaryButton onClick={() => setShow(true)}>Change owner</SecondaryButton>
        </div>
        <Card>
          <div className="flex items-center gap-3">
            <Avatar name={current?.name} size={32} />
            <div className="flex-1">
              <div className="text-sm font-semibold">{current?.githubLogin ?? "—"}</div>
              <div className="text-xs text-ink-faint">{current?.name}</div>
            </div>
            <span className="rounded-full bg-brand/15 px-2 py-0.5 text-xs text-brand">Owner</span>
          </div>
        </Card>
      </div>

      {show && (
        <Modal title="Change owner" onClose={() => setShow(false)}
          footer={<>
            <SecondaryButton onClick={() => setShow(false)}>Cancel</SecondaryButton>
            <PrimaryButton onClick={changeOwner}>Transfer ownership</PrimaryButton>
          </>}>
          <Field label="New owner (GitHub login or team)" value={newOwner} onChange={setNewOwner} placeholder="platform-team" />
        </Modal>
      )}
    </div>
  );
}
