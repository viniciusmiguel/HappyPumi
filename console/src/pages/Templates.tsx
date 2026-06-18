import { useEffect, useState } from "react";
import { LayoutTemplate } from "lucide-react";
import { api, type RegistryTemplate } from "../lib/api";
import { PageHeader, Table, EmptyState, LangBadge } from "../components/ui";

export default function Templates() {
  const [templates, setTemplates] = useState<RegistryTemplate[]>([]);
  useEffect(() => { api.templates().then((r) => setTemplates(r.templates ?? [])); }, []);

  return (
    <div>
      <PageHeader icon={LayoutTemplate} title="Templates" />
      <Table
        rows={templates}
        columns={[
          { header: "Template", cell: (t) => <span className="font-medium">{t.source ? `${t.source}/` : ""}{t.name}</span> },
          { header: "Publisher", cell: (t) => <span className="text-ink-dim">{t.publisher ?? t.source ?? "—"}</span> },
          { header: "Tags", cell: (t) => <LangBadge label={t.language} /> },
        ]}
        empty={<EmptyState icon={LayoutTemplate} title="No templates"
          description="Templates let developers generate Pulumi programs and ship them through the console." />}
      />
    </div>
  );
}
