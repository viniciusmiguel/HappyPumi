import { Routes, Route, Navigate, useLocation } from "react-router-dom";
import { isAuthenticated } from "./lib/auth";
import Layout from "./components/Layout";
import Login from "./pages/Login";
import Callback from "./pages/Callback";
import Dashboard from "./pages/Dashboard";
import Stacks from "./pages/Stacks";
import StackDetail from "./pages/StackDetail";
import Environments from "./pages/Environments";
import EnvironmentDetail from "./pages/EnvironmentDetail";
import Deployments from "./pages/Deployments";
import DeploymentDetail from "./pages/DeploymentDetail";
import Components from "./pages/Components";
import ComponentDetail from "./pages/ComponentDetail";
import Registry from "./pages/Registry";
import Templates from "./pages/Templates";
import Members from "./pages/Members";
import Roles from "./pages/Roles";
import Policies from "./pages/Policies";
import Integrations from "./pages/Integrations";
import * as Empty from "./pages/empties";

/** Redirects to /login (remembering where you were) when there's no stored token. */
function RequireAuth({ children }: { children: React.ReactNode }) {
  const location = useLocation();
  if (!isAuthenticated())
    return <Navigate to="/login" replace state={{ from: location }} />;
  return <>{children}</>;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/callback" element={<Callback />} />
      <Route element={<RequireAuth><Layout /></RequireAuth>}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<Dashboard />} />

        <Route path="/stacks" element={<Stacks />} />
        <Route path="/stacks/:project/:stack" element={<StackDetail />} />

        <Route path="/environments" element={<Environments />} />
        <Route path="/environments/:project/:name" element={<EnvironmentDetail />} />

        <Route path="/deployments" element={<Deployments />} />
        <Route path="/deployments/:project/:stack/:version" element={<DeploymentDetail />} />

        <Route path="/policy-findings" element={<Policies />} />

        <Route path="/platform/services" element={<Empty.Services />} />
        <Route path="/platform/components" element={<Components />} />
        <Route path="/platform/components/:source/:publisher/:name" element={<ComponentDetail />} />
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
