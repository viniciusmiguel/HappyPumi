import { useEffect, useState } from "react";
import { GitBranch, Plus, Trash2, FolderGit2 } from "lucide-react";
import { api, type VcsIntegrationSummary } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton, SecondaryButton, Modal, Badge, Avatar } from "../components/ui";
import { ConnectModal } from "./vcs/ConnectModal";
import { RepoBrowser } from "./vcs/RepoBrowser";

// Maps the API's vcsProvider value to a human label for the table.
const PROVIDER_LABELS: Record<string, string> = {
  github: "GitHub",
  "github-enterprise": "GitHub Enterprise",
  "azure-devops": "Azure DevOps",
  gitlab: "GitLab",
  bitbucket: "Bitbucket",
};

function label(provider: string): string {
  return PROVIDER_LABELS[provider] ?? provider;
}

export default function VersionControl() {
  const org = useOrg();
  const [integrations, setIntegrations] = useState<VcsIntegrationSummary[]>([]);
  const [pending, setPending] = useState<VcsIntegrationSummary | null>(null);
  const [browsing, setBrowsing] = useState<VcsIntegrationSummary | null>(null);
  const [showConnect, setShowConnect] = useState(false);

  function load() { api.vcsIntegrations(org).then((r) => setIntegrations(r.integrations ?? [])); }
  useEffect(() => { load(); }, [org]);

  async function disconnect() {
    if (!pending) return;
    await api.deleteVcsIntegration(org, pending.vcsProvider, pending.id);
    setPending(null);
    load();
  }

  return (
    <div>
      <PageHeader icon={GitBranch} title="Version control"
        actions={<PrimaryButton icon={Plus} onClick={() => setShowConnect(true)}>Connect account</PrimaryButton>} />
      <Table
        rows={integrations}
        columns={[
          {
            header: "Account",
            cell: (i) => (
              <span className="flex items-center gap-2 font-medium">
                <Avatar name={i.name ?? i.vcsProvider} /> {i.name ?? "—"}
              </span>
            ),
          },
          { header: "Provider", cell: (i) => <Badge tone="brand">{label(i.vcsProvider)}</Badge> },
          { header: "Host", cell: (i) => <span className="text-ink-faint">{i.host ?? i.baseUrl ?? "—"}</span> },
          {
            header: "",
            cell: (i) => (
              <span className="flex justify-end gap-2">
                <SecondaryButton icon={FolderGit2} onClick={() => setBrowsing(i)}>Browse</SecondaryButton>
                <SecondaryButton icon={Trash2} onClick={() => setPending(i)}>Disconnect</SecondaryButton>
              </span>
            ),
          },
        ]}
        empty={<EmptyState icon={GitBranch} title="Connect your version control system"
          description="Combine Pulumi with your VCS to enable pull request comments, policy enforcement, and drift detection."
          action={<PrimaryButton icon={Plus} onClick={() => setShowConnect(true)}>Connect account</PrimaryButton>} />}
      />
      {pending && (
        <Modal title="Disconnect integration" onClose={() => setPending(null)}
          footer={<><SecondaryButton onClick={() => setPending(null)}>Cancel</SecondaryButton><PrimaryButton onClick={disconnect}>Disconnect</PrimaryButton></>}>
          <p className="text-sm text-ink-faint">
            Disconnect the {label(pending.vcsProvider)} integration {pending.name ? `for ${pending.name}` : ""}? This removes the
            stored connection from this organization.
          </p>
        </Modal>
      )}
      {browsing && <RepoBrowser org={org} integration={browsing} onClose={() => setBrowsing(null)} />}
      {showConnect && (
        <ConnectModal org={org} onClose={() => setShowConnect(false)}
          onDone={() => { setShowConnect(false); load(); }} />
      )}
    </div>
  );
}
