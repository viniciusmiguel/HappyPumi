import { useCallback, useEffect, useState } from "react";
import { Key } from "lucide-react";
import { api, type AccessToken, type CreatedAccessToken } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Card, Field, Modal, PageHeader, PrimaryButton, SecondaryButton, Table, Tabs } from "../components/ui";

type Scope = "personal" | "org";

// Settings → Access tokens (PR1): issue / list / revoke personal and organization tokens. The token value is
// shown ONCE in a dialog at creation and never returned again (the API stores only its hash), so the list
// shows metadata only. Backed by /api/user/tokens and /api/orgs/{org}/tokens.
export default function AccessTokens() {
  const org = useOrg();
  const [scope, setScope] = useState<Scope>("personal");

  return (
    <div>
      <PageHeader icon={Key} title="Access tokens" />
      <Tabs
        active={scope}
        onChange={(k) => setScope(k as Scope)}
        tabs={[{ key: "personal", label: "Personal" }, { key: "org", label: "Organization" }]}
      />
      <div className="mt-4">
        {scope === "personal"
          ? <TokenSection key="personal" scope="personal" org={org} />
          : <TokenSection key="org" scope="org" org={org} />}
      </div>
    </div>
  );
}

function TokenSection({ scope, org }: { scope: Scope; org: string }) {
  const [tokens, setTokens] = useState<AccessToken[]>([]);
  const [creating, setCreating] = useState(false);
  const [issued, setIssued] = useState<CreatedAccessToken | null>(null);

  const reload = useCallback(() => {
    (scope === "personal" ? api.personalTokens() : api.orgTokens(org)).then(setTokens);
  }, [scope, org]);
  useEffect(() => { reload(); }, [reload]);

  const remove = async (id: string) => {
    await (scope === "personal" ? api.deletePersonalToken(id) : api.deleteOrgToken(org, id));
    reload();
  };

  const title = scope === "personal" ? "Personal access tokens" : `${org} organization tokens`;
  return (
    <Card
      title={title}
      actions={<PrimaryButton onClick={() => setCreating(true)}>Create token</PrimaryButton>}
    >
      <Table<AccessToken>
        rows={tokens}
        empty={<p className="px-6 py-4 text-sm text-ink-dim">No tokens yet. Create one for CLI or CI/CD access.</p>}
        columns={[
          { header: "Name", cell: (t) => <span className="font-medium">{t.name || t.description || "(unnamed)"}</span> },
          { header: "Created by", cell: (t) => <span className="text-ink-dim">{t.createdBy || "—"}</span> },
          { header: "Created", cell: (t) => <span className="text-ink-dim">{formatDate(t.created)}</span> },
          { header: "Last used", cell: (t) => <span className="text-ink-dim">{formatEpoch(t.lastUsed)}</span> },
          {
            header: "", className: "text-right",
            cell: (t) => <button onClick={() => remove(t.id)} className="text-red-400 hover:text-red-300">Revoke</button>,
          },
        ]}
      />
      {creating && (
        <CreateTokenModal
          scope={scope}
          org={org}
          onClose={() => setCreating(false)}
          onCreated={(c) => { setCreating(false); setIssued(c); reload(); }}
        />
      )}
      {issued && <IssuedTokenModal created={issued} onClose={() => setIssued(null)} />}
    </Card>
  );
}

function CreateTokenModal({ scope, org, onClose, onCreated }: {
  scope: Scope; org: string; onClose: () => void; onCreated: (c: CreatedAccessToken) => void;
}) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [saving, setSaving] = useState(false);

  // Personal tokens carry only a description on the wire; org tokens carry a required name.
  const valid = scope === "personal" ? description.trim().length > 0 : name.trim().length > 0;

  const save = async () => {
    if (!valid) return;
    setSaving(true);
    try {
      const created = scope === "personal"
        ? await api.createPersonalToken(description)
        : await api.createOrgToken(org, name, description);
      onCreated(created);
    } finally { setSaving(false); }
  };

  return (
    <Modal
      title={scope === "personal" ? "Create personal token" : "Create organization token"}
      onClose={onClose}
      footer={<>
        <SecondaryButton onClick={onClose}>Cancel</SecondaryButton>
        <PrimaryButton onClick={saving || !valid ? undefined : save}>Create</PrimaryButton>
      </>}
    >
      {scope === "org" && <Field label="Name" value={name} onChange={setName} placeholder="ci-pipeline" />}
      <Field label="Description" value={description} onChange={setDescription} placeholder="What is this token for?" />
    </Modal>
  );
}

function IssuedTokenModal({ created, onClose }: { created: CreatedAccessToken; onClose: () => void }) {
  const [copied, setCopied] = useState(false);
  const copy = async () => {
    await navigator.clipboard.writeText(created.tokenValue);
    setCopied(true);
  };
  return (
    <Modal
      title="Token created"
      onClose={onClose}
      footer={<PrimaryButton onClick={onClose}>Done</PrimaryButton>}
    >
      <p className="text-sm text-ink-dim">
        Copy this token now — it is shown only once and cannot be retrieved later.
      </p>
      <div className="mt-3 flex items-center gap-2">
        <code className="flex-1 break-all rounded-md border border-line bg-bg px-3 py-2 font-mono text-xs">{created.tokenValue}</code>
        <SecondaryButton onClick={copy}>{copied ? "Copied" : "Copy"}</SecondaryButton>
      </div>
    </Modal>
  );
}

function formatDate(iso?: string): string {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? "—" : d.toLocaleDateString();
}

function formatEpoch(seconds?: number): string {
  if (!seconds) return "Never";
  return new Date(seconds * 1000).toLocaleDateString();
}
