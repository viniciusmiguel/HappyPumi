import { useState } from "react";
import { NavLink, useLocation } from "react-router-dom";
import {
  ChevronLeft, ChevronDown, Plus, PanelLeftClose,
  Settings as SettingsIcon, LogOut, BookOpen, CreditCard, UserPlus,
} from "lucide-react";
import { homeItems, sections, sectionForPath, type NavItem, type SectionId } from "../lib/nav";
import { type CurrentUser } from "../lib/api";

function NavRow({ item, onClick }: { item: NavItem; onClick?: () => void }) {
  const Icon = item.icon;
  return (
    <NavLink
      to={item.to}
      onClick={onClick}
      className={({ isActive }) =>
        `flex items-center gap-2.5 rounded-md px-2.5 py-1.5 text-sm transition-colors ${
          isActive ? "bg-active text-ink" : "text-ink-dim hover:bg-hover hover:text-ink"
        }`
      }
    >
      <Icon size={16} className="shrink-0" />
      <span className="truncate">{item.label}</span>
    </NavLink>
  );
}

function SectionLauncher({ id, open }: { id: SectionId; open: (s: SectionId) => void }) {
  const s = sections.find((x) => x.id === id)!;
  const Icon = s.icon;
  return (
    <button
      onClick={() => open(s.id)}
      className="flex w-full items-center gap-2.5 rounded-md px-2.5 py-1.5 text-sm text-ink-dim transition-colors hover:bg-hover hover:text-ink"
    >
      <Icon size={16} className="shrink-0" />
      <span className="flex-1 truncate text-left">{s.title}</span>
      <ChevronDown size={14} className="-rotate-90 text-ink-faint" />
    </button>
  );
}

export default function Sidebar({ user, onCollapse }: { user: CurrentUser | null; onCollapse: () => void }) {
  const location = useLocation();
  const [section, setSection] = useState<SectionId | null>(() => sectionForPath(location.pathname));
  const [menuOpen, setMenuOpen] = useState(false);

  // Open the matching drill-in section when navigating directly to one of its routes. Re-derived during
  // render (React's "store info from previous render" pattern) rather than in an effect to avoid a
  // cascading re-render on every navigation.
  const [prevPath, setPrevPath] = useState(location.pathname);
  if (prevPath !== location.pathname) {
    setPrevPath(location.pathname);
    setSection(sectionForPath(location.pathname));
  }

  const orgs = user?.organizations ?? [{ githubLogin: user?.githubLogin ?? "happypumi" }];
  const org = orgs[0]?.name ?? orgs[0]?.githubLogin ?? "happypumi";
  const active = section ? sections.find((s) => s.id === section)! : null;

  return (
    <aside className="flex h-full w-64 shrink-0 flex-col border-r border-line bg-bg">
      {/* brand + collapse */}
      <div className="flex items-center justify-between px-3 py-3">
        <div className="flex items-center gap-2 px-1">
          <div className="grid size-6 place-items-center rounded bg-gradient-to-br from-violet-500 to-fuchsia-500 text-[11px] font-bold text-white">P</div>
          <span className="text-sm font-semibold tracking-tight">HappyPumi</span>
        </div>
        <button onClick={onCollapse} className="rounded p-1 text-ink-faint hover:bg-hover hover:text-ink" title="Collapse">
          <PanelLeftClose size={16} />
        </button>
      </div>

      {/* org switcher */}
      <div className="px-3">
        <button className="flex w-full items-center gap-2 rounded-md border border-line bg-panel px-2.5 py-2 text-sm hover:bg-hover">
          <span className="grid size-5 place-items-center rounded-full bg-amber-400 text-[10px] font-bold text-black">
            {org[0]?.toUpperCase()}
          </span>
          <span className="flex-1 truncate text-left">{org}</span>
          <ChevronDown size={14} className="text-ink-faint" />
        </button>
      </div>

      {/* primary action */}
      <div className="px-3 py-3">
        <button className="flex w-full items-center justify-center gap-2 rounded-md bg-brand px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-hover">
          <Plus size={16} /> New task
        </button>
      </div>

      {/* nav (home set, or a drilled-in section) */}
      <nav className="flex-1 space-y-0.5 overflow-y-auto px-3 pb-3">
        {active ? (
          <>
            <button
              onClick={() => setSection(null)}
              className="mb-1 flex items-center gap-2 rounded-md px-2.5 py-1.5 text-sm font-medium text-ink hover:bg-hover"
            >
              <ChevronLeft size={16} /> Back
            </button>
            <div className="px-2.5 pb-1 pt-2 text-[11px] font-semibold uppercase tracking-wider text-ink-faint">
              {active.title}
            </div>
            {active.items.map((i) => <NavRow key={i.to} item={i} />)}
          </>
        ) : (
          <>
            {homeItems.map((i) => <NavRow key={i.to} item={i} />)}
            <div className="my-2 border-t border-line" />
            {sections.map((s) => <SectionLauncher key={s.id} id={s.id} open={setSection} />)}
          </>
        )}
      </nav>

      {/* user menu */}
      <div className="relative border-t border-line p-3">
        {menuOpen && (
          <div className="absolute bottom-14 left-3 right-3 overflow-hidden rounded-lg border border-line bg-panel py-1 shadow-xl">
            {[
              { label: "Account settings", icon: SettingsIcon },
              { label: "Invite members", icon: UserPlus },
              { label: "Billing & usage", icon: CreditCard },
              { label: "Docs", icon: BookOpen },
              { label: "Sign out", icon: LogOut },
            ].map((m) => (
              <button key={m.label} className="flex w-full items-center gap-2.5 px-3 py-1.5 text-sm text-ink-dim hover:bg-hover hover:text-ink">
                <m.icon size={15} /> {m.label}
              </button>
            ))}
          </div>
        )}
        <button onClick={() => setMenuOpen((v) => !v)} className="flex w-full items-center gap-2 rounded-md px-1.5 py-1.5 hover:bg-hover">
          <span className="grid size-7 place-items-center rounded-full bg-amber-400 text-xs font-bold text-black">
            {org[0]?.toUpperCase()}
          </span>
          <span className="flex-1 truncate text-left text-sm">{user?.name ?? user?.githubLogin ?? "happypumi"}</span>
          <ChevronDown size={14} className="text-ink-faint" />
        </button>
      </div>
    </aside>
  );
}
