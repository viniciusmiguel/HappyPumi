import { useEffect, useState } from "react";
import { ExternalLink } from "lucide-react";
import { api, type Resource } from "../../lib/api";
import { Modal, KeyValue, Badge } from "../../components/ui";
import { urnName, providerOf, normalizeType, CLOUD_LINKS } from "./resourceFormat";

// A read-only properties dialog for one resource. The list row carries a thin resource; we fetch the
// single-resource endpoint on open to surface full inputs/outputs (which the list may omit).
export function ResourceDetail(
  { org, project, stack, resource, onClose }:
  { org: string; project: string; stack: string; resource: Resource; onClose: () => void },
) {
  const [full, setFull] = useState<Resource>(resource);

  useEffect(() => {
    api.stackResource(org, project, stack, resource.urn).then((r) => {
      if (r?.resource?.resource) setFull(r.resource.resource);
    });
  }, [org, project, stack, resource.urn]);

  const prov = providerOf(full.type);
  const link = CLOUD_LINKS[prov];
  const inputs = Object.entries(full.inputs ?? {});
  const outputs = Object.entries(full.outputs ?? {});

  return (
    <Modal title={urnName(full.urn)} onClose={onClose}>
      <div className="space-y-4">
        <div className="space-y-0.5">
          <KeyValue label="Type"><span className="font-mono text-xs">{normalizeType(full.type)}</span></KeyValue>
          <KeyValue label="URN"><span className="break-all font-mono text-xs text-ink-dim">{full.urn}</span></KeyValue>
          {full.id ? <KeyValue label="ID"><span className="font-mono text-xs">{full.id}</span></KeyValue> : null}
          <KeyValue label="Provider">
            {link
              ? <a className="inline-flex items-center gap-1 text-brand hover:underline" href={link} target="_blank" rel="noreferrer">{prov} <ExternalLink size={12} /></a>
              : <span>{prov || "—"}</span>}
          </KeyValue>
          {full.custom != null ? <KeyValue label="Custom"><Badge>{String(full.custom)}</Badge></KeyValue> : null}
        </div>

        <PropsBlock title="Inputs" entries={inputs} />
        <PropsBlock title="Outputs" entries={outputs} />
      </div>
    </Modal>
  );
}

function PropsBlock({ title, entries }: { title: string; entries: [string, unknown][] }) {
  if (entries.length === 0) return null;
  return (
    <div>
      <h3 className="mb-1.5 text-sm font-semibold text-ink-dim">{title}</h3>
      <div className="rounded-md border border-line">
        {entries.map(([k, v]) => (
          <div key={k} className="flex gap-3 border-b border-line px-3 py-1.5 text-sm last:border-0">
            <span className="w-40 shrink-0 font-mono text-xs text-ink-dim">{k}</span>
            <span className="break-all font-mono text-xs">{render(v)}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function render(v: unknown): string {
  if (v === null || v === undefined) return "—";
  if (typeof v === "object") return JSON.stringify(v);
  return String(v);
}
