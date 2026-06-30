import { useCallback, useEffect, useState } from "react";
import { Lock } from "lucide-react";
import { api, type OrgKey } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Badge, Card, Field, Modal, PageHeader, PrimaryButton, SecondaryButton, Table } from "../components/ui";

// Settings → Encryption keys (PR4): list / add / set-default / disable customer-managed keys (BYOK). A key
// becomes the org default when created or promoted; disable-all reverts the org to service-managed encryption.
// Backed by /api/orgs/{org}/cmk.
export default function EncryptionKeys() {
  const org = useOrg();
  const [keys, setKeys] = useState<OrgKey[]>([]);
  const [adding, setAdding] = useState(false);

  const reload = useCallback(() => { api.orgKeys(org).then(setKeys); }, [org]);
  useEffect(() => { reload(); }, [reload]);

  const setDefault = async (id: string) => { await api.setDefaultOrgKey(org, id); reload(); };
  const disable = async (id: string) => { await api.disableOrgKey(org, id); reload(); };
  const disableAll = async () => { await api.disableAllOrgKeys(org); reload(); };

  return (
    <div>
      <PageHeader icon={Lock} title="Encryption keys" />
      <Card
        title="Customer-managed keys"
        actions={<div className="flex gap-2">
          <SecondaryButton onClick={disableAll}>Disable all</SecondaryButton>
          <PrimaryButton onClick={() => setAdding(true)}>Add AWS KMS key</PrimaryButton>
        </div>}
      >
        <Table<OrgKey>
          rows={keys}
          empty={<p className="px-6 py-4 text-sm text-ink-dim">No customer-managed keys. Secrets use service-managed encryption.</p>}
          columns={[
            { header: "Name", cell: (k) => <span className="font-medium">{k.name || "(unnamed)"}</span> },
            { header: "Type", cell: (k) => <span className="text-ink-dim">{k.keyType}</span> },
            { header: "State", cell: (k) => <Badge tone={stateTone(k.state)}>{k.state ?? "active"}</Badge> },
            { header: "AWS KMS ARN", cell: (k) => <code className="break-all font-mono text-xs text-ink-dim">{k.awsKms?.keyArn ?? "—"}</code> },
            {
              header: "", className: "text-right",
              cell: (k) => <RowActions
                onSetDefault={k.state === "default" ? undefined : () => setDefault(k.id)}
                onDisable={k.state === "disabled" ? undefined : () => disable(k.id)}
              />,
            },
          ]}
        />
        {adding && (
          <AddKeyModal
            org={org}
            onClose={() => setAdding(false)}
            onAdded={() => { setAdding(false); reload(); }}
          />
        )}
      </Card>
    </div>
  );
}

function RowActions({ onSetDefault, onDisable }: { onSetDefault?: () => void; onDisable?: () => void }) {
  return (
    <div className="flex justify-end gap-3">
      {onSetDefault && <button onClick={onSetDefault} className="text-brand hover:underline">Set default</button>}
      {onDisable && <button onClick={onDisable} className="text-red-400 hover:text-red-300">Disable</button>}
    </div>
  );
}

function AddKeyModal({ org, onClose, onAdded }: { org: string; onClose: () => void; onAdded: () => void }) {
  const [name, setName] = useState("");
  const [keyArn, setKeyArn] = useState("");
  const [roleArn, setRoleArn] = useState("");
  const [saving, setSaving] = useState(false);

  const valid = name.trim().length > 0 && keyArn.trim().length > 0;

  const save = async () => {
    if (!valid) return;
    setSaving(true);
    try {
      await api.createOrgKey(org, { name, keyType: "aws-kms", awsKms: { keyArn, roleArn } });
      onAdded();
    } finally { setSaving(false); }
  };

  return (
    <Modal
      title="Add AWS KMS key"
      onClose={onClose}
      footer={<>
        <SecondaryButton onClick={onClose}>Cancel</SecondaryButton>
        <PrimaryButton onClick={saving || !valid ? undefined : save}>Add key</PrimaryButton>
      </>}
    >
      <Field label="Name" value={name} onChange={setName} placeholder="production-kms" />
      <Field label="Key ARN" value={keyArn} onChange={setKeyArn} placeholder="arn:aws:kms:us-east-1:123:key/abc" />
      <Field label="Role ARN" value={roleArn} onChange={setRoleArn} placeholder="arn:aws:iam::123:role/pulumi-kms" />
    </Modal>
  );
}

function stateTone(state?: string): "default" | "brand" | "success" | "warn" | "danger" {
  if (state === "default") return "success";
  if (state === "disabled") return "danger";
  return "brand";
}
