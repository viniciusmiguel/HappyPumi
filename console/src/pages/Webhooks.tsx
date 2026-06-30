import { useCallback, useEffect, useState } from "react";
import { Webhook as WebhookIcon } from "lucide-react";
import { api, type StackWebhook, type StackWebhookInput, type WebhookDeliveryLog } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Badge, Card, Field, Modal, PageHeader, PrimaryButton, SecondaryButton } from "../components/ui";

const FORMATS = ["raw", "slack", "ms_teams", "pulumi_deployments"];

// The org-settings Webhooks page (PR2): list/add/edit/delete organization webhooks and inspect each webhook's
// delivery history with redeliver + ping. Org webhooks receive org-wide activity (e.g. stack updates). Mirrors
// the stack Settings webhooks section, backed by the /api/orgs/{org}/hooks endpoints.
export default function Webhooks() {
  const org = useOrg();
  const [hooks, setHooks] = useState<StackWebhook[]>([]);
  const [editing, setEditing] = useState<StackWebhook | "new" | null>(null);
  const [deliveriesFor, setDeliveriesFor] = useState<string | null>(null);

  const reload = useCallback(() => { api.orgWebhooks(org).then(setHooks); }, [org]);
  useEffect(() => { reload(); }, [reload]);

  const remove = async (name: string) => { await api.deleteOrgWebhook(org, name); reload(); };

  return (
    <div>
      <PageHeader icon={WebhookIcon} title="Webhooks"
        actions={<PrimaryButton onClick={() => setEditing("new")}>Add webhook</PrimaryButton>} />
      <Card title="Organization webhooks">
        {hooks.length === 0 ? (
          <p className="text-sm text-ink-dim">No webhooks. Add one to receive POSTs on organization-wide stack and deployment events.</p>
        ) : (
          <div className="space-y-2">
            {hooks.map((h) => (
              <HookRow key={h.name} hook={h} onEdit={() => setEditing(h)} onDeliveries={() => setDeliveriesFor(h.name)}
                onPing={async () => { await api.pingOrgWebhook(org, h.name); setDeliveriesFor(h.name); }}
                onDelete={() => remove(h.name)} />
            ))}
          </div>
        )}
      </Card>
      {editing && (
        <HookModal org={org} existing={editing === "new" ? null : editing}
          onClose={() => setEditing(null)} onSaved={async () => { setEditing(null); await reload(); }} />
      )}
      {deliveriesFor && (
        <DeliveriesModal org={org} name={deliveriesFor} onClose={() => setDeliveriesFor(null)} />
      )}
    </div>
  );
}

function HookRow({ hook, onEdit, onDeliveries, onPing, onDelete }: {
  hook: StackWebhook; onEdit: () => void; onDeliveries: () => void; onPing: () => void; onDelete: () => void;
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

function HookModal({ org, existing, onClose, onSaved }: {
  org: string; existing: StackWebhook | null; onClose: () => void; onSaved: () => void;
}) {
  const [name, setName] = useState(existing?.name ?? "");
  const [payloadUrl, setPayloadUrl] = useState(existing?.payloadUrl ?? "");
  const [format, setFormat] = useState(existing?.format ?? "raw");
  const [secret, setSecret] = useState("");
  const [active, setActive] = useState(existing?.active ?? true);
  const [saving, setSaving] = useState(false);

  const save = async () => {
    if (!name.trim() || !payloadUrl.trim()) return;
    setSaving(true);
    const body: StackWebhookInput = { payloadUrl, format, active, ...(secret ? { secret } : {}) };
    try {
      if (existing) await api.updateOrgWebhook(org, existing.name, body);
      else await api.createOrgWebhook(org, { name, ...body });
      onSaved();
    } finally { setSaving(false); }
  };

  return (
    <Modal title={existing ? `Edit ${existing.name}` : "Add webhook"} onClose={onClose}
      footer={<><SecondaryButton onClick={onClose}>Cancel</SecondaryButton>
        <PrimaryButton onClick={saving ? undefined : save}>Save</PrimaryButton></>}>
      {!existing && <Field label="Name" value={name} onChange={setName} placeholder="ci-webhook" />}
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

function DeliveriesModal({ org, name, onClose }: { org: string; name: string; onClose: () => void }) {
  const [deliveries, setDeliveries] = useState<WebhookDeliveryLog[]>([]);
  const reload = useCallback(() => { api.orgWebhookDeliveries(org, name).then(setDeliveries); }, [org, name]);
  useEffect(() => { reload(); }, [reload]);

  const redeliver = async (event: string) => { await api.redeliverOrgWebhookEvent(org, name, event); reload(); };

  return (
    <Modal title={`Deliveries — ${name}`} onClose={onClose}>
      {deliveries.length === 0 ? (
        <p className="text-sm text-ink-dim">No deliveries yet. Ping the webhook or trigger an event.</p>
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
