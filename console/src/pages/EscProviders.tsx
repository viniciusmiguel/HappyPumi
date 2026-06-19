import { useEffect, useState } from "react";
import { Boxes, KeyRound, RefreshCw } from "lucide-react";
import { api, type ProviderSchema, type EscSchema } from "../lib/api";
import { PageHeader, Breadcrumb, Card, Badge, EmptyState } from "../components/ui";

type Kind = "open" | "rotate";
interface Selection { kind: Kind; name: string; }

export default function EscProviders() {
  const [providers, setProviders] = useState<string[]>([]);
  const [rotators, setRotators] = useState<string[]>([]);
  const [selected, setSelected] = useState<Selection | null>(null);
  const [schema, setSchema] = useState<ProviderSchema | null>(null);

  useEffect(() => {
    api.escProviders().then((r) => {
      const list = r.providers ?? [];
      setProviders(list);
      if (list.length) setSelected({ kind: "open", name: list[0] });
    });
    api.escRotators().then((r) => setRotators(r.rotators ?? []));
  }, []);

  useEffect(() => {
    if (!selected) return;
    let active = true; // ignore a stale resolve if the selection changed first
    const load = selected.kind === "open" ? api.escProviderSchema : api.escRotatorSchema;
    load(selected.name).then((s) => { if (active) setSchema(s); });
    return () => { active = false; };
  }, [selected]);

  return (
    <div className="pb-10">
      <PageHeader icon={Boxes} title="Providers & rotators" />
      <div className="px-6 pt-2"><Breadcrumb items={[{ label: "Environments", to: "/environments" }, { label: "Providers" }]} /></div>

      <div className="grid grid-cols-[16rem_1fr] gap-6 px-6 py-5">
        <nav className="space-y-4 text-sm">
          <CatalogGroup icon={KeyRound} title="Dynamic providers" hint="fn::open"
            names={providers} kind="open" selected={selected} onSelect={setSelected} />
          <CatalogGroup icon={RefreshCw} title="Secret rotators" hint="fn::rotate"
            names={rotators} kind="rotate" selected={selected} onSelect={setSelected} />
        </nav>

        <div>
          {schema ? <SchemaDetail kind={selected!.kind} schema={schema} />
            : <EmptyState icon={Boxes} title="Select an integration" description="Pick a provider or rotator to see its inputs and a snippet." />}
        </div>
      </div>
    </div>
  );
}

function CatalogGroup({ icon: Icon, title, hint, names, kind, selected, onSelect }: {
  icon: typeof KeyRound; title: string; hint: string; names: string[]; kind: Kind;
  selected: Selection | null; onSelect: (s: Selection) => void;
}) {
  return (
    <div>
      <div className="mb-1.5 flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-ink-faint">
        <Icon size={13} /> {title} <code className="rounded bg-panel px-1 text-[10px] normal-case">{hint}</code>
      </div>
      <div className="space-y-0.5">
        {names.map((n) => {
          const active = selected?.kind === kind && selected.name === n;
          return (
            <button key={n} onClick={() => onSelect({ kind, name: n })}
              className={`block w-full rounded-md px-3 py-1.5 text-left font-mono text-xs ${active ? "bg-active text-ink" : "text-ink-dim hover:bg-hover"}`}>
              {n}
            </button>
          );
        })}
        {names.length === 0 && <p className="px-3 text-xs text-ink-faint">None registered.</p>}
      </div>
    </div>
  );
}

function SchemaDetail({ kind, schema }: { kind: Kind; schema: ProviderSchema }) {
  const fnKey = `fn::${kind}::${schema.name}`;
  return (
    <div className="max-w-3xl space-y-5">
      <div>
        <div className="flex items-center gap-2.5">
          <h2 className="text-lg font-semibold">{schema.name}</h2>
          <Badge tone={kind === "open" ? "brand" : "warn"}>{fnKey}</Badge>
        </div>
        {schema.description && <p className="mt-1 text-sm text-ink-dim">{schema.description}</p>}
      </div>

      <SchemaTable title="Inputs" schema={schema.inputs} />
      <SchemaTable title="Outputs" schema={schema.outputs} />

      <Card title="Usage">
        <pre className="overflow-auto font-mono text-xs leading-relaxed">{snippet(fnKey, schema.inputs)}</pre>
      </Card>
    </div>
  );
}

function SchemaTable({ title, schema }: { title: string; schema?: EscSchema }) {
  const props = schema?.properties ?? {};
  const entries = Object.entries(props);
  return (
    <div>
      <h3 className="mb-2 text-sm font-semibold">{title}</h3>
      {entries.length === 0 ? (
        <p className="text-xs text-ink-faint">{schema?.additionalProperties ? "Free-form map of values." : "No fields."}</p>
      ) : (
        <div className="overflow-hidden rounded-lg border border-line">
          <div className="grid grid-cols-[1fr_8rem_5rem] border-b border-line bg-panel px-3 py-1.5 text-xs font-medium uppercase tracking-wider text-ink-faint">
            <div>Field</div><div>Type</div><div>Required</div>
          </div>
          {entries.map(([key, sub]) => (
            <div key={key} className="grid grid-cols-[1fr_8rem_5rem] items-center border-b border-line/60 px-3 py-1.5 text-sm last:border-0">
              <div className="flex items-center gap-2">
                <span className="font-mono text-xs">{key}</span>
                {sub.secret && <Badge tone="warn">secret</Badge>}
                {sub.description && <span className="truncate text-xs text-ink-faint">— {sub.description}</span>}
              </div>
              <div className="text-xs text-ink-dim">{sub.type ?? (sub.properties ? "object" : "—")}</div>
              <div className="text-xs">{schema?.required?.includes(key) ? "yes" : "—"}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

// Minimal copy-paste YAML snippet listing the integration's required inputs as placeholders.
function snippet(fnKey: string, inputs?: EscSchema): string {
  const required = inputs?.required ?? [];
  const fields = required.length ? required : Object.keys(inputs?.properties ?? {});
  const body = fields.length ? fields.map((f) => `        ${f}: <${f}>`).join("\n") : "        # (no inputs)";
  return `values:\n  example:\n    ${fnKey}:\n${body}`;
}
