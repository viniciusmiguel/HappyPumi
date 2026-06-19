// Empty-state pages for surfaces HappyPumi doesn't back with data yet (matching the console's empty states).
import {
  Server, Box, Rocket, Search, CheckSquare, Cloud, GitBranch, ClipboardList,
  UsersRound, Fingerprint, CreditCard, Bot, Plus,
} from "lucide-react";
import { PageHeader, EmptyState, PrimaryButton } from "../components/ui";

function Page({ icon, title, empty }: { icon: typeof Server; title: string; empty: React.ReactNode }) {
  return <div><PageHeader icon={icon} title={title} />{empty}</div>;
}

export const Services = () => (
  <Page icon={Server} title="Services" empty={
    <EmptyState icon={Server} title="Create your first service"
      description="Services let you group Stacks, Environments and other resources in a way that makes sense to your organization."
      action={<PrimaryButton icon={Plus}>Create a new service</PrimaryButton>} />} />
);

export const Components = () => (
  <Page icon={Box} title="Private components" empty={
    <EmptyState icon={Box} title="No private components"
      description="Publish reusable components to your organization's private registry." />} />
);

export const Deployments = () => (
  <Page icon={Rocket} title="Deployments" empty={
    <EmptyState icon={Rocket} title="No deployments yet"
      description="Run a deployment from Git, the REST API, or click-to-deploy. They'll show up here." />} />
);

export const Environments = () => (
  <Page icon={Box} title="Environments" empty={
    <EmptyState icon={Box} title="No environments yet"
      description="Pulumi ESC environments compose secrets and configuration for your stacks and tools."
      action={<PrimaryButton icon={Plus}>Create environment</PrimaryButton>} />} />
);

export const Resources = () => (
  <Page icon={Search} title="Resources" empty={
    <EmptyState icon={Search} title="Search your resources"
      description="Find resources across every stack in your organization with Pulumi Insights." />} />
);

export const Approvals = () => (
  <Page icon={CheckSquare} title="Approvals" empty={
    <EmptyState icon={CheckSquare} title="No change requests"
      description="Review and approve infrastructure changes before they're applied." />} />
);

export const Accounts = () => (
  <Page icon={Cloud} title="Accounts" empty={
    <EmptyState icon={Cloud} title="No cloud accounts"
      description="Connect cloud accounts to scan resources and detect drift." action={<PrimaryButton icon={Plus}>Add account</PrimaryButton>} />} />
);

export const VersionControl = () => (
  <Page icon={GitBranch} title="Version control" empty={
    <EmptyState icon={GitBranch} title="Connect your version control system"
      description="You don't have any version control accounts yet. Combine Pulumi with your VCS to enable pull request comments, policy enforcement, and drift detection."
      action={<PrimaryButton icon={Plus}>Add account</PrimaryButton>} />} />
);

export const AuditLogs = () => (
  <Page icon={ClipboardList} title="Audit logs" empty={
    <EmptyState icon={ClipboardList} title="No audit events"
      description="Every infrastructure-changing action in your organization is recorded here." />} />
);

export const Teams = () => (
  <Page icon={UsersRound} title="Teams" empty={
    <EmptyState icon={UsersRound} title="No teams"
      description="Group members into teams and assign roles to manage access at scale."
      action={<PrimaryButton icon={Plus}>New team</PrimaryButton>} />} />
);

export const Identity = () => (
  <Page icon={Fingerprint} title="Identity providers" empty={
    <EmptyState icon={Fingerprint} title="No identity providers"
      description="Configure SAML/SCIM or OIDC issuers for single sign-on and provisioning." />} />
);

export const Billing = () => (
  <Page icon={CreditCard} title="Billing & usage" empty={
    <EmptyState icon={CreditCard} title="Usage & billing"
      description="Track resource hours, deployment minutes, and your subscription here." />} />
);

export const Neo = () => (
  <Page icon={Bot} title="Neo" empty={
    <EmptyState icon={Bot} title="Start a Neo agent task"
      description="Neo is your AI agent for cloud management. Describe a task and let it plan and execute."
      action={<PrimaryButton icon={Plus}>New task</PrimaryButton>} />} />
);
