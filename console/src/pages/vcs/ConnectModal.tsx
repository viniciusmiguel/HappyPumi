import { useState } from "react";
import { api } from "../../lib/api";
import { Modal, PrimaryButton, SecondaryButton, Field } from "../../components/ui";

// Connect flow for the two in-scope providers. GitHub uses the App-install URL; Azure DevOps creates the
// record, runs OAuth (initiate -> the user authorizes in a new tab -> paste the returned code -> complete).
export function ConnectModal({ org, onClose, onDone }: { org: string; onClose: () => void; onDone: () => void }) {
  const [tab, setTab] = useState<"github" | "azure-devops">("github");
  return (
    <Modal title="Connect version control" onClose={onClose}
      footer={<SecondaryButton onClick={onClose}>Close</SecondaryButton>}>
      <div className="mb-4 flex gap-2">
        <SecondaryButton onClick={() => setTab("github")}>GitHub</SecondaryButton>
        <SecondaryButton onClick={() => setTab("azure-devops")}>Azure DevOps</SecondaryButton>
      </div>
      {tab === "github" ? <GitHubConnect org={org} /> : <AzureDevOpsConnect org={org} onDone={onDone} />}
    </Modal>
  );
}

function GitHubConnect({ org }: { org: string }) {
  async function connect() {
    const res = await api.startGitHubSetup(org);
    if (res.installationUrl) window.open(res.installationUrl, "_blank", "noopener");
  }
  return (
    <div className="space-y-3">
      <p className="text-sm text-ink-faint">
        Install the HappyPumi GitHub App to enable pull-request comments, policy enforcement, and drift detection.
      </p>
      <PrimaryButton onClick={connect}>Connect GitHub</PrimaryButton>
    </div>
  );
}

function AzureDevOpsConnect({ org, onDone }: { org: string; onDone: () => void }) {
  const [adoOrg, setAdoOrg] = useState("");
  const [project, setProject] = useState("");
  const [code, setCode] = useState("");
  const [session, setSession] = useState("");
  const [error, setError] = useState("");

  async function initiate() {
    setError("");
    try {
      await api.createAzureDevOpsSetup(org, adoOrg, project);
      const res = await api.initiateAzureDevOpsOAuth(org);
      setSession(res.sessionID);
      if (res.url) window.open(res.url, "_blank", "noopener");
    } catch (e) { setError(String(e)); }
  }

  async function complete() {
    setError("");
    try {
      await api.completeAzureDevOpsOAuth(org, code, session);
      onDone();
    } catch (e) { setError(String(e)); }
  }

  return (
    <div className="space-y-3">
      <Field label="Azure DevOps organization" value={adoOrg} onChange={setAdoOrg} placeholder="mycompany" />
      <Field label="Project" value={project} onChange={setProject} placeholder="Widgets" />
      <PrimaryButton onClick={initiate}>Authorize Azure DevOps</PrimaryButton>
      {session && (
        <div className="space-y-2 border-t border-line pt-3">
          <Field label="Authorization code" value={code} onChange={setCode} placeholder="paste the code from the redirect" />
          <PrimaryButton onClick={complete}>Complete connection</PrimaryButton>
        </div>
      )}
      {error && <p className="text-sm text-danger">{error}</p>}
    </div>
  );
}
