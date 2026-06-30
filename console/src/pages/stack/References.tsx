import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Share2 } from "lucide-react";
import { api, type StackRef } from "../../lib/api";
import { Card, EmptyState, Badge } from "../../components/ui";

// The References tab: upstream (stacks this one reads via StackReference) and downstream (stacks that
// read this one). Derived server-side from checkpoint StackReference resources.
export function References({ org, project, stack }: { org: string; project: string; stack: string }) {
  const [upstream, setUpstream] = useState<StackRef[]>([]);
  const [downstream, setDownstream] = useState<StackRef[]>([]);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    Promise.all([
      api.stackUpstreamRefs(org, project, stack),
      api.stackDownstreamRefs(org, project, stack),
    ]).then(([u, d]) => {
      setUpstream(u.referencedStacks);
      setDownstream(d.referencedStacks);
      setLoaded(true);
    });
  }, [org, project, stack]);

  if (loaded && upstream.length === 0 && downstream.length === 0) {
    return <EmptyState icon={Share2} title="No stack references"
      description="Stacks read via StackReference (and stacks that read this one) appear here." />;
  }

  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <RefList title="Reads from (upstream)" refs={upstream} />
      <RefList title="Read by (downstream)" refs={downstream} />
    </div>
  );
}

function RefList({ title, refs }: { title: string; refs: StackRef[] }) {
  return (
    <Card title={title}>
      {refs.length === 0 ? <p className="text-sm text-ink-dim">None.</p> : (
        <ul className="space-y-1.5">
          {refs.map((r) => (
            <li key={`${r.organization}/${r.routingProject}/${r.name}`}
              className="flex items-center justify-between rounded-md border border-line px-3 py-1.5 text-sm">
              <Link to={`/stacks/${r.routingProject}/${r.name}`} className="font-medium hover:underline">
                {r.routingProject}/{r.name}
              </Link>
              <Badge>v{r.version}</Badge>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
