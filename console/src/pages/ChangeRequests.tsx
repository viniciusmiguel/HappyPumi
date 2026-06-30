import { useCallback, useEffect, useState } from "react";
import { ClipboardCheck } from "lucide-react";
import {
  api,
  type ChangeRequest,
  type ChangeRequestApplicableGate,
  type ChangeRequestDetail,
  type ChangeRequestEvent,
} from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Badge, Card, KeyValue, PageHeader, PrimaryButton, SecondaryButton, Table } from "../components/ui";

// Management → Change requests (change-requests PR3): a PR-like review surface over ESC environment drafts.
// Lists change requests; the detail panel shows the gate evaluation (per-gate approvals N/M), the event
// timeline, and the submit/approve/unapprove/comment/apply/close actions. Apply is blocked until every gate
// is satisfied (the API returns 400 and the panel surfaces the reason). Backed by /api/change-requests/{org}.
export default function ChangeRequests() {
  const org = useOrg();
  const [requests, setRequests] = useState<ChangeRequest[]>([]);
  const [selected, setSelected] = useState<string | null>(null);

  const reload = useCallback(() => {
    api.changeRequests(org).then((r) => setRequests(r.changeRequests ?? []));
  }, [org]);
  useEffect(() => { reload(); }, [reload]);

  return (
    <div>
      <PageHeader icon={ClipboardCheck} title="Change requests" />
      <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card title="Change requests">
          <Table<ChangeRequest>
            rows={requests}
            onRowClick={(c) => setSelected(c.id)}
            empty={<p className="px-6 py-4 text-sm text-ink-dim">No change requests yet. Create an environment draft to open one.</p>}
            columns={[
              { header: "Target", cell: (c) => <span className="font-medium">{targetLabel(c)}</span> },
              { header: "Status", cell: (c) => <StatusBadge status={c.status} /> },
              { header: "Creator", cell: (c) => <span className="text-ink-dim">{c.createdBy?.githubLogin ?? "—"}</span> },
            ]}
          />
        </Card>
        {selected && <Detail org={org} id={selected} onChanged={reload} />}
      </div>
    </div>
  );
}

function targetLabel(c: ChangeRequest): string {
  const e = c.entity;
  return e ? `${e.project ?? ""}/${e.name ?? ""}` : c.id;
}

function StatusBadge({ status }: { status: string }) {
  const tone = status === "applied" ? "success" : status === "submitted" ? "brand" : status === "closed" ? "danger" : "default";
  return <Badge tone={tone}>{status}</Badge>;
}

function Detail({ org, id, onChanged }: { org: string; id: string; onChanged: () => void }) {
  const [cr, setCr] = useState<ChangeRequestDetail | null>(null);
  const [events, setEvents] = useState<ChangeRequestEvent[]>([]);
  const [comment, setComment] = useState("");
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(() => {
    api.changeRequest(org, id).then(setCr);
    api.changeRequestEvents(org, id).then((r) => setEvents(r.events ?? []));
  }, [org, id]);
  useEffect(() => { reload(); }, [reload]);

  const run = async (action: () => Promise<unknown>) => {
    setError(null);
    try {
      await action();
      reload();
      onChanged();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    }
  };

  if (!cr) return <Card title="Details"><p className="px-6 py-4 text-sm text-ink-dim">Loading…</p></Card>;

  const satisfied = cr.gateEvaluation?.satisfied ?? true;
  return (
    <Card title={`Change request · ${targetLabel(cr)}`}>
      <div className="space-y-4 px-6 py-4">
        <KeyValue label="Status"><StatusBadge status={cr.status} /></KeyValue>
        <KeyValue label="Revision">{String(cr.latestRevisionNumber ?? 0)}</KeyValue>
        <GateEvaluation gates={cr.gateEvaluation?.applicableGates ?? []} satisfied={satisfied} />
        <Timeline events={events} />
        <div className="flex flex-wrap gap-2">
          <SecondaryButton onClick={() => run(() => api.submitChangeRequest(org, id))}>Submit</SecondaryButton>
          <SecondaryButton onClick={() => run(() => api.approveChangeRequest(org, id))}>Approve</SecondaryButton>
          <SecondaryButton onClick={() => run(() => api.unapproveChangeRequest(org, id))}>Unapprove</SecondaryButton>
          <SecondaryButton onClick={() => run(() => api.closeChangeRequest(org, id))}>Close</SecondaryButton>
          <PrimaryButton onClick={satisfied ? () => run(() => api.applyChangeRequest(org, id)) : undefined}>
            {satisfied ? "Apply" : "Apply (blocked)"}
          </PrimaryButton>
        </div>
        <div className="flex gap-2">
          <input
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="Add a comment"
            className="flex-1 rounded border border-line bg-transparent px-2 py-1 text-sm"
          />
          <SecondaryButton onClick={() => comment.trim() && run(async () => { await api.commentChangeRequest(org, id, comment.trim()); setComment(""); })}>
            Comment
          </SecondaryButton>
        </div>
        {error && <p className="text-xs text-red-400">{error}</p>}
      </div>
    </Card>
  );
}

function GateEvaluation({ gates, satisfied }: { gates: ChangeRequestApplicableGate[]; satisfied: boolean }) {
  return (
    <div>
      <div className="flex items-center gap-2 text-sm">
        <span className="text-ink-dim">Gates</span>
        <Badge tone={satisfied ? "success" : "warn"}>{satisfied ? "Satisfied" : "Pending"}</Badge>
      </div>
      {gates.length === 0
        ? <p className="mt-1 text-xs text-ink-dim">No gates apply to this change request.</p>
        : <ul className="mt-1 space-y-1 text-sm">
            {gates.map((g) => (
              <li key={g.id} className="flex items-center justify-between">
                <span>{g.name}</span>
                <span className="text-ink-dim">{(g.ruleDetails.approvers?.length ?? 0)} / {g.ruleDetails.requiredApprovals ?? 0} approvals</span>
              </li>
            ))}
          </ul>}
    </div>
  );
}

function Timeline({ events }: { events: ChangeRequestEvent[] }) {
  if (events.length === 0) return <p className="text-xs text-ink-dim">No timeline events yet.</p>;
  return (
    <ul className="space-y-1 text-sm">
      {events.map((e, i) => (
        <li key={i} className="flex items-center gap-2">
          <Badge>{e.eventType}</Badge>
          <span className="text-ink-dim">{e.createdBy?.githubLogin ?? ""} {e.comment ?? ""}</span>
        </li>
      ))}
    </ul>
  );
}
