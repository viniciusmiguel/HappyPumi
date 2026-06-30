import { useCallback, useEffect, useState } from "react";
import { Fingerprint } from "lucide-react";
import { api, type SamlOrganization, type SamlUserInfo } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Badge, Card, Field, KeyValue, PageHeader, PrimaryButton, Table } from "../components/ui";

// Settings → SAML / SSO (PR5): view and update the org's SAML configuration by pasting the IdP metadata XML,
// and manage the SAML admins. Backed by the real /api/orgs/{org}/saml spec endpoints.
export default function SamlSso() {
  const org = useOrg();
  const [config, setConfig] = useState<SamlOrganization | null>(null);
  const [admins, setAdmins] = useState<SamlUserInfo[]>([]);

  const reload = useCallback(() => {
    api.samlOrg(org).then(setConfig);
    api.samlAdmins(org).then((r) => setAdmins(r.samlAdmins ?? []));
  }, [org]);
  useEffect(() => { reload(); }, [reload]);

  return (
    <div>
      <PageHeader icon={Fingerprint} title="SAML / SSO" />
      <div className="mt-4 space-y-4">
        <ConfigCard org={org} config={config} onSaved={reload} />
        <AdminsCard org={org} admins={admins} onAdded={reload} />
      </div>
    </div>
  );
}

function ConfigCard({ org, config, onSaved }: {
  org: string; config: SamlOrganization | null; onSaved: () => void;
}) {
  const [xml, setXml] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const save = async () => {
    if (xml.trim().length === 0) return;
    setSaving(true);
    setError(null);
    try {
      await api.updateSamlOrg(org, xml);
      onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Card title="SAML configuration">
      <div className="px-6 py-4 space-y-3">
        <KeyValue label="Entity ID">{config?.entityId ?? "—"}</KeyValue>
        <KeyValue label="SSO URL">{config?.ssoUrl ?? "—"}</KeyValue>
        <KeyValue label="NameID format">{config?.nameIdFormat ?? "—"}</KeyValue>
        <KeyValue label="Valid until">{config?.validUntil ?? "—"}</KeyValue>
        <KeyValue label="Status">
          {config?.validationError
            ? <Badge tone="danger">{config.validationError}</Badge>
            : <Badge tone={config?.entityId ? "success" : "default"}>{config?.entityId ? "Configured" : "Not configured"}</Badge>}
        </KeyValue>
        <label className="block text-sm text-ink-dim">IdP metadata XML</label>
        <textarea
          value={xml}
          onChange={(e) => setXml(e.target.value)}
          placeholder="<EntityDescriptor …>…</EntityDescriptor>"
          rows={8}
          className="w-full rounded border border-line bg-surface px-3 py-2 font-mono text-xs text-ink"
        />
        {error && <p className="text-xs text-red-400">{error}</p>}
        <PrimaryButton onClick={saving ? undefined : save}>Save configuration</PrimaryButton>
      </div>
    </Card>
  );
}

function AdminsCard({ org, admins, onAdded }: {
  org: string; admins: SamlUserInfo[]; onAdded: () => void;
}) {
  const [login, setLogin] = useState("");
  const [error, setError] = useState<string | null>(null);

  const add = async () => {
    if (login.trim().length === 0) return;
    setError(null);
    try {
      await api.addSamlAdmin(org, login.trim());
      setLogin("");
      onAdded();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    }
  };

  return (
    <Card title="SAML admins" actions={<PrimaryButton onClick={add}>Add admin</PrimaryButton>}>
      <div className="px-6 pt-4">
        <Field label="User login" value={login} onChange={setLogin} placeholder="alice" />
        {error && <p className="text-xs text-red-400">{error}</p>}
      </div>
      <Table<SamlUserInfo>
        rows={admins}
        empty={<p className="px-6 py-4 text-sm text-ink-dim">No SAML admins yet.</p>}
        columns={[
          { header: "Login", cell: (u) => <span className="font-medium">{u.githubLogin}</span> },
          { header: "Name", cell: (u) => <span className="text-ink-dim">{u.name ?? "—"}</span> },
          { header: "Email", cell: (u) => <span className="text-ink-dim">{u.email ?? "—"}</span> },
        ]}
      />
    </Card>
  );
}
