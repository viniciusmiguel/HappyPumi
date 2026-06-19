import { useEffect, useState } from "react";
import { Outlet } from "react-router-dom";
import { PanelLeftOpen } from "lucide-react";
import Sidebar from "./Sidebar";
import { api, type CurrentUser } from "../lib/api";

export default function Layout() {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [collapsed, setCollapsed] = useState(false);

  useEffect(() => { api.currentUser().then(setUser); }, []);

  return (
    <div className="flex h-screen overflow-hidden">
      {collapsed ? (
        <div className="flex w-12 shrink-0 flex-col items-center border-r border-line bg-bg py-3">
          <button onClick={() => setCollapsed(false)} className="rounded p-1.5 text-ink-faint hover:bg-hover hover:text-ink" title="Expand">
            <PanelLeftOpen size={16} />
          </button>
        </div>
      ) : (
        <Sidebar user={user} onCollapse={() => setCollapsed(true)} />
      )}
      <main className="flex-1 overflow-y-auto">
        <Outlet context={{ user }} />
      </main>
    </div>
  );
}
