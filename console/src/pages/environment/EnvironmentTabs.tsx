import { useEffect, useState } from "react";
import { Plus, RefreshCw, Link2 } from "lucide-react";
import {
  api, timeAgo, type EnvRevision, type EscValue, type Actor,
  type EnvTag, type EnvWebhook, type EnvSchedule, type RotationEvent, type EnvReferrer,
} from "../../lib/api";
import { Table, Card, Avatar, EmptyState, PrimaryButton, SecondaryButton, Modal, Field, Badge } from "../../components/ui";

interface EnvProps { org: string; project: string; name: string; }

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

export function Editor({ org, project, name }: EnvProps) {
  const [yaml, setYaml] = useState("");
  const [preview, setPreview] = useState("{}");
  const [status, setStatus] = useState<string | null>(null);

  function refreshPreview(y: string) {
    api.checkEnvironment(org, y).then((r) => {
      const resolved: Record<string, unknown> = {};
      for (const [k, v] of Object.entries(r.properties ?? {})) resolved[k] = resolve(v);
      setPreview(JSON.stringify(resolved, null, 2));
    });
  }

  useEffect(() => { api.environmentYaml(org, project, name).then((y) => { setYaml(y); refreshPreview(y); }); }, [org, project, name]);

  async function save() {
    setStatus("Saving…");
    try {
      await api.updateEnvironment(org, project, name, yaml);
      refreshPreview(yaml);
      setStatus("Saved");
    } catch (e) {
      setStatus(e instanceof Error ? e.message : String(e));
    }
  }

  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <p className="text-sm text-ink-dim">Edit the environment definition and save to record a new revision. The resolved preview is on the right.</p>
        <div className="flex items-center gap-3">
          {status && <span className="text-xs text-ink-faint">{status}</span>}
          <PrimaryButton onClick={save}>Save</PrimaryButton>
        </div>
      </div>
      <div className="grid gap-3 lg:grid-cols-2">
        <div className="rounded-lg border border-line">
          <div className="border-b border-line px-3 py-2 text-sm font-medium">Environment definition</div>
          <textarea value={yaml} onChange={(e) => setYaml(e.target.value)} spellCheck={false}
            className="h-[60vh] w-full resize-none bg-transparent p-3 font-mono text-xs leading-relaxed outline-none" />
        </div>
        <div className="rounded-lg border border-line">
          <div className="border-b border-line px-3 py-2 text-sm font-medium">Environment preview</div>
          <pre className="max-h-[60vh] overflow-auto p-3 font-mono text-xs leading-relaxed text-ink-dim">{preview}</pre>
        </div>
      </div>
    </div>
  );
}

export function Versions({ revisions }: { revisions: EnvRevision[] }) {
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

export function TagsTab({ org, project, name }: EnvProps) {
  const [tags, setTags] = useState<EnvTag[]>([]);
  const [draft, setDraft] = useState<{ name: string; value: string } | null>(null);
  const [error, setError] = useState<string | null>(null);

  function load() {
    api.environmentTags(org, project, name).then((r) => setTags(Object.values(r.tags ?? {})));
  }
  useEffect(load, [org, project, name]);

  async function create() {
    if (!draft?.name) return;
    try { await api.createEnvironmentTag(org, project, name, draft.name, draft.value); setDraft(null); setError(null); load(); }
    catch (e) { setError(e instanceof Error ? e.message : String(e)); }
  }
  async function remove(tag: string) { await api.deleteEnvironmentTag(org, project, name, tag); load(); }

  return (
    <div className="max-w-3xl">
      <SecondaryButton icon={Plus} onClick={() => setDraft({ name: "", value: "" })}>New Tag</SecondaryButton>
      <div className="mt-3">
        <Table
          rows={tags}
          empty="No environment tags."
          columns={[
            { header: "Name", cell: (t) => <span className="font-mono text-xs">{t.name}</span> },
            { header: "Value", cell: (t) => t.value },
            { header: "", cell: (t) => <button onClick={() => remove(t.name)} className="text-xs text-red-400 hover:underline">Remove</button> },
          ]}
        />
      </div>
      {draft && (
        <Modal title="New environment tag" onClose={() => setDraft(null)}
          footer={<><SecondaryButton onClick={() => setDraft(null)}>Cancel</SecondaryButton><PrimaryButton onClick={create}>Create</PrimaryButton></>}>
          <Field label="Name" value={draft.name} onChange={(v) => setDraft((d) => ({ ...d!, name: v }))} placeholder="environment" />
          <Field label="Value" value={draft.value} onChange={(v) => setDraft((d) => ({ ...d!, value: v }))} placeholder="production" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}

export function WebhooksTab({ org, project, name }: EnvProps) {
  const [hooks, setHooks] = useState<EnvWebhook[]>([]);
  const [draft, setDraft] = useState<{ name: string; payloadUrl: string } | null>(null);
  const [error, setError] = useState<string | null>(null);

  function load() { api.environmentWebhooks(org, project, name).then(setHooks); }
  useEffect(load, [org, project, name]);

  async function create() {
    if (!draft?.name || !draft.payloadUrl) return;
    try { await api.createEnvironmentWebhook(org, project, name, draft); setDraft(null); setError(null); load(); }
    catch (e) { setError(e instanceof Error ? e.message : String(e)); }
  }
  async function remove(hook: string) { await api.deleteEnvironmentWebhook(org, project, name, hook); load(); }

  return (
    <div className="max-w-3xl">
      <SecondaryButton icon={Plus} onClick={() => setDraft({ name: "", payloadUrl: "" })}>New Webhook</SecondaryButton>
      <div className="mt-3">
        <Table
          rows={hooks}
          empty="No webhooks. Webhooks notify an endpoint when this environment changes."
          columns={[
            { header: "Name", cell: (h) => <span className="font-medium">{h.displayName || h.name}</span> },
            { header: "Payload URL", cell: (h) => <span className="font-mono text-xs text-ink-dim">{h.payloadUrl}</span> },
            { header: "Status", cell: (h) => <Badge tone={h.active ? "success" : "default"}>{h.active ? "active" : "disabled"}</Badge> },
            { header: "", cell: (h) => <button onClick={() => remove(h.name)} className="text-xs text-red-400 hover:underline">Remove</button> },
          ]}
        />
      </div>
      {draft && (
        <Modal title="New webhook" onClose={() => setDraft(null)}
          footer={<><SecondaryButton onClick={() => setDraft(null)}>Cancel</SecondaryButton><PrimaryButton onClick={create}>Create</PrimaryButton></>}>
          <Field label="Name" value={draft.name} onChange={(v) => setDraft((d) => ({ ...d!, name: v }))} placeholder="my-webhook" />
          <Field label="Payload URL" value={draft.payloadUrl} onChange={(v) => setDraft((d) => ({ ...d!, payloadUrl: v }))} placeholder="https://example.com/hook" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}

export function SchedulesTab({ org, project, name }: EnvProps) {
  const [schedules, setSchedules] = useState<EnvSchedule[]>([]);
  const [draft, setDraft] = useState<{ kind: string; scheduleCron: string } | null>(null);
  const [error, setError] = useState<string | null>(null);

  function load() { api.environmentSchedules(org, project, name).then((r) => setSchedules(r.schedules ?? [])); }
  useEffect(load, [org, project, name]);

  async function create() {
    if (!draft?.scheduleCron) return;
    try { await api.createEnvironmentSchedule(org, project, name, draft); setDraft(null); setError(null); load(); }
    catch (e) { setError(e instanceof Error ? e.message : String(e)); }
  }
  async function remove(id: string) { await api.deleteEnvironmentSchedule(org, project, name, id); load(); }

  return (
    <div className="max-w-3xl">
      <SecondaryButton icon={Plus} onClick={() => setDraft({ kind: "rotation", scheduleCron: "0 0 * * *" })}>New Scheduled Action</SecondaryButton>
      <div className="mt-3">
        <Table
          rows={schedules}
          empty="No scheduled actions. Schedule secret rotation or environment deletion on a cron."
          columns={[
            { header: "Kind", cell: (s) => <Badge tone="brand">{s.kind}</Badge> },
            { header: "Schedule", cell: (s) => <span className="font-mono text-xs">{s.scheduleCron || "once"}</span> },
            { header: "Next run", cell: (s) => <span className="text-ink-dim">{s.paused ? "paused" : (s.nextExecution ? timeAgo(s.nextExecution) : "—")}</span> },
            { header: "", cell: (s) => <button onClick={() => remove(s.id)} className="text-xs text-red-400 hover:underline">Remove</button> },
          ]}
        />
      </div>
      {draft && (
        <Modal title="New scheduled action" onClose={() => setDraft(null)}
          footer={<><SecondaryButton onClick={() => setDraft(null)}>Cancel</SecondaryButton><PrimaryButton onClick={create}>Create</PrimaryButton></>}>
          <Field label="Kind" value={draft.kind} onChange={(v) => setDraft((d) => ({ ...d!, kind: v }))}
            options={["rotation", "deletion"]} />
          <Field label="Cron schedule" value={draft.scheduleCron} onChange={(v) => setDraft((d) => ({ ...d!, scheduleCron: v }))} placeholder="0 0 * * *" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}

export function RotationTab({ org, project, name }: EnvProps) {
  const [events, setEvents] = useState<RotationEvent[]>([]);
  const [status, setStatus] = useState<string | null>(null);

  function load() { api.rotationHistory(org, project, name).then((r) => setEvents(r.events ?? [])); }
  useEffect(load, [org, project, name]);

  async function rotate() {
    setStatus("Rotating…");
    try { await api.rotateEnvironment(org, project, name); setStatus("Rotation complete"); load(); }
    catch (e) { setStatus(e instanceof Error ? e.message : String(e)); }
  }

  return (
    <div className="max-w-3xl">
      <div className="mb-3 flex items-center gap-3">
        <PrimaryButton icon={RefreshCw} onClick={rotate}>Rotate now</PrimaryButton>
        {status && <span className="text-xs text-ink-faint">{status}</span>}
      </div>
      <Table
        rows={events}
        empty="No rotations yet. Rotating runs every fn::rotate in the definition and records a new revision."
        columns={[
          { header: "When", cell: (e) => <span className="text-ink-dim">{timeAgo(e.created)}</span> },
          { header: "Revisions", cell: (e) => <span className="font-mono text-xs">{e.preRotationRevision} → {e.postRotationRevision ?? "—"}</span> },
          { header: "Result", cell: (e) => e.errorMessage ? <Badge tone="danger">failed</Badge> : <Badge tone="success">ok</Badge> },
        ]}
      />
    </div>
  );
}

export function ImportedByTab({ org, project, name }: EnvProps) {
  const [referrers, setReferrers] = useState<EnvReferrer[]>([]);

  useEffect(() => {
    api.environmentReferrers(org, project, name).then((r) => setReferrers(Object.values(r.referrers ?? {}).flat()));
  }, [org, project, name]);

  if (referrers.length === 0)
    return <EmptyState icon={Link2} title="No import references"
      description="No other environments, stacks, or Insights accounts import this environment." />;

  return (
    <Table
      rows={referrers}
      columns={[
        { header: "Type", cell: (r) => r.environment ? "Environment" : r.stack ? "Stack" : "Insights account" },
        { header: "Referrer", cell: (r) => <span className="font-mono text-xs">
          {r.environment ? `${r.environment.project}/${r.environment.name}`
            : r.stack ? `${r.stack.projectName}/${r.stack.stackName}`
            : r.insightsAccount?.name}</span> },
      ]}
    />
  );
}

export function SettingsTab({ org, project, name, owner }: EnvProps & { owner?: Actor }) {
  const [protectedDeletion, setProtectedDeletion] = useState(false);
  // Local override wins once the user transfers ownership; otherwise show the loaded owner prop.
  const [override, setOverride] = useState<Actor | undefined>(undefined);
  const current = override ?? owner;
  const [show, setShow] = useState(false);
  const [newOwner, setNewOwner] = useState("");

  useEffect(() => { api.environmentSettings(org, project, name).then((s) => setProtectedDeletion(!!s.deletionProtected)); }, [org, project, name]);

  async function toggleProtection() {
    const next = !protectedDeletion;
    setProtectedDeletion(next);
    await api.updateEnvironmentSettings(org, project, name, next);
  }
  async function changeOwner() {
    if (!newOwner) return;
    await api.reassignEnvironmentOwner(org, project, name, newOwner);
    setOverride({ githubLogin: newOwner, name: newOwner });
    setShow(false); setNewOwner("");
  }

  return (
    <div className="max-w-2xl space-y-6">
      <div>
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

      <div>
        <h2 className="mb-3 text-base font-semibold">Deletion protection</h2>
        <Card>
          <div className="flex items-center justify-between">
            <p className="text-sm text-ink-dim">When enabled, this environment cannot be deleted until protection is turned off.</p>
            <SecondaryButton onClick={toggleProtection}>{protectedDeletion ? "Disable" : "Enable"}</SecondaryButton>
          </div>
        </Card>
      </div>

      {show && (
        <Modal title="Change owner" onClose={() => setShow(false)}
          footer={<><SecondaryButton onClick={() => setShow(false)}>Cancel</SecondaryButton><PrimaryButton onClick={changeOwner}>Transfer ownership</PrimaryButton></>}>
          <Field label="New owner (GitHub login or team)" value={newOwner} onChange={setNewOwner} placeholder="platform-team" />
        </Modal>
      )}
    </div>
  );
}
