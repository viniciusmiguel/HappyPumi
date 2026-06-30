import { useCallback, useEffect, useState } from "react";
import { api, type EnvWebhook, type EnvWebhookInput, type WebhookDeliveryLog } from "../../lib/api";
import { Badge, Card, Field, Modal, PrimaryButton, SecondaryButton } from "../../components/ui";

type Props = { org: string; project: string; name: string };
const FORMATS = ["raw", "slack", "ms_teams", "pulumi_deployments"];

// The Webhooks tab of an ESC environment (PR3): list/add/edit/delete webhooks and inspect each webhook's
// delivery history with redeliver + ping. Backed by the project-aware /esc/environments/.../hooks endpoints;
// a real environment update fires an `env_updated` delivery.
export function WebhooksTab({ org, project, name }: Props) {
  const [hooks, setHooks] = useState<EnvWebhook[]>([]);
  const [editing, setEditing] = useState<EnvWebhook | "new" | null>(null);
  const [deliveriesFor, setDeliveriesFor] = useState<string | null>(null);

  const reload = useCallback(() => { api.environmentWebhooks(org, project, name).then(setHooks); }, [org, project, name]);
  useEffect(() => { reload(); }, [reload]);

  const remove = async (hook: string) => { await api.deleteEnvironmentWebhook(org, project, name, hook); reload(); };

  return (
    <Card title="Webhooks" actions={<SecondaryButton onClick={() => setEditing("new")}>Add webhook</SecondaryButton>}>
      {hooks.length === 0 ? (
        <p className="text-sm text-ink-dim">No webhooks. Add one to receive POSTs when this environment changes.</p>
      ) : (
        <div className="space-y-2">
          {hooks.map((h) => (
            <HookRow key={h.name} hook={h} onEdit={() => setEditing(h)} onDeliveries={() => setDeliveriesFor(h.name)}
              onPing={async () => { await api.pingEnvironmentWebhook(org, project, name, h.name); setDeliveriesFor(h.name); }}
              onDelete={() => remove(h.name)} />
          ))}
        </div>
      )}
      {editing && (
        <HookModal org={org} project={project} name={name} existing={editing === "new" ? null : editing}
          onClose={() => setEditing(null)} onSaved={async () => { setEditing(null); await reload(); }} />
      )}
      {deliveriesFor && (
        <DeliveriesModal org={org} project={project} name={name} hook={deliveriesFor} onClose={() => setDeliveriesFor(null)} />
      )}
    </Card>
  );
}

function HookRow({ hook, onEdit, onDeliveries, onPing, onDelete }: {
  hook: EnvWebhook; onEdit: () => void; onDeliveries: () => void; onPing: () => void; onDelete: () => void;
}) {
  return (
    <div className="flex items-center gap-3 rounded-md border border-line px-3 py-2 text-sm">
      <span className="font-medium">{hook.displayName || hook.name}</span>
      <span className="flex-1 truncate font-mono text-xs text-ink-dim">{hook.payloadUrl}</span>
      <Badge tone="default">{hook.format || "raw"}</Badge>
      <Badge tone={hook.active ? "success" : "warn"}>{hook.active ? "active" : "paused"}</Badge>
      <button onClick={onDeliveries} className="text-ink-dim hover:text-ink">Deliveries</button>
      <button onClick={onPing} className="text-ink-dim hover:text-ink">Ping</button>
      <button onClick={onEdit} className="text-ink-dim hover:text-ink">Edit</button>
      <button onClick={onDelete} className="text-red-400 hover:text-red-300">Delete</button>
    </div>
  );
}

function HookModal({ org, project, name, existing, onClose, onSaved }: Props & {
  existing: EnvWebhook | null; onClose: () => void; onSaved: () => void;
}) {
  const [hookName, setHookName] = useState(existing?.name ?? "");
  const [payloadUrl, setPayloadUrl] = useState(existing?.payloadUrl ?? "");
  const [format, setFormat] = useState(existing?.format ?? "raw");
  const [secret, setSecret] = useState("");
  const [active, setActive] = useState(existing?.active ?? true);
  const [saving, setSaving] = useState(false);

  const save = async () => {
    if (!hookName.trim() || !payloadUrl.trim()) return;
    setSaving(true);
    const body: EnvWebhookInput = { payloadUrl, format, active, ...(secret ? { secret } : {}) };
    try {
      if (existing) await api.updateEnvironmentWebhook(org, project, name, existing.name, body);
      else await api.createEnvironmentWebhook(org, project, name, { name: hookName, ...body });
      onSaved();
    } finally { setSaving(false); }
  };

  return (
    <Modal title={existing ? `Edit ${existing.name}` : "Add webhook"} onClose={onClose}
      footer={<><SecondaryButton onClick={onClose}>Cancel</SecondaryButton>
        <PrimaryButton onClick={saving ? undefined : save}>Save</PrimaryButton></>}>
      {!existing && <Field label="Name" value={hookName} onChange={setHookName} placeholder="ci-webhook" />}
      <Field label="Payload URL" value={payloadUrl} onChange={setPayloadUrl} placeholder="https://example.com/hook" />
      <Field label="Format" value={format} onChange={setFormat} options={FORMATS} />
      <Field label="Secret (optional)" value={secret} onChange={setSecret} placeholder={existing?.hasSecret ? "•••• (unchanged)" : "signing secret"} />
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" checked={active} onChange={(e) => setActive(e.target.checked)} className="accent-brand" />
        <span>Active</span>
      </label>
    </Modal>
  );
}

function DeliveriesModal({ org, project, name, hook, onClose }: Props & { hook: string; onClose: () => void }) {
  const [deliveries, setDeliveries] = useState<WebhookDeliveryLog[]>([]);
  const reload = useCallback(() => { api.environmentWebhookDeliveries(org, project, name, hook).then(setDeliveries); }, [org, project, name, hook]);
  useEffect(() => { reload(); }, [reload]);

  const redeliver = async (event: string) => { await api.redeliverEnvironmentWebhookEvent(org, project, name, hook, event); reload(); };

  return (
    <Modal title={`Deliveries — ${hook}`} onClose={onClose}>
      {deliveries.length === 0 ? (
        <p className="text-sm text-ink-dim">No deliveries yet. Ping the webhook or update the environment.</p>
      ) : (
        <div className="space-y-2">
          {deliveries.map((d) => (
            <div key={d.id} className="flex items-center gap-3 rounded-md border border-line px-3 py-2 text-sm">
              <span className="font-mono text-xs">{d.kind}</span>
              <Badge tone={d.responseCode && d.responseCode >= 200 && d.responseCode < 300 ? "success" : "danger"}>{d.responseCode || 0}</Badge>
              <span className="flex-1" />
              <button onClick={() => redeliver(d.kind)} className="text-ink-dim hover:text-ink">Redeliver</button>
            </div>
          ))}
        </div>
      )}
    </Modal>
  );
}
