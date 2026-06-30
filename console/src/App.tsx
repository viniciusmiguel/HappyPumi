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
import EscProviders from "./pages/EscProviders";
import Deployments from "./pages/Deployments";
import DeploymentDetail from "./pages/DeploymentDetail";
import Components from "./pages/Components";
import ComponentDetail from "./pages/ComponentDetail";
import Registry from "./pages/Registry";
import Templates from "./pages/Templates";
import Members from "./pages/Members";
import Roles from "./pages/Roles";
import Teams from "./pages/Teams";
import Policies from "./pages/Policies";
import PolicyFindings from "./pages/PolicyFindings";
import Integrations from "./pages/Integrations";
import Services from "./pages/Services";
import Approvals from "./pages/Approvals";
import Accounts from "./pages/Accounts";
import VersionControl from "./pages/VersionControl";
import AuditLogs from "./pages/AuditLogs";
import Identity from "./pages/Identity";
import Billing from "./pages/Billing";
import Webhooks from "./pages/Webhooks";
import AccessTokens from "./pages/AccessTokens";
import OidcIssuers from "./pages/OidcIssuers";
import CloudAccounts from "./pages/CloudAccounts";
import SamlSso from "./pages/SamlSso";
import EncryptionKeys from "./pages/EncryptionKeys";
import ChangeGates from "./pages/ChangeGates";
import ChangeRequests from "./pages/ChangeRequests";

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
        <Route path="/environments/providers" element={<EscProviders />} />
        <Route path="/environments/:project/:name" element={<EnvironmentDetail />} />

        <Route path="/deployments" element={<Deployments />} />
        <Route path="/deployments/:project/:stack/:version" element={<DeploymentDetail />} />

        <Route path="/policy-findings" element={<PolicyFindings />} />

        <Route path="/platform/services" element={<Services />} />
        <Route path="/platform/components" element={<Components />} />
        <Route path="/platform/components/:source/:publisher/:name" element={<ComponentDetail />} />
        <Route path="/platform/registry" element={<Registry />} />
        <Route path="/platform/templates" element={<Templates />} />

        <Route path="/management/approvals" element={<Approvals />} />
        <Route path="/management/change-requests" element={<ChangeRequests />} />
        <Route path="/management/accounts" element={<Accounts />} />
        <Route path="/management/version-control" element={<VersionControl />} />
        <Route path="/management/policies" element={<Policies />} />
        <Route path="/management/audit-logs" element={<AuditLogs />} />

        <Route path="/settings/members" element={<Members />} />
        <Route path="/settings/teams" element={<Teams />} />
        <Route path="/settings/roles" element={<Roles />} />
        <Route path="/settings/identity" element={<Identity />} />
        <Route path="/settings/oidc-issuers" element={<OidcIssuers />} />
        <Route path="/settings/cloud-accounts" element={<CloudAccounts />} />
        <Route path="/settings/saml" element={<SamlSso />} />
        <Route path="/settings/integrations" element={<Integrations />} />
        <Route path="/settings/webhooks" element={<Webhooks />} />
        <Route path="/settings/tokens" element={<AccessTokens />} />
        <Route path="/settings/encryption-keys" element={<EncryptionKeys />} />
        <Route path="/settings/change-gates" element={<ChangeGates />} />
        <Route path="/settings/billing" element={<Billing />} />

        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Route>
    </Routes>
  );
}
