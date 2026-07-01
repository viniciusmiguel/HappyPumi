import { useCallback, useEffect, useState } from "react";
import { ClipboardList, Download } from "lucide-react";
import { api, timeAgo, type AuditEvent, type AuditExportSettings } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, Avatar, Card, Field, PrimaryButton, SecondaryButton, Badge } from "../components/ui";

// Management → Audit logs (org-admin PR2): the v2 event list plus the S3 export-config lifecycle
// (enable/disable, bucket/role/prefix, force + test, CSV download). Backed by the real
// /api/orgs/{org}/auditlogs spec endpoints.
export default function AuditLogs() {
  const org = useOrg();
  const [events, setEvents] = useState<AuditEvent[]>([]);

  useEffect(() => { api.auditLogEvents(org).then((r) => setEvents(r.auditLogEvents ?? [])); }, [org]);

  return (
    <div>
      <PageHeader
        icon={ClipboardList}
        title="Audit logs"
        actions={<a href={`/api/orgs/${org}/auditlogs/export`} download
          className="inline-flex items-center gap-2 rounded-md border border-line px-3 py-1.5 text-sm text-ink hover:bg-surface-2">
          <Download size={16} /> Export CSV
        </a>}
      />
      <div className="mt-4">
        <ExportConfigCard org={org} />
      </div>
      <div className="mt-4">
        <Table
          rows={events}
          columns={[
            { header: "Event", cell: (e) => <code className="text-ink">{e.event}</code> },
            { header: "Description", cell: (e) => <span className="text-ink-dim">{e.description}</span> },
            { header: "Actor", cell: (e) => <span className="flex items-center gap-2"><Avatar name={e.actorName} size={18} />{e.actorName}</span> },
            { header: "When", cell: (e) => <span className="text-ink-faint">{timeAgo(e.timestamp)}</span> },
          ]}
          empty={<EmptyState icon={ClipboardList} title="No audit events"
            description="Every infrastructure-changing action in your organization is recorded here." />}
        />
      </div>
    </div>
  );
}

function ExportConfigCard({ org }: { org: string }) {
  const [config, setConfig] = useState<AuditExportSettings | null>(null);
  const [bucket, setBucket] = useState("");
  const [roleArn, setRoleArn] = useState("");
  const [prefix, setPrefix] = useState("");
  const [enabled, setEnabled] = useState(false);
  const [status, setStatus] = useState<string | null>(null);

  const reload = useCallback(() => {
    api.auditExportConfig(org).then((c) => {
      setConfig(c);
      setEnabled(c.enabled);
      setBucket(c.s3Config.s3BucketName ?? "");
      setRoleArn(c.s3Config.iamRoleArn ?? "");
      setPrefix(c.s3Config.s3PathPrefix ?? "");
    });
  }, [org]);
  useEffect(() => { reload(); }, [reload]);

  const save = async () => {
    await api.updateAuditExportConfig(org, {
      newEnabled: enabled,
      newS3Configuration: { iamRoleArn: roleArn, s3BucketName: bucket, s3PathPrefix: prefix },
    });
    setStatus("Configuration saved");
    reload();
  };

  const force = async () => setStatus((await api.forceAuditExport(org)).message);
  const test = async () => setStatus((await api.testAuditExport(org, { iamRoleArn: roleArn, s3BucketName: bucket, s3PathPrefix: prefix })).message);
  const remove = async () => { await api.deleteAuditExportConfig(org); setStatus("Configuration removed"); reload(); };

  return (
    <Card
      title={<span className="flex items-center gap-2">S3 export configuration
        <Badge tone={config?.enabled ? "success" : "default"}>{config?.enabled ? "Enabled" : "Disabled"}</Badge></span>}
      actions={<PrimaryButton onClick={save}>Save</PrimaryButton>}
    >
      <label className="mb-3 flex items-center gap-2 text-sm text-ink">
        <input type="checkbox" checked={enabled} onChange={(e) => setEnabled(e.target.checked)} />
        Enable automated audit-log export
      </label>
      <Field label="S3 bucket name" value={bucket} onChange={setBucket} placeholder="my-audit-bucket" />
      <Field label="IAM role ARN" value={roleArn} onChange={setRoleArn} placeholder="arn:aws:iam::123456789012:role/pulumi-audit" />
      <Field label="S3 path prefix (optional)" value={prefix} onChange={setPrefix} placeholder="audit-logs/" />
      <div className="mt-3 flex items-center gap-3">
        <SecondaryButton onClick={test}>Test</SecondaryButton>
        <SecondaryButton onClick={force}>Force export</SecondaryButton>
        <SecondaryButton onClick={remove}>Remove</SecondaryButton>
      </div>
      {status && <p className="mt-3 text-sm text-ink-dim">{status}</p>}
      {config?.lastResult?.timestamp ? (
        <p className="mt-1 text-xs text-ink-faint">Last result: {config.lastResult.message || "success"} ({timeAgo(config.lastResult.timestamp)})</p>
      ) : null}
    </Card>
  );
}
