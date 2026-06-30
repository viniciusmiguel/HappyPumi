import { useCallback, useEffect, useState } from "react";
import { ShieldCheck } from "lucide-react";
import { api, type OidcIssuer, type RegisterOidcIssuerBody } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Badge, Card, Field, Modal, PageHeader, PrimaryButton, SecondaryButton, Table } from "../components/ui";

// Settings → OIDC issuers (PR3): register / list / regenerate-thumbprints / delete trusted OIDC issuers used
// for token exchange in CI/CD. Backed by the real /api/orgs/{org}/oidc/issuers spec endpoints.
export default function OidcIssuers() {
  const org = useOrg();
  const [issuers, setIssuers] = useState<OidcIssuer[]>([]);
  const [registering, setRegistering] = useState(false);

  const reload = useCallback(() => {
    api.oidcIssuers(org).then((r) => setIssuers(r.oidcIssuers ?? []));
  }, [org]);
  useEffect(() => { reload(); }, [reload]);

  const regenerate = async (id?: string) => {
    if (!id) return;
    await api.regenerateOidcThumbprints(org, id);
    reload();
  };

  const remove = async (id?: string) => {
    if (!id) return;
    await api.deleteOidcIssuer(org, id);
    reload();
  };

  return (
    <div>
      <PageHeader icon={ShieldCheck} title="OIDC issuers" />
      <div className="mt-4">
        <Card
          title="Registered issuers"
          actions={<PrimaryButton onClick={() => setRegistering(true)}>Register issuer</PrimaryButton>}
        >
          <Table<OidcIssuer>
            rows={issuers}
            empty={<p className="px-6 py-4 text-sm text-ink-dim">No OIDC issuers yet. Register one to enable token exchange.</p>}
            columns={[
              { header: "Name", cell: (i) => <span className="font-medium">{i.name}</span> },
              { header: "URL", cell: (i) => <span className="text-ink-dim">{i.url}</span> },
              {
                header: "Thumbprints",
                cell: (i) => <Badge tone={i.thumbprints?.length ? "success" : "default"}>{i.thumbprints?.length ?? 0}</Badge>,
              },
              { header: "Created", cell: (i) => <span className="text-ink-dim">{formatDate(i.created)}</span> },
              {
                header: "", className: "text-right",
                cell: (i) => (
                  <span className="flex justify-end gap-3">
                    <button onClick={() => regenerate(i.id)} className="text-brand hover:underline">Regenerate thumbprints</button>
                    <button onClick={() => remove(i.id)} className="text-red-400 hover:text-red-300">Delete</button>
                  </span>
                ),
              },
            ]}
          />
          {registering && (
            <RegisterIssuerModal
              org={org}
              onClose={() => setRegistering(false)}
              onRegistered={() => { setRegistering(false); reload(); }}
            />
          )}
        </Card>
      </div>
    </div>
  );
}

function RegisterIssuerModal({ org, onClose, onRegistered }: {
  org: string; onClose: () => void; onRegistered: () => void;
}) {
  const [name, setName] = useState("");
  const [url, setUrl] = useState("");
  const [maxExpiration, setMaxExpiration] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const valid = name.trim().length > 0 && url.trim().length > 0;

  const save = async () => {
    if (!valid) return;
    setSaving(true);
    setError(null);
    try {
      const body: RegisterOidcIssuerBody = { name, url };
      const seconds = Number.parseInt(maxExpiration, 10);
      if (Number.isFinite(seconds) && seconds > 0) body.maxExpiration = seconds;
      await api.registerOidcIssuer(org, body);
      onRegistered();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title="Register OIDC issuer"
      onClose={onClose}
      footer={<>
        <SecondaryButton onClick={onClose}>Cancel</SecondaryButton>
        <PrimaryButton onClick={saving || !valid ? undefined : save}>Register</PrimaryButton>
      </>}
    >
      <Field label="Name" value={name} onChange={setName} placeholder="github-actions" />
      <Field label="Issuer URL" value={url} onChange={setUrl} placeholder="https://token.actions.githubusercontent.com" />
      <Field label="Max expiration (seconds, optional)" value={maxExpiration} onChange={setMaxExpiration} placeholder="3600" />
      {error && <p className="text-xs text-red-400">{error}</p>}
    </Modal>
  );
}

function formatDate(iso?: string): string {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? "—" : d.toLocaleDateString();
}
