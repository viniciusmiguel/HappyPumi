import { Routes, Route, Navigate } from "react-router-dom";
import Layout from "./components/Layout";
import Dashboard from "./pages/Dashboard";
import Stacks from "./pages/Stacks";
import Registry from "./pages/Registry";
import Templates from "./pages/Templates";
import Members from "./pages/Members";
import Roles from "./pages/Roles";
import Policies from "./pages/Policies";
import Integrations from "./pages/Integrations";
import * as Empty from "./pages/empties";

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/stacks" element={<Stacks />} />
        <Route path="/deployments" element={<Empty.Deployments />} />
        <Route path="/resources" element={<Empty.Resources />} />
        <Route path="/environments" element={<Empty.Environments />} />
        <Route path="/policies" element={<Policies />} />
        <Route path="/neo" element={<Empty.Neo />} />

        <Route path="/platform/services" element={<Empty.Services />} />
        <Route path="/platform/components" element={<Empty.Components />} />
        <Route path="/platform/registry" element={<Registry />} />
        <Route path="/platform/templates" element={<Templates />} />

        <Route path="/management/approvals" element={<Empty.Approvals />} />
        <Route path="/management/accounts" element={<Empty.Accounts />} />
        <Route path="/management/version-control" element={<Empty.VersionControl />} />
        <Route path="/management/policies" element={<Policies />} />
        <Route path="/management/audit-logs" element={<Empty.AuditLogs />} />

        <Route path="/settings/members" element={<Members />} />
        <Route path="/settings/teams" element={<Empty.Teams />} />
        <Route path="/settings/roles" element={<Roles />} />
        <Route path="/settings/identity" element={<Empty.Identity />} />
        <Route path="/settings/integrations" element={<Integrations />} />
        <Route path="/settings/billing" element={<Empty.Billing />} />

        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Route>
    </Routes>
  );
}
