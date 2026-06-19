import { useEffect, useRef, useState, type ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { Link, NavLink } from "react-router-dom";
import { X } from "lucide-react";

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

/** Breadcrumb trail. Each crumb is text or a link; the last is the current page. */
export function Breadcrumb({ items }: { items: { label: string; to?: string }[] }) {
  return (
    <nav className="flex items-center gap-1.5 text-sm text-ink-dim">
      {items.map((it, i) => (
        <span key={i} className="flex items-center gap-1.5">
          {it.to ? <Link to={it.to} className="hover:text-ink hover:underline">{it.label}</Link>
            : <span className={i === items.length - 1 ? "font-medium text-ink" : ""}>{it.label}</span>}
          {i < items.length - 1 && <span className="text-ink-faint">/</span>}
        </span>
      ))}
    </nav>
  );
}

export interface TabDef { key: string; label: string; badge?: ReactNode; }
/** Horizontal tab bar driven by a controlled active key. */
export function Tabs({ tabs, active, onChange }: { tabs: TabDef[]; active: string; onChange: (k: string) => void }) {
  return (
    <div className="flex gap-5 border-b border-line px-6">
      {tabs.map((t) => (
        <button
          key={t.key}
          onClick={() => onChange(t.key)}
          className={`-mb-px flex items-center gap-2 border-b-2 px-0.5 py-2.5 text-sm transition-colors ${
            active === t.key ? "border-brand text-ink" : "border-transparent text-ink-dim hover:text-ink"
          }`}
        >
          {t.label}
          {t.badge != null && <Badge>{t.badge}</Badge>}
        </button>
      ))}
    </div>
  );
}

/** Small rounded count/label chip. */
export function Badge({ children, tone = "default" }: { children: ReactNode; tone?: "default" | "brand" | "success" | "warn" | "danger" }) {
  const tones: Record<string, string> = {
    default: "bg-panel text-ink-dim",
    brand: "bg-brand/15 text-brand",
    success: "bg-emerald-500/15 text-emerald-400",
    warn: "bg-amber-500/15 text-amber-400",
    danger: "bg-red-500/15 text-red-400",
  };
  return <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${tones[tone]}`}>{children}</span>;
}

/** Colored status dot + text for update/deployment results. */
export function StatusDot({ status }: { status?: string }) {
  const s = (status ?? "").toLowerCase();
  const color = s.includes("succe") ? "bg-emerald-500"
    : s.includes("fail") ? "bg-red-500"
    : s.includes("running") || s.includes("in-progress") ? "bg-violet-500"
    : s.includes("queue") || s.includes("pend") ? "bg-amber-500"
    : "bg-zinc-500";
  return <span className={`inline-block size-2 rounded-full ${color}`} />;
}

/** Card surface with optional title header. */
export function Card({ title, actions, children, className = "" }: { title?: ReactNode; actions?: ReactNode; children: ReactNode; className?: string }) {
  return (
    <section className={`rounded-xl border border-line bg-panel ${className}`}>
      {(title || actions) && (
        <div className="flex items-center justify-between border-b border-line px-4 py-3">
          {title && <div className="text-sm font-semibold">{title}</div>}
          {actions}
        </div>
      )}
      <div className="p-4">{children}</div>
    </section>
  );
}

export function SecondaryButton({ children, icon: Icon, onClick }: { children: ReactNode; icon?: LucideIcon; onClick?: () => void }) {
  return (
    <button onClick={onClick} className="inline-flex items-center gap-2 rounded-md border border-line bg-panel px-3 py-1.5 text-sm text-ink transition-colors hover:bg-hover">
      {Icon && <Icon size={15} />} {children}
    </button>
  );
}

/** Initials avatar disc. */
export function Avatar({ name, size = 24 }: { name?: string; size?: number }) {
  const initials = (name ?? "?").split(/[\s/_-]+/).slice(0, 2).map((w) => w[0]?.toUpperCase()).join("");
  return (
    <span className="grid place-items-center rounded-full bg-violet-500/80 font-semibold text-white"
      style={{ width: size, height: size, fontSize: size * 0.42 }}>
      {initials}
    </span>
  );
}

/** Vertical sub-navigation (settings panes, etc.). */
export function SubNav({ items }: { items: { label: string; to: string; icon?: LucideIcon }[] }) {
  return (
    <nav className="w-56 shrink-0 space-y-0.5">
      {items.map((it) => (
        <NavLink key={it.to} to={it.to} end
          className={({ isActive }) =>
            `flex items-center gap-2.5 rounded-md px-3 py-2 text-sm transition-colors ${
              isActive ? "bg-active text-ink" : "text-ink-dim hover:bg-hover hover:text-ink"}`}>
          {it.icon && <it.icon size={15} />} {it.label}
        </NavLink>
      ))}
    </nav>
  );
}

/** Modal dialog with an overlay, title bar, body, and footer actions. Closes on overlay click / Esc. */
export function Modal({ title, onClose, children, footer }: { title: string; onClose: () => void; children: ReactNode; footer?: ReactNode }) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [onClose]);
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4" onClick={onClose}>
      <div className="w-full max-w-md overflow-hidden rounded-xl border border-line bg-panel shadow-2xl" onClick={(e) => e.stopPropagation()}>
        <div className="flex items-center justify-between border-b border-line px-4 py-3">
          <h2 className="text-sm font-semibold">{title}</h2>
          <button onClick={onClose} className="rounded p-1 text-ink-faint hover:bg-hover hover:text-ink"><X size={16} /></button>
        </div>
        <div className="space-y-3 p-4">{children}</div>
        {footer && <div className="flex justify-end gap-2 border-t border-line px-4 py-3">{footer}</div>}
      </div>
    </div>
  );
}

/** Labeled text input / select for modal forms. */
export function Field({ label, value, onChange, placeholder, options }: {
  label: string; value: string; onChange: (v: string) => void; placeholder?: string; options?: string[];
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-xs font-medium text-ink-dim">{label}</span>
      {options ? (
        <select value={value} onChange={(e) => onChange(e.target.value)}
          className="w-full rounded-md border border-line bg-bg px-2.5 py-1.5 text-sm outline-none focus:border-brand">
          {options.map((o) => <option key={o} value={o}>{o}</option>)}
        </select>
      ) : (
        <input value={value} onChange={(e) => onChange(e.target.value)} placeholder={placeholder}
          className="w-full rounded-md border border-line bg-bg px-2.5 py-1.5 text-sm outline-none placeholder:text-ink-faint focus:border-brand" />
      )}
    </label>
  );
}

/** Button + click-away dropdown menu of actions. */
export function Dropdown({ trigger, items }: { trigger: ReactNode; items: { label: string; icon?: LucideIcon; onSelect: () => void; danger?: boolean }[] }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const onDoc = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false); };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, []);
  return (
    <div className="relative" ref={ref}>
      <div onClick={() => setOpen((v) => !v)}>{trigger}</div>
      {open && (
        <div className="absolute right-0 z-40 mt-1 w-48 overflow-hidden rounded-lg border border-line bg-panel py-1 shadow-xl">
          {items.map((it) => (
            <button key={it.label} onClick={() => { setOpen(false); it.onSelect(); }}
              className={`flex w-full items-center gap-2.5 px-3 py-1.5 text-sm hover:bg-hover ${it.danger ? "text-red-400" : "text-ink-dim hover:text-ink"}`}>
              {it.icon && <it.icon size={15} />} {it.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

/** Two-column key/value definition row. */
export function KeyValue({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex items-start gap-6 py-2.5">
      <div className="w-32 shrink-0 text-sm font-medium text-ink-dim">{label}</div>
      <div className="text-sm">{children}</div>
    </div>
  );
}

function inline(text: string): ReactNode {
  // Minimal inline markdown: `code` and **bold**.
  const parts = text.split(/(`[^`]+`|\*\*[^*]+\*\*)/g);
  return parts.map((p, i) => {
    if (p.startsWith("`") && p.endsWith("`")) return <code key={i} className="rounded bg-bg px-1 text-[0.85em]">{p.slice(1, -1)}</code>;
    if (p.startsWith("**") && p.endsWith("**")) return <strong key={i}>{p.slice(2, -2)}</strong>;
    return p;
  });
}

/** Minimal Markdown renderer (headings, code fences, lists, tables, paragraphs) — enough for READMEs. */
export function Markdown({ source }: { source: string }) {
  const lines = source.replace(/\r/g, "").split("\n");
  const out: ReactNode[] = [];
  let i = 0, key = 0;
  while (i < lines.length) {
    const line = lines[i];
    if (line.startsWith("```")) {
      const buf: string[] = [];
      i++;
      while (i < lines.length && !lines[i].startsWith("```")) buf.push(lines[i++]);
      i++;
      out.push(<pre key={key++} className="my-3 overflow-auto rounded-md bg-bg p-3 font-mono text-xs leading-relaxed">{buf.join("\n")}</pre>);
      continue;
    }
    const h = /^(#{1,4})\s+(.*)/.exec(line);
    if (h) {
      const lvl = h[1].length;
      const sizes = ["text-2xl", "text-xl", "text-lg", "text-base"];
      out.push(<div key={key++} className={`mt-4 mb-2 font-semibold ${sizes[lvl - 1]}`}>{inline(h[2])}</div>);
      i++; continue;
    }
    if (line.startsWith("|")) {
      const rows: string[] = [];
      while (i < lines.length && lines[i].startsWith("|")) rows.push(lines[i++]);
      const cells = rows.map((r) => r.split("|").slice(1, -1).map((c) => c.trim()));
      const [head, , ...body] = cells; // skip the |---| separator
      out.push(
        <table key={key++} className="my-3 w-full border border-line text-sm">
          <thead><tr className="border-b border-line bg-panel">{head?.map((c, j) => <th key={j} className="px-3 py-1.5 text-left font-medium">{inline(c)}</th>)}</tr></thead>
          <tbody>{body.map((r, ri) => <tr key={ri} className="border-b border-line/60">{r.map((c, j) => <td key={j} className="px-3 py-1.5">{inline(c)}</td>)}</tr>)}</tbody>
        </table>,
      );
      continue;
    }
    if (/^[-*]\s+/.test(line)) {
      const items: string[] = [];
      while (i < lines.length && /^[-*]\s+/.test(lines[i])) items.push(lines[i++].replace(/^[-*]\s+/, ""));
      out.push(<ul key={key++} className="my-2 list-disc space-y-1 pl-6 text-sm">{items.map((it, j) => <li key={j}>{inline(it)}</li>)}</ul>);
      continue;
    }
    if (line.trim() === "") { i++; continue; }
    const para: string[] = [];
    while (i < lines.length && lines[i].trim() !== "" && !/^[#`|]|^[-*]\s/.test(lines[i])) para.push(lines[i++]);
    out.push(<p key={key++} className="my-2 text-sm leading-relaxed text-ink-dim">{inline(para.join(" "))}</p>);
  }
  return <div>{out}</div>;
}
