import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";

/** Page header with an icon, title, and optional actions, matching the console's page chrome. */
export function PageHeader({ icon: Icon, title, actions }: { icon: LucideIcon; title: string; actions?: ReactNode }) {
  return (
    <div className="flex items-center justify-between border-b border-line px-6 py-4">
      <div className="flex items-center gap-2.5">
        <span className="grid size-7 place-items-center rounded-md border border-line bg-panel text-ink-dim">
          <Icon size={16} />
        </span>
        <h1 className="text-lg font-semibold">{title}</h1>
      </div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </div>
  );
}

/** Centered empty state with icon, title, description, and an optional action. */
export function EmptyState({
  icon: Icon, title, description, action,
}: { icon: LucideIcon; title: string; description?: ReactNode; action?: ReactNode }) {
  return (
    <div className="grid place-items-center px-6 py-24 text-center">
      <span className="mb-5 grid size-12 place-items-center rounded-full border border-line bg-panel text-ink-dim">
        <Icon size={22} />
      </span>
      <h2 className="text-xl font-semibold">{title}</h2>
      {description && <p className="mt-2 max-w-xl text-sm text-ink-dim">{description}</p>}
      {action && <div className="mt-6">{action}</div>}
    </div>
  );
}

export function PrimaryButton({ children, icon: Icon, onClick }: { children: ReactNode; icon?: LucideIcon; onClick?: () => void }) {
  return (
    <button
      onClick={onClick}
      className="inline-flex items-center gap-2 rounded-md bg-brand px-3.5 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-hover"
    >
      {Icon && <Icon size={16} />} {children}
    </button>
  );
}

/** Simple data table. Columns render a cell from a row. */
export function Table<T>({ columns, rows, empty }: {
  columns: { header: string; cell: (row: T) => ReactNode; className?: string }[];
  rows: T[];
  empty?: ReactNode;
}) {
  if (rows.length === 0 && empty) return <>{empty}</>;
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-line text-left text-xs uppercase tracking-wider text-ink-faint">
            {columns.map((c) => <th key={c.header} className={`px-6 py-2.5 font-medium ${c.className ?? ""}`}>{c.header}</th>)}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={i} className="border-b border-line/60 transition-colors hover:bg-hover">
              {columns.map((c) => <td key={c.header} className={`px-6 py-3 ${c.className ?? ""}`}>{c.cell(row)}</td>)}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function LangBadge({ label }: { label?: string }) {
  if (!label) return <span className="text-ink-faint">—</span>;
  return <span className="rounded bg-panel px-2 py-0.5 text-xs text-ink-dim">{label}</span>;
}
