import { useCallback, useEffect, useState } from "react";
import { Cloud } from "lucide-react";
import { api, type CloudAccount, type CloudProvider } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Badge, Card, PageHeader, PrimaryButton, Table } from "../components/ui";

// Settings → Cloud accounts (PR6): connect AWS / Azure / GCP via the ESC cloud-setup OAuth flow and list the
// accounts/subscriptions/projects discovered for each. Backed by the real /api/esc/cloudsetup/* endpoints.
interface ProviderCard { key: CloudProvider; label: string; load: (org: string) => Promise<{ accounts?: CloudAccount[] }>; }

const providers: ProviderCard[] = [
  { key: "aws", label: "AWS", load: (org) => api.awsSsoAccounts(org) },
  { key: "azure", label: "Azure", load: (org) => api.azureAccounts(org) },
  { key: "gcp", label: "GCP", load: (org) => api.gcpAccounts(org) },
];

export default function CloudAccounts() {
  const org = useOrg();
  return (
    <div>
      <PageHeader icon={Cloud} title="Cloud accounts" />
      <div className="mt-4 grid gap-4">
        {providers.map((p) => <ProviderPanel key={p.key} org={org} provider={p} />)}
      </div>
    </div>
  );
}

function ProviderPanel({ org, provider }: { org: string; provider: ProviderCard }) {
  const [accounts, setAccounts] = useState<CloudAccount[]>([]);
  const [status, setStatus] = useState<string | null>(null);

  const reload = useCallback(() => {
    provider.load(org).then((r) => setAccounts(r.accounts ?? []));
  }, [org, provider]);
  useEffect(() => { reload(); }, [reload]);

  const connect = async () => {
    setStatus(null);
    const { url } = await api.initiateCloudOAuth(org, provider.key);
    if (!url) {
      setStatus(`${provider.label} OAuth is not configured on this server.`);
      return;
    }
    window.open(url, "_blank", "noopener");
  };

  return (
    <Card title={provider.label} actions={<PrimaryButton onClick={connect}>Connect {provider.label}</PrimaryButton>}>
      {status && <p className="px-6 pt-3 text-sm text-ink-dim">{status}</p>}
      <Table<CloudAccount>
        rows={accounts}
        empty={<p className="px-6 py-4 text-sm text-ink-dim">No {provider.label} accounts connected yet.</p>}
        columns={[
          { header: "ID", cell: (a) => <span className="font-medium">{a.id}</span> },
          { header: "Name", cell: (a) => <span className="text-ink-dim">{a.name}</span> },
          {
            header: "Roles",
            cell: (a) => (a.roles?.length
              ? <span className="flex flex-wrap gap-1">{a.roles.map((r) => <Badge key={r}>{r}</Badge>)}</span>
              : <span className="text-ink-dim">—</span>),
          },
        ]}
      />
    </Card>
  );
}
