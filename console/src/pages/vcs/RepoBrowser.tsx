import { useEffect, useState } from "react";
import { api, type VcsIntegrationSummary, type VcsRepo, type VcsBranch } from "../../lib/api";
import { Modal, SecondaryButton, Table, Badge } from "../../components/ui";

// Per-integration repo browser: lists the integration's repos and, for a selected repo, its branches.
// Both calls dispatch server-side to the provider for the integration's kind (empty when unconfigured).
export function RepoBrowser({ org, integration, onClose }: {
  org: string; integration: VcsIntegrationSummary; onClose: () => void;
}) {
  const [repos, setRepos] = useState<VcsRepo[]>([]);
  const [selected, setSelected] = useState<VcsRepo | null>(null);
  const [branches, setBranches] = useState<VcsBranch[]>([]);

  useEffect(() => {
    api.vcsRepos(org, integration.vcsProvider, integration.id).then((r) => setRepos(r.repos ?? []));
  }, [org, integration]);

  useEffect(() => {
    if (!selected) return;
    api.vcsBranches(org, integration.vcsProvider, integration.id, selected.id).then((r) => setBranches(r.branches ?? []));
  }, [org, integration, selected]);

  return (
    <Modal title={`Repositories — ${integration.name ?? integration.vcsProvider}`} onClose={onClose}
      footer={<SecondaryButton onClick={onClose}>Close</SecondaryButton>}>
      {selected ? (
        <div className="space-y-3">
          <SecondaryButton onClick={() => setSelected(null)}>Back to repositories</SecondaryButton>
          <Table
            rows={branches}
            columns={[
              { header: "Branch", cell: (b) => <span className="font-medium">{b.name}</span> },
              { header: "", cell: (b) => b.isProtected ? <Badge tone="warn">protected</Badge> : null },
            ]}
            empty={<p className="p-4 text-sm text-ink-faint">No branches found for {selected.name}.</p>}
          />
        </div>
      ) : (
        <Table
          rows={repos}
          columns={[
            { header: "Repository", cell: (r) => <span className="font-medium">{r.name}</span> },
            { header: "Owner", cell: (r) => <span className="text-ink-faint">{r.owner}</span> },
          ]}
          onRowClick={(r) => setSelected(r)}
          empty={<p className="p-4 text-sm text-ink-faint">
            No repositories — connect the account's access (OAuth/token) to browse repositories.
          </p>}
        />
      )}
    </Modal>
  );
}
