import { useEffect, useState } from "react";
import { ClipboardList } from "lucide-react";
import { api, timeAgo, type AuditEvent } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, Avatar } from "../components/ui";

export default function AuditLogs() {
  const org = useOrg();
  const [events, setEvents] = useState<AuditEvent[]>([]);
  useEffect(() => { api.auditLogs(org).then((r) => setEvents(r.auditLogEvents ?? [])); }, [org]);

  return (
    <div>
      <PageHeader icon={ClipboardList} title="Audit logs" />
      <Table
        rows={events}
        columns={[
          { header: "Event", cell: (e) => <code className="text-ink">{e.event}</code> },
          { header: "Description", cell: (e) => <span className="text-ink-dim">{e.description}</span> },
          { header: "Actor", cell: (e) => <span className="flex items-center gap-2"><Avatar name={e.actorName} size={18} />{e.actorName}</span> },
          { header: "Source IP", cell: (e) => <span className="text-ink-faint">{e.sourceIP}</span> },
          { header: "When", cell: (e) => <span className="text-ink-faint">{timeAgo(e.timestamp)}</span> },
        ]}
        empty={<EmptyState icon={ClipboardList} title="No audit events"
          description="Every infrastructure-changing action in your organization is recorded here." />}
      />
    </div>
  );
}
