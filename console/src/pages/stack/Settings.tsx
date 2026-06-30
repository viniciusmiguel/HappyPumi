import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, type Stack } from "../../lib/api";
import { Card, Field, PrimaryButton, SecondaryButton } from "../../components/ui";
import { Webhooks } from "./Webhooks";

type Props = { org: string; project: string; stack: string; meta: Stack | null; onChanged: () => void; onDelete: () => void };

// The Settings tab: edit a tag value, transfer the stack to another org, reassign ownership, toggle
// notification preferences, and the delete danger zone. Backed by the PR6 settings-action endpoints.
export function Settings({ org, project, stack, meta, onChanged, onDelete }: Props) {
  return (
    <div className="max-w-2xl space-y-4">
      <Tags org={org} project={project} stack={stack} meta={meta} onChanged={onChanged} />
      <Notifications org={org} project={project} stack={stack} meta={meta} onChanged={onChanged} />
      <Ownership org={org} project={project} stack={stack} meta={meta} onChanged={onChanged} />
      <Webhooks org={org} project={project} stack={stack} />
      <Transfer org={org} project={project} stack={stack} />
      <Card title="Danger zone">
        <button onClick={onDelete} className="rounded-md border border-red-500/40 px-3 py-1.5 text-sm text-red-400 hover:bg-red-500/10">Delete stack</button>
      </Card>
    </div>
  );
}

function Tags({ org, project, stack, meta, onChanged }: Omit<Props, "onDelete">) {
  const tags = Object.entries(meta?.tags ?? {});
  if (tags.length === 0) return <Card title="Stack tags"><p className="text-sm text-ink-dim">No tags.</p></Card>;
  return (
    <Card title="Stack tags">
      <div className="space-y-2">
        {tags.map(([k, v]) => <TagRow key={k} org={org} project={project} stack={stack} name={k} value={v} onChanged={onChanged} />)}
      </div>
    </Card>
  );
}

function TagRow({ org, project, stack, name, value, onChanged }: { org: string; project: string; stack: string; name: string; value: string; onChanged: () => void }) {
  const [draft, setDraft] = useState(value);
  const [saving, setSaving] = useState(false);
  const save = async () => {
    setSaving(true);
    try { await api.updateStackTag(org, project, stack, name, draft); onChanged(); }
    finally { setSaving(false); }
  };
  return (
    <div className="flex items-end gap-2">
      <span className="mb-2 w-40 shrink-0 font-mono text-xs text-ink-dim">{name}</span>
      <div className="flex-1"><Field label="" value={draft} onChange={setDraft} /></div>
      <div className="mb-0.5"><SecondaryButton onClick={saving || draft === value ? undefined : save}>Save</SecondaryButton></div>
    </div>
  );
}

function Notifications({ org, project, stack, meta, onChanged }: Omit<Props, "onDelete">) {
  const current = meta?.notificationSettings;
  const [success, setSuccess] = useState(!!current?.notifyUpdateSuccess);
  const [failure, setFailure] = useState(!!current?.notifyUpdateFailure);
  const [saving, setSaving] = useState(false);
  const save = async () => {
    setSaving(true);
    try { await api.updateStackNotifications(org, project, stack, { notifyUpdateSuccess: success, notifyUpdateFailure: failure }); onChanged(); }
    finally { setSaving(false); }
  };
  return (
    <Card title="Notifications">
      <div className="space-y-2 text-sm">
        <Toggle label="Notify on update success" checked={success} onChange={setSuccess} />
        <Toggle label="Notify on update failure" checked={failure} onChange={setFailure} />
      </div>
      <div className="mt-3"><PrimaryButton onClick={saving ? undefined : save}>Save settings</PrimaryButton></div>
    </Card>
  );
}

function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <label className="flex items-center gap-2">
      <input type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} className="accent-brand" />
      <span>{label}</span>
    </label>
  );
}

function Ownership({ org, project, stack, meta, onChanged }: Omit<Props, "onDelete">) {
  const [owner, setOwner] = useState("");
  const [saving, setSaving] = useState(false);
  const reassign = async () => {
    if (!owner.trim()) return;
    setSaving(true);
    try { await api.reassignStackOwner(org, project, stack, owner.trim()); setOwner(""); onChanged(); }
    finally { setSaving(false); }
  };
  return (
    <Card title="Ownership">
      {meta?.ownedBy?.githubLogin && <p className="mb-2 text-sm text-ink-dim">Owned by <b className="text-ink">{meta.ownedBy.githubLogin}</b>.</p>}
      <div className="flex items-end gap-2">
        <div className="flex-1"><Field label="New owner login" value={owner} onChange={setOwner} placeholder="github-login" /></div>
        <div className="mb-0.5"><PrimaryButton onClick={saving ? undefined : reassign}>Reassign</PrimaryButton></div>
      </div>
    </Card>
  );
}

function Transfer({ org, project, stack }: { org: string; project: string; stack: string }) {
  const navigate = useNavigate();
  const [dest, setDest] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const transfer = async () => {
    if (!dest.trim()) return;
    setSaving(true); setError(null);
    try { await api.transferStack(org, project, stack, dest.trim()); navigate("/stacks"); }
    catch { setError(`Could not transfer the stack to '${dest.trim()}'. The destination may already have a stack of this name.`); }
    finally { setSaving(false); }
  };
  return (
    <Card title="Transfer stack">
      <p className="mb-2 text-sm text-ink-dim">Move this stack to another organization, preserving its state and history.</p>
      <div className="flex items-end gap-2">
        <div className="flex-1"><Field label="Destination organization" value={dest} onChange={setDest} placeholder="org-name" /></div>
        <div className="mb-0.5"><PrimaryButton onClick={saving ? undefined : transfer}>Transfer</PrimaryButton></div>
      </div>
      {error && <p className="mt-2 text-sm text-red-400">{error}</p>}
    </Card>
  );
}
