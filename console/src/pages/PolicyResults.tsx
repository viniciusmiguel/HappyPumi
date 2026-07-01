import { useCallback, useEffect, useState } from "react";
import { ShieldCheck } from "lucide-react";
import {
  api,
  type PolicyComplianceRow,
  type PolicyResultsMetadata,
} from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Badge, Card, PageHeader, PrimaryButton, Table } from "../components/ui";

// Policy results (policy-results PR2): metadata cards (policies/resources with-issues vs total) + a per-policy
// compliance table + a CSV export button. All figures are aggregated server-side from the recorded findings.
export default function PolicyResults() {
  const org = useOrg();
  const [meta, setMeta] = useState<PolicyResultsMetadata | null>(null);
  const [rows, setRows] = useState<PolicyComplianceRow[]>([]);

  const reload = useCallback(() => {
    api.policyResultsMetadata(org).then(setMeta);
    api.policiesCompliance(org, {}).then((r) => setRows(r.policies ?? []));
  }, [org]);
  useEffect(() => { reload(); }, [reload]);

  const exportCsv = async () => {
    const csv = await api.exportPolicyIssues(org, {});
    downloadCsv(csv, `policy-issues-${org}.csv`);
  };

  return (
    <div>
      <PageHeader
        icon={ShieldCheck}
        title="Policy results"
        actions={<PrimaryButton onClick={exportCsv}>Export issues (CSV)</PrimaryButton>}
      />

      <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <MetricCard
          title="Policies with issues"
          value={meta?.policyWithIssuesCount ?? 0}
          total={meta?.policyTotalCount ?? 0}
        />
        <MetricCard
          title="Resources with issues"
          value={meta?.resourcesWithIssuesCount ?? 0}
          total={meta?.resourcesTotalCount ?? 0}
        />
      </div>

      <div className="mt-4">
        <Card title="Policy compliance">
          <Table<PolicyComplianceRow>
            rows={rows}
            empty={<p className="px-6 py-4 text-sm text-ink-dim">No policy issues. Run an update with a policy pack enabled to populate this view.</p>}
            columns={[
              { header: "Policy", cell: (r) => <span className="font-medium">{r.policyName}</span> },
              { header: "Policy pack", cell: (r) => <span className="text-ink-dim">{r.policyPack}</span> },
              { header: "Severity", cell: (r) => <Badge tone={severityTone(r.severity)}>{r.severity || "—"}</Badge> },
              { header: "Failing resources", cell: (r) => <span>{r.failingResources}</span> },
            ]}
          />
        </Card>
      </div>
    </div>
  );
}

function MetricCard({ title, value, total }: { title: string; value: number; total: number }) {
  return (
    <Card title={title}>
      <div className="px-6 py-4">
        <span className="text-3xl font-semibold">{value}</span>
        <span className="ml-2 text-sm text-ink-dim">of {total}</span>
      </div>
    </Card>
  );
}

function severityTone(severity: string): "default" | "warn" | "danger" {
  if (severity === "mandatory") return "danger";
  if (severity === "advisory") return "warn";
  return "default";
}

// Trigger a client-side download of the exported CSV text.
function downloadCsv(csv: string, filename: string) {
  const blob = new Blob([csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}
