// Sidebar information architecture, mirroring the Pulumi Console: a top-level "home" set plus drill-in
// sections (Platform / Management / Settings) reached via launchers and exited with "Back".
import type { LucideIcon } from "lucide-react";
import {
  LayoutDashboard, Layers, Rocket, Search, Box, ShieldCheck, Bot,
  Server, Package, LayoutTemplate,
  CheckSquare, Cloud, GitBranch, ClipboardList,
  Users, UsersRound, KeyRound, Fingerprint, Plug, CreditCard, Settings,
  ChevronRight,
} from "lucide-react";

export interface NavItem { label: string; to: string; icon: LucideIcon; }
export interface NavSection { id: SectionId; title: string; icon: LucideIcon; items: NavItem[]; }
export type SectionId = "platform" | "management" | "settings";

export const homeItems: NavItem[] = [
  { label: "Dashboard", to: "/dashboard", icon: LayoutDashboard },
  { label: "Stacks", to: "/stacks", icon: Layers },
  { label: "Deployments", to: "/deployments", icon: Rocket },
  { label: "Resources", to: "/resources", icon: Search },
  { label: "Environments", to: "/environments", icon: Box },
  { label: "Policies", to: "/policies", icon: ShieldCheck },
  { label: "Neo", to: "/neo", icon: Bot },
];

export const sections: NavSection[] = [
  {
    id: "platform", title: "Platform", icon: Server,
    items: [
      { label: "Services", to: "/platform/services", icon: Server },
      { label: "Private components", to: "/platform/components", icon: Box },
      { label: "Registry", to: "/platform/registry", icon: Package },
      { label: "Templates", to: "/platform/templates", icon: LayoutTemplate },
    ],
  },
  {
    id: "management", title: "Management", icon: ClipboardList,
    items: [
      { label: "Approvals", to: "/management/approvals", icon: CheckSquare },
      { label: "Accounts", to: "/management/accounts", icon: Cloud },
      { label: "Version control", to: "/management/version-control", icon: GitBranch },
      { label: "Policies", to: "/management/policies", icon: ShieldCheck },
      { label: "Audit logs", to: "/management/audit-logs", icon: ClipboardList },
    ],
  },
  {
    id: "settings", title: "Access management", icon: Settings,
    items: [
      { label: "Members", to: "/settings/members", icon: Users },
      { label: "Teams", to: "/settings/teams", icon: UsersRound },
      { label: "Roles", to: "/settings/roles", icon: KeyRound },
      { label: "Identity providers", to: "/settings/identity", icon: Fingerprint },
      { label: "Integrations", to: "/settings/integrations", icon: Plug },
      { label: "Billing & usage", to: "/settings/billing", icon: CreditCard },
    ],
  },
];

export const launcherIcon = ChevronRight;

/** The section a route belongs to, or null for the home set (controls which sidebar view opens). */
export function sectionForPath(path: string): SectionId | null {
  return sections.find((s) => s.items.some((i) => path.startsWith(i.to)))?.id ?? null;
}
