import { useEffect, useState } from "react";
import { Fingerprint, Plus } from "lucide-react";
import { api, timeAgo, type OidcIssuer } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton, SecondaryButton, Modal, Field } from "../components/ui";

export default function Identity() {
  const org = useOrg();
  const [issuers, setIssuers] = useState<OidcIssuer[]>([]);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ name: "", url: "" });
  const [error, setError] = useState<string | null>(null);

  function load() { api.oidcIssuers(org).then((r) => setIssuers(r.issuers ?? [])); }
  useEffect(() => { load(); }, [org]);

  async function create() {
    if (!form.name || !form.url) return;
    setError(null);
    try { await api.createOidcIssuer(org, form.name, form.url); setShowNew(false); setForm({ name: "", url: "" }); load(); }
    catch (e) { setError(e instanceof Error ? e.message : String(e)); }
  }

  return (
    <div>
      <PageHeader icon={Fingerprint} title="Identity providers"
        actions={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>Add issuer</PrimaryButton>} />
      <Table
        rows={issuers}
        columns={[
          { header: "Issuer", cell: (i) => <span className="font-medium">{i.name}</span> },
          { header: "URL", cell: (i) => <span className="text-ink-dim">{i.url}</span> },
          { header: "Added", cell: (i) => <span className="text-ink-faint">{timeAgo(i.created)}</span> },
        ]}
        empty={<EmptyState icon={Fingerprint} title="No identity providers"
          description="Configure OIDC issuers for single sign-on and token exchange with your CI/CD."
          action={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>Add issuer</PrimaryButton>} />}
      />
      {showNew && (
        <Modal title="Add an OIDC issuer" onClose={() => setShowNew(false)}
          footer={<><SecondaryButton onClick={() => setShowNew(false)}>Cancel</SecondaryButton><PrimaryButton onClick={create}>Add</PrimaryButton></>}>
          <Field label="Name" value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} placeholder="github-actions" />
          <Field label="Issuer URL" value={form.url} onChange={(v) => setForm((f) => ({ ...f, url: v }))} placeholder="https://token.actions.githubusercontent.com" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}
