import { Plug, ExternalLink } from "lucide-react";
import { PageHeader } from "../components/ui";

const CD = [
  "AWS Code Services", "Codefresh", "GitHub Actions", "GitLab", "Google Cloud Build",
  "Jenkins", "JetBrains TeamCity", "Kubernetes Operator", "Travis CI", "Octopus", "Spinnaker",
];

export default function Integrations() {
  return (
    <div>
      <PageHeader icon={Plug} title="Integrations" />
      <div className="px-6 py-5">
        <h2 className="mb-1 text-sm font-semibold">Continuous delivery</h2>
        <p className="mb-4 text-sm text-ink-dim">
          Pulumi supports a wide range of continuous-delivery workflows and integrates well with popular CI/CD platforms.
        </p>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {CD.map((name) => (
            <a key={name} href="https://www.pulumi.com/docs/iac/packages-and-automation/continuous-delivery/" target="_blank" rel="noreferrer"
              className="flex items-center gap-3 rounded-lg border border-line bg-panel p-4 transition-colors hover:bg-hover">
              <span className="grid size-9 place-items-center rounded-md bg-hover text-ink-dim">{name[0]}</span>
              <span className="flex-1 text-sm font-medium">{name}</span>
              <ExternalLink size={14} className="text-ink-faint" />
            </a>
          ))}
        </div>
      </div>
    </div>
  );
}
