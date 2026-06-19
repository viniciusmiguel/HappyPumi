import { useEffect, useState } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { KeyRound } from "lucide-react";
import { api, type EnvRevision, type Actor } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Breadcrumb, Tabs } from "../components/ui";
import {
  Editor, Versions, TagsTab, WebhooksTab, SchedulesTab, RotationTab, ImportedByTab, SettingsTab,
} from "./environment/EnvironmentTabs";

const TABS = [
  { key: "editor", label: "Editor" },
  { key: "versions", label: "Versions" },
  { key: "tags", label: "Environment Tags" },
  { key: "webhooks", label: "Webhooks" },
  { key: "schedules", label: "Scheduled Actions" },
  { key: "rotation", label: "Rotation" },
  { key: "imported", label: "Imported By" },
  { key: "settings", label: "Settings" },
];

export default function EnvironmentDetail() {
  const org = useOrg();
  const { project = "", name = "" } = useParams();
  const [params, setParams] = useSearchParams();
  const active = params.get("tab") || "editor";

  const [revisions, setRevisions] = useState<EnvRevision[]>([]);
  const [owner, setOwner] = useState<Actor | undefined>();

  useEffect(() => {
    api.environmentRevisions(org, project, name).then(setRevisions);
    api.environments(org).then((r) => setOwner(r.environments?.find((e) => e.project === project && e.name === name)?.ownedBy));
  }, [org, project, name]);

  return (
    <div className="pb-10">
      <div className="px-6 pt-5">
        <div className="mb-2 flex items-center gap-2.5">
          <span className="grid size-7 place-items-center rounded-md border border-line bg-panel text-ink-dim"><KeyRound size={16} /></span>
          <h1 className="text-xl font-semibold">{name}</h1>
        </div>
        <Breadcrumb items={[{ label: "Environments", to: "/environments" }, { label: project }, { label: name }]} />
      </div>

      <div className="mt-4">
        <Tabs tabs={TABS} active={active} onChange={(k) => setParams({ tab: k }, { replace: true })} />
      </div>

      <div className="px-6 py-5">
        {active === "editor" && <Editor org={org} project={project} name={name} />}
        {active === "versions" && <Versions revisions={revisions} />}
        {active === "tags" && <TagsTab org={org} project={project} name={name} />}
        {active === "webhooks" && <WebhooksTab org={org} project={project} name={name} />}
        {active === "schedules" && <SchedulesTab org={org} project={project} name={name} />}
        {active === "rotation" && <RotationTab org={org} project={project} name={name} />}
        {active === "imported" && <ImportedByTab org={org} project={project} name={name} />}
        {active === "settings" && <SettingsTab org={org} project={project} name={name} owner={owner} />}
      </div>
    </div>
  );
}
