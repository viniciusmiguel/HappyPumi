import { useCallback, useEffect, useState } from "react";
import { ShieldCheck } from "lucide-react";
import { api, type ChangeGate, type ChangeGateInput } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Badge, Card, Field, Modal, PageHeader, PrimaryButton, SecondaryButton, Table } from "../components/ui";

// Settings → Change gates (change-requests PR1): list / create / edit / delete approval gates that require
// staged changes to a target entity (today only ESC environments) to collect N approvals before apply.
// Backed by the real /api/change-gates/{org} CRUD endpoints.
export default function ChangeGates() {
  const org = useOrg();
  const [gates, setGates] = useState<ChangeGate[]>([]);
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<ChangeGate | null>(null);

  const reload = useCallback(() => {
    api.changeGates(org).then((r) => setGates(r.gates ?? []));
  }, [org]);
  useEffect(() => { reload(); }, [reload]);

  const remove = async (id: string) => {
    await api.deleteChangeGate(org, id);
    reload();
  };

  return (
    <div>
      <PageHeader icon={ShieldCheck} title="Change gates" />
      <div className="mt-4">
        <Card
          title="Approval gates"
          actions={<PrimaryButton onClick={() => setCreating(true)}>Create gate</PrimaryButton>}
        >
          <Table<ChangeGate>
            rows={gates}
            empty={<p className="px-6 py-4 text-sm text-ink-dim">No change gates yet. Create one to require approvals before changes apply.</p>}
            columns={[
              { header: "Name", cell: (g) => <span className="font-medium">{g.name}</span> },
              {
                header: "Status",
                cell: (g) => <Badge tone={g.enabled ? "success" : "default"}>{g.enabled ? "Enabled" : "Disabled"}</Badge>,
              },
              { header: "Target", cell: (g) => <span className="text-ink-dim">{targetLabel(g)}</span> },
              {
                header: "Approvals",
                cell: (g) => <span className="text-ink-dim">{g.rule.numApprovalsRequired ?? 0}</span>,
              },
              {
                header: "", className: "text-right",
                cell: (g) => (
                  <span className="flex justify-end gap-3">
                    <button onClick={() => setEditing(g)} className="text-brand hover:underline">Edit</button>
                    <button onClick={() => remove(g.id)} className="text-red-400 hover:text-red-300">Delete</button>
                  </span>
                ),
              },
            ]}
          />
          {creating && (
            <GateModal org={org} onClose={() => setCreating(false)} onSaved={() => { setCreating(false); reload(); }} />
          )}
          {editing && (
            <GateModal
              org={org} gate={editing}
              onClose={() => setEditing(null)}
              onSaved={() => { setEditing(null); reload(); }}
            />
          )}
        </Card>
      </div>
    </div>
  );
}

function targetLabel(g: ChangeGate): string {
  const actions = (g.target.actionTypes ?? []).join(", ") || "any";
  return `${g.target.entityType} · ${actions}`;
}

function GateModal({ org, gate, onClose, onSaved }: {
  org: string; gate?: ChangeGate; onClose: () => void; onSaved: () => void;
}) {
  const [name, setName] = useState(gate?.name ?? "");
  const [enabled, setEnabled] = useState(gate?.enabled ?? true);
  const [numApprovals, setNumApprovals] = useState(String(gate?.rule.numApprovalsRequired ?? 1));
  const [allowSelfApproval, setAllowSelfApproval] = useState(gate?.rule.allowSelfApproval ?? false);
  const [entityType, setEntityType] = useState(gate?.target.entityType ?? "environment");
  const [actionTypes, setActionTypes] = useState((gate?.target.actionTypes ?? ["update"]).join(", "));
  const [qualifiedName, setQualifiedName] = useState(gate?.target.qualifiedName ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const valid = name.trim().length > 0;

  const save = async () => {
    if (!valid) return;
    setSaving(true);
    setError(null);
    try {
      const body = buildBody(name, enabled, numApprovals, allowSelfApproval, entityType, actionTypes, qualifiedName);
      if (gate) await api.updateChangeGate(org, gate.id, body);
      else await api.createChangeGate(org, body);
      onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title={gate ? "Edit change gate" : "Create change gate"}
      onClose={onClose}
      footer={<>
        <SecondaryButton onClick={onClose}>Cancel</SecondaryButton>
        <PrimaryButton onClick={saving || !valid ? undefined : save}>{gate ? "Save" : "Create"}</PrimaryButton>
      </>}
    >
      <Field label="Name" value={name} onChange={setName} placeholder="prod-approvals" />
      <Field label="Enabled" value={String(enabled)} onChange={(v) => setEnabled(v === "true")} options={["true", "false"]} />
      <Field label="Approvals required" value={numApprovals} onChange={setNumApprovals} placeholder="1" />
      <Field label="Allow self approval" value={String(allowSelfApproval)} onChange={(v) => setAllowSelfApproval(v === "true")} options={["true", "false"]} />
      <Field label="Entity type" value={entityType} onChange={setEntityType} options={["environment"]} />
      <Field label="Action types (comma separated)" value={actionTypes} onChange={setActionTypes} placeholder="update" />
      <Field label="Qualified name (optional)" value={qualifiedName} onChange={setQualifiedName} placeholder="project/env" />
      {error && <p className="text-xs text-red-400">{error}</p>}
    </Modal>
  );
}

function buildBody(
  name: string, enabled: boolean, numApprovals: string, allowSelfApproval: boolean,
  entityType: string, actionTypes: string, qualifiedName: string,
): ChangeGateInput {
  const approvals = Number.parseInt(numApprovals, 10);
  const actions = actionTypes.split(",").map((a) => a.trim()).filter((a) => a.length > 0);
  return {
    enabled,
    name: name.trim(),
    rule: {
      ruleType: "approval_required",
      numApprovalsRequired: Number.isFinite(approvals) && approvals > 0 ? approvals : 1,
      allowSelfApproval,
      requireReapprovalOnChange: true,
      eligibleApprovers: [],
    },
    target: {
      entityType,
      actionTypes: actions.length > 0 ? actions : ["update"],
      qualifiedName: qualifiedName.trim().length > 0 ? qualifiedName.trim() : undefined,
    },
  };
}
