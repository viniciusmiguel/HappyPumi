import { useEffect, useState } from "react";
import { ShieldAlert } from "lucide-react";
import { api, timeAgo, type PolicyViolation } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, Badge } from "../components/ui";

// Maps a policy enforcement level to a badge tone (mandatory blocks, advisory warns, disabled is neutral).
function levelTone(level: string): "danger" | "warn" | "default" {
  if (level === "mandatory") return "danger";
  if (level === "advisory") return "warn";
  return "default";
}

export default function PolicyFindings() {
  const org = useOrg();
  const [findings, setFindings] = useState<PolicyViolation[]>([]);

  useEffect(() => { api.policyViolations(org).then((r) => setFindings(r.policyViolations ?? [])); }, [org]);

  return (
    <div>
      <PageHeader icon={ShieldAlert} title="Policy findings" />
      <Table
        rows={findings}
        columns={[
          { header: "Policy", cell: (f) => (
            <div>
              <span className="font-medium">{f.policyName}</span>
              <div className="text-xs text-ink-faint">{f.policyPack}{f.policyPackTag ? `@${f.policyPackTag}` : ""}</div>
            </div>
          ) },
          { header: "Level", cell: (f) => <Badge tone={levelTone(f.level)}>{f.level}</Badge> },
          { header: "Resource", cell: (f) => (
            <div>
              <span>{f.resourceName || "—"}</span>
              <div className="text-xs text-ink-faint">{f.resourceType}</div>
            </div>
          ) },
          { header: "Stack", cell: (f) => <span className="text-ink-dim">{f.projectName}/{f.stackName}</span> },
          { header: "Message", cell: (f) => <span className="text-ink-dim">{f.message}</span> },
          { header: "Observed", cell: (f) => <span className="text-ink-faint">{timeAgo(f.observedAt)}</span> },
        ]}
        empty={<EmptyState icon={ShieldAlert} title="No policy findings"
          description="Findings appear here when an enabled CrossGuard policy pack flags a resource during an update." />}
      />
    </div>
  );
}
