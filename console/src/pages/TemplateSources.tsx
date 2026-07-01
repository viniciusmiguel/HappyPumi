import { useCallback, useEffect, useState } from "react";
import { LayoutTemplate } from "lucide-react";
import { api, type TemplateSource, type UpsertTemplateSourceBody } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Badge, Card, Field, Modal, PageHeader, PrimaryButton, SecondaryButton, Table } from "../components/ui";

// Settings → Template sources (templates PR1): list / add / edit / delete the org's template sources. Each
// source records where project templates are fetched from; the API runs a deterministic http(s) URL check
// surfaced as the valid badge / error. Backed by the real /api/orgs/{org}/templates/sources spec endpoints.
export default function TemplateSources() {
  const org = useOrg();
  const [sources, setSources] = useState<TemplateSource[]>([]);
  const [editing, setEditing] = useState<TemplateSource | null>(null);
  const [creating, setCreating] = useState(false);

  const reload = useCallback(() => {
    api.templateSources(org).then((r) => setSources(r.sources ?? []));
  }, [org]);
  useEffect(() => { reload(); }, [reload]);

  const remove = async (id: string) => {
    await api.deleteTemplateSource(org, id);
    reload();
  };

  return (
    <div>
      <PageHeader icon={LayoutTemplate} title="Template sources" />
      <div className="mt-4">
        <Card
          title="Registered sources"
          actions={<PrimaryButton onClick={() => setCreating(true)}>Add source</PrimaryButton>}
        >
          <Table<TemplateSource>
            rows={sources}
            empty={<p className="px-6 py-4 text-sm text-ink-dim">No template sources yet. Add one to source project templates for the org.</p>}
            columns={[
              { header: "Name", cell: (s) => <span className="font-medium">{s.name}</span> },
              { header: "Source URL", cell: (s) => <span className="text-ink-dim">{s.sourceURL}</span> },
              {
                header: "Status",
                cell: (s) => <Badge tone={s.isValid ? "success" : "danger"}>{s.isValid ? "Valid" : "Invalid"}</Badge>,
              },
              { header: "Error", cell: (s) => <span className="text-red-400">{s.error ?? ""}</span> },
              {
                header: "", className: "text-right",
                cell: (s) => (
                  <span className="flex justify-end gap-3">
                    <button onClick={() => setEditing(s)} className="text-brand hover:underline">Edit</button>
                    <button onClick={() => remove(s.id)} className="text-red-400 hover:text-red-300">Delete</button>
                  </span>
                ),
              },
            ]}
          />
          {creating && (
            <SourceModal
              org={org}
              onClose={() => setCreating(false)}
              onSaved={() => { setCreating(false); reload(); }}
            />
          )}
          {editing && (
            <SourceModal
              org={org}
              source={editing}
              onClose={() => setEditing(null)}
              onSaved={() => { setEditing(null); reload(); }}
            />
          )}
        </Card>
      </div>
    </div>
  );
}

function SourceModal({ org, source, onClose, onSaved }: {
  org: string; source?: TemplateSource; onClose: () => void; onSaved: () => void;
}) {
  const [name, setName] = useState(source?.name ?? "");
  const [sourceURL, setSourceURL] = useState(source?.sourceURL ?? "");
  const [destinationURL, setDestinationURL] = useState(source?.destinationURL ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const valid = name.trim().length > 0 && sourceURL.trim().length > 0;

  const save = async () => {
    if (!valid) return;
    setSaving(true);
    setError(null);
    try {
      const body: UpsertTemplateSourceBody = { name, sourceURL };
      if (destinationURL.trim().length > 0) body.destinationURL = destinationURL;
      if (source) await api.updateTemplateSource(org, source.id, body);
      else await api.createTemplateSource(org, body);
      onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title={source ? "Edit template source" : "Add template source"}
      onClose={onClose}
      footer={<>
        <SecondaryButton onClick={onClose}>Cancel</SecondaryButton>
        <PrimaryButton onClick={saving || !valid ? undefined : save}>{source ? "Save" : "Add"}</PrimaryButton>
      </>}
    >
      <Field label="Name" value={name} onChange={setName} placeholder="team-templates" />
      <Field label="Source URL" value={sourceURL} onChange={setSourceURL} placeholder="https://github.com/acme/templates" />
      <Field label="Destination URL (optional)" value={destinationURL} onChange={setDestinationURL} placeholder="https://github.com/acme/generated" />
      {error && <p className="text-xs text-red-400">{error}</p>}
    </Modal>
  );
}
