import { useEffect, useState } from "react";
import { LayoutTemplate, Rocket } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { api, type RegistryTemplate } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import {
  PageHeader, Table, EmptyState, LangBadge, SecondaryButton, Modal, Field, PrimaryButton,
} from "../components/ui";

export default function Templates() {
  const [templates, setTemplates] = useState<RegistryTemplate[]>([]);
  const [deploying, setDeploying] = useState<RegistryTemplate | null>(null);
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
          { header: "", cell: (t) => (
            <SecondaryButton icon={Rocket} onClick={() => setDeploying(t)}>Deploy</SecondaryButton>
          ) },
        ]}
        empty={<EmptyState icon={LayoutTemplate} title="No templates"
          description="Templates let developers generate Pulumi programs and ship them through the console." />}
      />
      {deploying && <DeployModal template={deploying} onClose={() => setDeploying(null)} />}
    </div>
  );
}

// Initiates a managed deployment of a published template via the workflow runner. The runner fetches the
// template archive and runs `pulumi up` against HappyPumi (see GetWorkflowJobEndpoint).
function DeployModal({ template, onClose }: { template: RegistryTemplate; onClose: () => void }) {
  const org = useOrg();
  const navigate = useNavigate();
  const [project, setProject] = useState(template.name);
  const [stack, setStack] = useState("dev");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const templateRef = `${template.source ?? "private"}/${template.publisher ?? org}/${template.name}/${template.version ?? "latest"}`;

  async function deploy() {
    setBusy(true);
    setError(null);
    try {
      await api.createDeployment(org, project, stack, "update", templateRef);
      navigate("/deployments");
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
      setBusy(false);
    }
  }

  return (
    <Modal
      title={`Deploy ${template.name}`}
      onClose={onClose}
      footer={
        <PrimaryButton icon={Rocket} onClick={busy ? undefined : deploy}>
          {busy ? "Starting…" : "Deploy"}
        </PrimaryButton>
      }
    >
      <p className="text-xs text-ink-dim">
        Runs <code className="text-ink">pulumi up</code> of <code className="text-ink">{templateRef}</code> on
        the workflow runner, creating the stack if it doesn't exist.
      </p>
      <Field label="Project" value={project} onChange={setProject} placeholder="project name" />
      <Field label="Stack" value={stack} onChange={setStack} placeholder="dev" />
      {error && <p className="text-xs text-red-400">{error}</p>}
    </Modal>
  );
}
