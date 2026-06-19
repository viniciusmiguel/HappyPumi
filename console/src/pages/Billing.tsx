import { useEffect, useState } from "react";
import { CreditCard } from "lucide-react";
import { api, type Stack, type Deployment } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Card } from "../components/ui";

// Billing & usage is computed from real org data (no separate metering store): stacks under management,
// total resources across their latest checkpoints, and deployments run.
export default function Billing() {
  const org = useOrg();
  const [stacks, setStacks] = useState<Stack[]>([]);
  const [deployments, setDeployments] = useState<Deployment[]>([]);

  useEffect(() => {
    api.userStacks().then((r) => setStacks(r.stacks ?? []));
    api.orgDeployments(org).then((r) => setDeployments(r.deployments ?? []));
  }, [org]);

  const resourceCount = stacks.reduce((sum, s) => sum + (s.resourceCount ?? 0), 0);
  const metrics = [
    { label: "Stacks under management", value: stacks.length },
    { label: "Resources under management", value: resourceCount },
    { label: "Deployments run", value: deployments.length },
  ];

  return (
    <div>
      <PageHeader icon={CreditCard} title="Billing & usage" />
      <div className="grid max-w-3xl grid-cols-1 gap-4 p-6 sm:grid-cols-3">
        {metrics.map((m) => (
          <Card key={m.label} title={m.label}>
            <div className="text-3xl font-semibold text-ink">{m.value}</div>
          </Card>
        ))}
      </div>
      <div className="px-6">
        <Card title="Plan">
          <div className="flex items-center justify-between">
            <div>
              <div className="text-sm font-medium text-ink">HappyPumi — Self-hosted</div>
              <div className="text-xs text-ink-dim">Unlimited stacks, resources, and deployments.</div>
            </div>
            <span className="rounded-full bg-brand/15 px-3 py-1 text-xs font-medium text-brand">Active</span>
          </div>
        </Card>
      </div>
    </div>
  );
}
