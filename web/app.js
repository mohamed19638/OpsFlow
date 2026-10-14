const app = document.querySelector("#app");
const dialog = document.querySelector("#detail-dialog");
const dialogTitle = document.querySelector("#dialog-title");
const dialogContent = document.querySelector("#detail-content");
const tokenKey = "opsflow.accessToken";
const refreshKey = "opsflow.refreshToken";
const apiRoot = "/api";

const state = {
  token: sessionStorage.getItem(tokenKey),
  refreshToken: sessionStorage.getItem(refreshKey),
  user: null,
  organizationId: null,
  page: "overview",
  search: "",
  data: [],
  loading: false,
  loadError: "",
  explorerIndex: 0,
  explorerResponse: null,
  explorerPath: "",
  explorerQuery: "",
  explorerBody: "",
  formSubmit: null,
  authEmail: "admin@acmetech.io",
};

const NAV = [
  ["overview", "Overview", "⌂"],
  ["organizations", "Organizations", "▦"],
  ["teams", "Teams", "♧"],
  ["services", "Services", "◈"],
  ["incidents", "Incidents", "◉"],
  ["deployments", "Deployments", "↗"],
  ["users", "Users & access", "＋"],
  ["audit", "Audit log", "≡"],
  ["integrations", "Integrations", "⇄"],
  ["explorer", "API Explorer", "</>"],
];

const ENUMS = {
  roles: ["Admin", "TeamLead", "Engineer", "Viewer"],
  serviceStatuses: ["Operational", "Degraded", "PartialOutage", "MajorOutage", "Maintenance"],
  severities: ["Critical", "High", "Medium", "Low"],
  incidentStatuses: ["Detected", "Investigating", "Identified", "Mitigating", "Resolved", "Postmortem", "Closed"],
  environments: ["Development", "Staging", "Production"],
  deploymentStatuses: ["Pending", "InProgress", "Succeeded", "Failed", "RolledBack"],
};

// This catalog mirrors the actual API controllers. There is no Instance controller or route.
const API_ENDPOINTS = [
  { group: "System", method: "GET", path: "/health", access: "Public", note: "Call the API health-check endpoint registered by the application." },
  { group: "Authentication", method: "POST", path: "/auth/login", access: "Public", note: "Exchange email and password for access and refresh tokens.", body: { email: "admin@acmetech.io", password: "your-password" } },
  { group: "Authentication", method: "POST", path: "/auth/register", access: "Admin", note: "Create an organization user. OrganizationId is required.", body: { email: "person@example.com", password: "Temporary@123", fullName: "New User", role: "Engineer", organizationId: "{orgId}" } },
  { group: "Authentication", method: "POST", path: "/auth/self-register", access: "Development only", note: "Create a local demo account with the Engineer role when exactly one organization exists.", body: { email: "person@example.com", password: "Temporary@123", fullName: "New User" } },
  { group: "Authentication", method: "POST", path: "/auth/refresh-token", access: "Public", note: "Rotate an access/refresh token pair.", body: { accessToken: "{accessToken}", refreshToken: "{refreshToken}" } },
  { group: "Authentication", method: "POST", path: "/auth/revoke-token", access: "Signed in", note: "Revoke the supplied refresh token.", body: { refreshToken: "{refreshToken}" } },

  { group: "Organizations", method: "GET", path: "/organizations", access: "Admin", note: "List organizations." },
  { group: "Organizations", method: "GET", path: "/organizations/{id}", access: "Admin or TeamLead", note: "Get one organization by ID." },
  { group: "Organizations", method: "POST", path: "/organizations", access: "Admin", note: "Create an organization.", body: { name: "Example Org", description: "Organization description" } },
  { group: "Organizations", method: "PUT", path: "/organizations/{id}", access: "Admin", note: "Replace organization name and description.", body: { name: "Example Org", description: "Updated description" } },
  { group: "Organizations", method: "DELETE", path: "/organizations/{id}", access: "Admin", note: "Delete an organization." },

  { group: "Teams", method: "GET", path: "/organizations/{orgId}/teams", access: "Signed in", note: "List teams in the organization." },
  { group: "Teams", method: "GET", path: "/organizations/{orgId}/teams/{id}", access: "Signed in", note: "Get one team by ID." },
  { group: "Teams", method: "POST", path: "/organizations/{orgId}/teams", access: "Admin or TeamLead", note: "Create a team.", body: { name: "Platform", description: "Platform engineering" } },
  { group: "Teams", method: "PUT", path: "/organizations/{orgId}/teams/{id}", access: "Admin or TeamLead", note: "Update a team.", body: { name: "Platform", description: "Updated description" } },
  { group: "Teams", method: "DELETE", path: "/organizations/{orgId}/teams/{id}", access: "Admin", note: "Delete a team." },
  { group: "Teams", method: "GET", path: "/organizations/{orgId}/teams/{id}/members", access: "Signed in", note: "List team members." },
  { group: "Teams", method: "POST", path: "/organizations/{orgId}/teams/{id}/members", access: "Admin or TeamLead", note: "Add a user to a team.", body: { userId: "{userId}" } },
  { group: "Teams", method: "DELETE", path: "/organizations/{orgId}/teams/{id}/members/{userId}", access: "Admin or TeamLead", note: "Remove a team member." },

  { group: "Services", method: "GET", path: "/organizations/{orgId}/services", access: "Signed in", note: "List organization services. Uses the Redis-backed service-list operation." },
  { group: "Services", method: "GET", path: "/organizations/{orgId}/services/{id}", access: "Signed in", note: "Get one service by ID." },
  { group: "Services", method: "POST", path: "/organizations/{orgId}/services", access: "Admin or TeamLead", note: "Create a service.", body: { name: "Payments API", description: "Payment processing", teamId: null } },
  { group: "Services", method: "PUT", path: "/organizations/{orgId}/services/{id}", access: "Admin or TeamLead", note: "Update service name, description, or team.", body: { name: "Payments API", description: "Updated description", teamId: null } },
  { group: "Services", method: "DELETE", path: "/organizations/{orgId}/services/{id}", access: "Admin", note: "Delete a service." },
  { group: "Services", method: "PATCH", path: "/organizations/{orgId}/services/{id}/status", access: "Admin, TeamLead, or Engineer", note: "Change service health status.", body: { status: "Degraded" } },

  { group: "Incidents", method: "GET", path: "/organizations/{orgId}/incidents", access: "Signed in", note: "List incidents. Optional query: severity, status, serviceId." },
  { group: "Incidents", method: "GET", path: "/organizations/{orgId}/incidents/{id}", access: "Signed in", note: "Get incident details, timeline events, and comments." },
  { group: "Incidents", method: "POST", path: "/organizations/{orgId}/incidents", access: "Admin, TeamLead, or Engineer", note: "Open an incident.", body: { title: "API latency", description: "Latency increased", severity: "High", serviceId: "{serviceId}" } },
  { group: "Incidents", method: "PATCH", path: "/organizations/{orgId}/incidents/{id}/status", access: "Admin, TeamLead, or Engineer", note: "Request a status transition. The backend validates lifecycle rules.", body: { status: "Investigating" } },
  { group: "Incidents", method: "PATCH", path: "/organizations/{orgId}/incidents/{id}/severity", access: "Admin or TeamLead", note: "Change severity.", body: { severity: "Critical" } },
  { group: "Incidents", method: "POST", path: "/organizations/{orgId}/incidents/{id}/assign", access: "Admin or TeamLead", note: "Assign to a user OR a team; send both null to unassign.", body: { userId: null, teamId: "{teamId}" } },
  { group: "Incidents", method: "POST", path: "/organizations/{orgId}/incidents/{id}/comments", access: "Admin, TeamLead, or Engineer", note: "Add an incident comment.", body: { body: "We are investigating the alert." } },
  { group: "Incidents", method: "GET", path: "/organizations/{orgId}/incidents/{id}/timeline", access: "Signed in", note: "Get incident timeline events." },
  { group: "Incidents", method: "POST", path: "/organizations/{orgId}/incidents/{id}/resolve", access: "Admin, TeamLead, or Engineer", note: "Resolve with resolution notes.", body: { resolutionNotes: "Rolled back the faulty release." } },
  { group: "Incidents", method: "POST", path: "/organizations/{orgId}/incidents/{id}/reopen", access: "Admin or TeamLead", note: "Reopen a resolved incident." },
  { group: "Incidents", method: "POST", path: "/organizations/{orgId}/incidents/{id}/postmortem", access: "Admin or TeamLead", note: "Add a postmortem.", body: { body: "Root cause and follow-up actions..." } },

  { group: "Deployments", method: "GET", path: "/organizations/{orgId}/deployments", access: "Signed in", note: "List deployments. Optional query: serviceId, environment." },
  { group: "Deployments", method: "GET", path: "/organizations/{orgId}/deployments/{id}", access: "Signed in", note: "Get one deployment by ID." },
  { group: "Deployments", method: "POST", path: "/organizations/{orgId}/deployments", access: "Admin, TeamLead, or Engineer", note: "Record a deployment.", body: { serviceId: "{serviceId}", version: "v1.2.3", environment: "Staging" } },
  { group: "Deployments", method: "PATCH", path: "/organizations/{orgId}/deployments/{id}/status", access: "Admin, TeamLead, or Engineer", note: "Update deployment status.", body: { status: "Succeeded" } },

  { group: "Users", method: "GET", path: "/users", access: "Admin", note: "List users." },
  { group: "Users", method: "GET", path: "/users/{id}", access: "Admin or the same user", note: "Get a user by ID." },
  { group: "Users", method: "GET", path: "/users/me", access: "Signed in", note: "Get the current user profile." },
  { group: "Users", method: "PUT", path: "/users/{id}/role", access: "Admin", note: "Update a user's role.", body: { role: "Engineer" } },

  { group: "Audit log", method: "GET", path: "/organizations/{orgId}/audit-logs", access: "Admin", note: "List organization audit records." },
];

function esc(value) {
  return String(value ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
}

function decodeToken(token) {
  try {
    let payload = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    payload += "=".repeat((4 - payload.length % 4) % 4);
    return JSON.parse(decodeURIComponent(Array.from(atob(payload), (c) => "%" + c.charCodeAt(0).toString(16).padStart(2, "0")).join("")));
  } catch { return null; }
}

function orgIdFromToken(token) {
  const claims = decodeToken(token);
  return claims?.orgId ?? null;
}

function isAdmin() { return state.user?.role === "Admin"; }
function canWrite(roleNames) { return roleNames.includes(state.user?.role); }
function orgPath(resource) { return "/organizations/" + encodeURIComponent(state.organizationId) + "/" + resource; }

async function apiRequest(path, options = {}) {
  const headers = { ...(options.body !== undefined ? { "Content-Type": "application/json" } : {}), ...(state.token ? { Authorization: "Bearer " + state.token } : {}), ...(options.headers || {}) };
  const response = await fetch(apiRoot + path, { ...options, headers });
  const text = await response.text();
  let data = null;
  if (text) {
    try { data = JSON.parse(text); } catch { data = text; }
  }
  if (!response.ok) {
    const message = typeof data === "object" && data ? (data.detail || data.title || data.message || JSON.stringify(data)) : (data || "Request failed");
    const error = new Error(message + " (" + response.status + ")");
    error.status = response.status;
    error.response = data;
    throw error;
  }
  return { status: response.status, data };
}

async function apiData(path, options) {
  return (await apiRequest(path, options)).data;
}

function saveTokens(tokens) {
  state.token = tokens.accessToken;
  state.refreshToken = tokens.refreshToken;
  state.organizationId = orgIdFromToken(tokens.accessToken);
  sessionStorage.setItem(tokenKey, tokens.accessToken);
  sessionStorage.setItem(refreshKey, tokens.refreshToken);
}

function clearSession() {
  state.token = null; state.refreshToken = null; state.user = null; state.organizationId = null;
  sessionStorage.removeItem(tokenKey); sessionStorage.removeItem(refreshKey);
}

function authLayout(card) {
  app.innerHTML = `<section class="auth-layout">
    <header class="auth-brand"><span class="brand-mark">O</span><span>OpsFlow</span><span class="auth-brand-note">Operations workspace</span></header>
    <div class="auth-content">${card}</div>
    <footer class="auth-footer">Local development workspace · Your data stays in this OpsFlow environment</footer>
  </section>`;
}

function showLogin(message = "", email = state.authEmail) {
  state.authEmail = email || "";
  authLayout(`<form class="auth-card" id="login-form" novalidate>
    <div class="eyebrow">Welcome back</div><h1>Sign in</h1><p class="auth-description">Use your OpsFlow account to open the workspace.</p>
    <label class="field"><span>Email address</span><input name="email" type="email" autocomplete="username" value="${esc(state.authEmail)}" required></label>
    <label class="field"><span>Password</span><input name="password" type="password" autocomplete="current-password" required></label>
    <button class="button auth-primary" type="submit">Sign in</button>
    ${message ? `<p class="form-error" role="alert">${esc(message)}</p>` : ""}
    <div class="auth-switch"><span>New to this demo?</span><button class="text-button" type="button" data-action="show-register">Create an account</button></div>
    <p class="auth-note">Demo sign-up creates an Engineer account in the local workspace. Admins can still create and manage accounts from Users &amp; access.</p>
  </form>`);
  app.querySelector("#login-form")?.addEventListener("submit", login);
}

function showRegister(message = "", values = {}) {
  authLayout(`<form class="auth-card" id="register-form" novalidate>
    <div class="eyebrow">Local demo account</div><h1>Create account</h1><p class="auth-description">Your account will join this workspace with the Engineer role.</p>
    <label class="field"><span>Full name</span><input name="fullName" autocomplete="name" value="${esc(values.fullName || "")}" maxlength="150" required></label>
    <label class="field"><span>Email address</span><input name="email" type="email" autocomplete="email" value="${esc(values.email || "")}" maxlength="254" required></label>
    <label class="field"><span>Password</span><input name="password" type="password" autocomplete="new-password" minlength="8" maxlength="72" required><small>8 to 72 characters.</small></label>
    <label class="field"><span>Confirm password</span><input name="confirmPassword" type="password" autocomplete="new-password" minlength="8" maxlength="72" required></label>
    <div class="auth-role"><span class="role-dot"></span><span><b>Engineer</b><small>Default demo role. You cannot grant yourself Admin access.</small></span></div>
    <button class="button auth-primary" type="submit">Create account and continue</button>
    ${message ? `<p class="form-error" role="alert">${esc(message)}</p>` : ""}
    <div class="auth-switch"><span>Already have an account?</span><button class="text-button" type="button" data-action="show-login">Back to sign in</button></div>
    <p class="auth-note">Self-registration works in local Development when the database contains one organization. It is disabled outside Development.</p>
  </form>`);
  app.querySelector("#register-form")?.addEventListener("submit", registerSelf);
}

async function login(event) {
  event.preventDefault();
  const form = new FormData(event.currentTarget);
  const email = String(form.get("email") || "").trim();
  const password = String(form.get("password") || "");
  state.authEmail = email;
  if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) || !password) {
    showLogin("Enter a valid email address and password.", email);
    return;
  }
  const button = event.currentTarget.querySelector("button[type=submit]");
  button.disabled = true; button.textContent = "Signing in…";
  try {
    const result = await apiData("/auth/login", { method: "POST", body: JSON.stringify({ email, password }) });
    saveTokens(result);
    if (!state.organizationId) throw new Error("The token has no organization ID.");
    await loadUserAndPage();
  } catch (error) {
    clearSession();
    showLogin(error.message || "Sign in failed. Check the email and password.", email);
  }
}

async function registerSelf(event) {
  event.preventDefault();
  const form = new FormData(event.currentTarget);
  const values = { fullName: String(form.get("fullName") || "").trim(), email: String(form.get("email") || "").trim() };
  const password = String(form.get("password") || "");
  if (!values.fullName || values.fullName.length > 150 ||
      !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email) || values.email.length > 254) {
    showRegister("Enter your full name and a valid email address.", values);
    return;
  }
  if (password.length < 8 || password.length > 72) {
    showRegister("Password must be between 8 and 72 characters.", values);
    return;
  }
  if (password !== form.get("confirmPassword")) {
    showRegister("The passwords do not match. Please enter them again.", values);
    return;
  }
  const button = event.currentTarget.querySelector("button[type=submit]");
  button.disabled = true; button.textContent = "Creating account…";
  let accountCreated = false;
  try {
    await apiData("/auth/self-register", { method: "POST", body: JSON.stringify({ ...values, password }) });
    accountCreated = true;
    const tokens = await apiData("/auth/login", { method: "POST", body: JSON.stringify({ email: values.email, password }) });
    saveTokens(tokens);
    state.page = "overview";
    if (!state.organizationId) throw new Error("The token has no organization ID.");
    await loadUserAndPage();
    toast("Account created. You are now signed in as Engineer.");
  } catch (error) {
    clearSession();
    if (accountCreated) showLogin("Your account was created. Sign in with the email and password you chose.", values.email);
    else showRegister(error.message || "Could not create the account.", values);
  }
}

async function loadUserAndPage() {
  if (!state.token) return showLogin();
  state.loading = true; state.loadError = ""; renderShell();
  try {
    state.user = await apiData("/users/me");
    state.organizationId = state.organizationId || orgIdFromToken(state.token);
    await loadPageData();
  } catch (error) {
    state.loadError = error.message;
    state.loading = false;
    renderShell();
  }
}

async function loadPageData() {
  if (!state.token) return showLogin();
  state.loading = true; state.loadError = ""; renderShell();
  try {
    const q = new URLSearchParams();
    if (state.page === "organizations" && !isAdmin()) {
      state.data = await apiData("/organizations/" + encodeURIComponent(state.organizationId));
    } else if (state.page === "overview") {
      const [services, incidents, deployments, teams] = await Promise.all([
        apiData(orgPath("services")), apiData(orgPath("incidents")), apiData(orgPath("deployments")), apiData(orgPath("teams")),
      ]);
      state.data = { services, incidents, deployments, teams };
    } else if (state.page === "organizations") state.data = await apiData("/organizations");
    else if (state.page === "integrations") state.data = [];
    else if (state.page === "teams") state.data = await apiData(orgPath("teams"));
    else if (state.page === "services") state.data = await apiData(orgPath("services"));
    else if (state.page === "incidents") {
      if (state.filters?.severity) q.set("severity", state.filters.severity);
      if (state.filters?.status) q.set("status", state.filters.status);
      if (state.filters?.serviceId) q.set("serviceId", state.filters.serviceId);
      state.data = await apiData(orgPath("incidents") + (q.size ? "?" + q.toString() : ""));
    } else if (state.page === "deployments") {
      if (state.filters?.environment) q.set("environment", state.filters.environment);
      if (state.filters?.serviceId) q.set("serviceId", state.filters.serviceId);
      state.data = await apiData(orgPath("deployments") + (q.size ? "?" + q.toString() : ""));
    } else if (state.page === "users") state.data = isAdmin() ? await apiData("/users") : await apiData("/users/me");
    else if (state.page === "audit") state.data = await apiData(orgPath("audit-logs"));
    else state.data = [];
  } catch (error) {
    state.data = [];
    state.loadError = error.message;
  }
  state.loading = false;
  renderShell();
}

async function signOut() {
  const refreshToken = state.refreshToken;
  try {
    if (state.token && refreshToken) await apiRequest("/auth/revoke-token", { method: "POST", body: JSON.stringify({ refreshToken }) });
  } catch { /* Clear the browser session even if the API is unavailable. */ }
  clearSession(); state.data = []; showLogin(); toast("Signed out.");
}

function titleCase(value) { return value ? value[0].toUpperCase() + value.slice(1) : ""; }
function date(value, time = false) {
  if (!value) return "—";
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return "—";
  return new Intl.DateTimeFormat(undefined, time ? { dateStyle: "medium", timeStyle: "short" } : { dateStyle: "medium" }).format(parsed);
}
function badge(value) {
  const text = String(value ?? "Unknown");
  return '<span class="badge ' + esc(text.toLowerCase().replace(/[^a-z]/g, "")) + '">' + esc(text) + "</span>";
}
function json(value) { return esc(JSON.stringify(value, null, 2)); }
function rows(value) { return Array.isArray(value) ? value : value ? [value] : []; }
function searchable(items) {
  const term = state.search.trim().toLowerCase();
  return rows(items).filter((item) => !term || JSON.stringify(item).toLowerCase().includes(term));
}
function actionButton(label, action, id, secondary = true) {
  return '<button class="button ' + (secondary ? "button-secondary" : "") + '" data-action="' + esc(action) + '" data-id="' + esc(id || "") + '">' + esc(label) + "</button>";
}

function renderShell() {
  if (!state.token) return showLogin();
  const admin = isAdmin();
  const visibleNav = NAV.filter(([key]) => {
    if (key === "audit") return admin;
    if (key === "organizations") return admin || state.user?.role === "TeamLead";
    return true;
  });
  const current = NAV.find(([key]) => key === state.page) || NAV[0];
  const userName = [state.user?.firstName, state.user?.lastName].filter(Boolean).join(" ") || "Loading profile";
  app.innerHTML = `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand"><span class="brand-mark">O</span><div class="brand-copy"><span class="brand-name">OpsFlow</span><span class="brand-caption">Operations workspace</span></div></div>
        <div class="nav-label">Workspace</div>
        <nav class="nav-list" aria-label="Main navigation">${visibleNav.map(([key, label, icon]) => `<button class="nav-item ${state.page === key ? "active" : ""}" data-page="${key}"><span class="nav-icon">${icon}</span><span class="nav-text">${label}</span></button>`).join("")}</nav>
        <div class="sidebar-bottom"><div class="connection"><span class="dot"></span><span>Connected · ${esc(state.user?.role || "loading")}</span></div>
          <div class="user-card"><span class="avatar">${esc((state.user?.firstName?.[0] || "O") + (state.user?.lastName?.[0] || ""))}</span><div class="user-meta"><div class="user-name">${esc(userName)}</div><div class="user-role">${esc(state.user?.email || "")}</div></div></div>
        </div>
      </aside>
      <section class="main">
        <header class="topbar">
          <div><div class="eyebrow">OpsFlow / ${esc(current[1])}</div><h1>${esc(current[1])}</h1><p class="subtitle">${esc(pageSubtitle(state.page))}</p></div>
          <div class="top-actions"><span class="signed-in-label">Signed in as <b>${esc(state.user?.email || "…")}</b> · ${esc(state.user?.role || "…")}</span><button class="button button-secondary" data-action="signout" title="Sign out and return to the sign-in page">Sign out / switch account</button><button class="button" data-action="reload" ${state.loading ? "disabled" : ""}>${state.loading ? "Loading…" : "↻ &nbsp;Reload data"}</button></div>
        </header>
        <div class="content" id="content">${state.loading ? '<section class="panel empty">Loading data from the OpsFlow API…</section>' : state.loadError ? `<section class="panel error-panel"><h2>The API did not return this page</h2><p>${esc(state.loadError)}</p><button class="button" data-action="reload">Try again</button><p class="field-hint">The API enforces roles and organization scope. A 403 response means this account is not allowed to read this resource.</p></section>` : renderPage()}</div>
      </section>
    </div>`;
}

function pageSubtitle(page) {
  return ({
    overview: "A live summary of services, incidents, and deployments.",
    organizations: "View organizations and manage their records.",
    teams: "Create teams and manage their membership.",
    services: "Register services and update their health status.",
    incidents: "Create, investigate, resolve, and review incident history.",
    deployments: "Record releases and update deployment status.",
    users: "Manage organization accounts and their roles.",
    audit: "Review actions recorded by the backend.",
    integrations: "See how persistence, caching, and background messaging fit into the workflows.",
    explorer: "Run the API routes directly and inspect the response.",
  })[page] || "";
}

function renderPage() {
  if (state.page === "overview") return renderOverview();
  if (state.page === "organizations") return renderOrganizations();
  if (state.page === "teams") return renderTeams();
  if (state.page === "services") return renderServices();
  if (state.page === "incidents") return renderIncidents();
  if (state.page === "deployments") return renderDeployments();
  if (state.page === "users") return renderUsers();
  if (state.page === "audit") return renderAudit();
  if (state.page === "integrations") return renderIntegrations();
  if (state.page === "explorer") return renderExplorer();
  return '<section class="panel empty">Choose a section from the navigation.</section>';
}

function renderOverview() {
  const d = state.data || {};
  const services = rows(d.services), incidents = rows(d.incidents), deployments = rows(d.deployments);
  const open = incidents.filter((x) => !["Resolved", "Closed"].includes(x.status)).length;
  const healthy = services.filter((x) => x.status === "Operational").length;
  return `<section class="stat-grid">
      ${statCard("Services", services.length, healthy + " operational", "◈")}
      ${statCard("Open incidents", open, "Incidents not resolved or closed", "◉")}
      ${statCard("Deployments", deployments.length, "Recorded releases", "↗")}
      ${statCard("Teams", rows(d.teams).length, "In this organization", "♧")}
    </section>
    <section class="dashboard-grid">
      <div class="panel"><div class="panel-heading"><h2>Service health</h2><button class="text-button" data-page="services">Open services →</button></div>${simpleTable(services.slice(0, 6), [["name","Service"],["status","Status"],["teamId","Team ID"]], "No services yet.")}</div>
      <div class="panel"><div class="panel-heading"><h2>Recent incidents</h2><button class="text-button" data-page="incidents">Open incidents →</button></div>${simpleTable(incidents.slice(0, 6), [["title","Incident"],["severity","Severity"],["status","Status"]], "No incidents yet.")}</div>
      <div class="panel"><div class="panel-heading"><h2>Recent deployments</h2><button class="text-button" data-page="deployments">Open deployments →</button></div>${simpleTable(deployments.slice(0, 6), [["version","Version"],["environment","Environment"],["status","Status"]], "No deployments yet.")}</div>
      <div class="panel guide-panel"><div class="eyebrow">Start here</div><h2>Create something</h2><p class="subtitle">Choose a resource, press <b>Create</b>, and submit it to the API. The API applies the real validation and permission rules.</p>
        <div class="quick-links"><button class="button" data-page="services">+ Service</button><button class="button button-secondary" data-page="incidents">+ Incident</button><button class="button button-secondary" data-page="deployments">+ Deployment</button></div>
        <div class="notice"><b>“Instance”</b> is not a resource in this backend. Use Incidents or Deployments. Open API Explorer to see every actual route, including <code>GET .../{id}</code>.</div>
      </div>
    </section>`;
}
function statCard(label, value, note, icon) {
  return `<article class="stat-card"><div class="stat-top"><span>${esc(label)}</span><span class="stat-icon">${icon}</span></div><div class="stat-value">${esc(value)}</div><div class="stat-note">${esc(note)}</div></article>`;
}
function simpleTable(items, columns, empty) {
  if (!items.length) return '<div class="empty">' + esc(empty) + "</div>";
  return '<div class="table-wrap"><table><thead><tr>' + columns.map(([, label]) => "<th>" + esc(label) + "</th>").join("") + '</tr></thead><tbody>' +
    items.map((item) => "<tr>" + columns.map(([key]) => "<td>" + (["status","severity","environment","role"].includes(key) ? badge(item[key]) : esc(item[key])) + "</td>").join("") + "</tr>").join("") +
    "</tbody></table></div>";
}

function listHeader(title, count, createAction, canCreate = false, hint = "") {
  return `<div class="resource-heading"><div><span class="eyebrow">API-backed resource</span><h2>${esc(title)}</h2><p class="subtitle">${esc(count)} returned by the API ${hint ? "· " + esc(hint) : ""}</p></div>${canCreate ? '<button class="button" data-action="' + esc(createAction) + '">＋ Create ' + esc(title.replace(/s$/, "")) + "</button>" : ""}</div>
    <div class="list-toolbar"><input id="search" class="search" type="search" placeholder="Search loaded rows…" value="${esc(state.search)}"><span class="sub-count">Search filters this response; it does not call a different API.</span></div>`;
}

function renderOrganizations() {
  const items = searchable(state.data);
  const canCreate = isAdmin();
  return listHeader("Organizations", items.length, "create-organization", canCreate) +
    (isAdmin() ? '<section class="panel list-panel"><div class="panel-heading"><h2>All organizations</h2><span class="sub-count">GET /api/organizations</span></div>' + resourceTable(items, "organization") + "</section>" :
      '<section class="panel list-panel"><div class="panel-heading"><h2>Your organization</h2><span class="sub-count">GET /api/organizations/{orgId}</span></div>' + resourceTable(items, "organization") + "</section>");
}

function renderTeams() {
  const items = searchable(state.data);
  return listHeader("Teams", items.length, "create-team", canWrite(["Admin","TeamLead"])) +
    '<section class="panel list-panel"><div class="panel-heading"><h2>Organization teams</h2><span class="sub-count">GET ' + esc(orgPath("teams")) + "</span></div>" + resourceTable(items, "team") + "</section>";
}

function renderServices() {
  const items = searchable(state.data);
  return listHeader("Services", items.length, "create-service", canWrite(["Admin","TeamLead"]), "status is cached on list reads") +
    '<section class="panel list-panel"><div class="panel-heading"><h2>Organization services</h2><span class="sub-count">GET ' + esc(orgPath("services")) + "</span></div>" + resourceTable(items, "service") + "</section>";
}

function renderIncidents() {
  const items = searchable(state.data);
  return '<form id="filter-form" class="filter-bar panel"><label>Severity<select name="severity"><option value="">All</option>' + ENUMS.severities.map((x) => '<option ' + (state.filters?.severity === x ? "selected" : "") + ">" + esc(x) + "</option>").join("") + '</select></label>' +
    '<label>Status<select name="status"><option value="">All</option>' + ENUMS.incidentStatuses.map((x) => '<option ' + (state.filters?.status === x ? "selected" : "") + ">" + esc(x) + "</option>").join("") + '</select></label>' +
    '<label>Service ID<input name="serviceId" placeholder="Optional service GUID" value="' + esc(state.filters?.serviceId || "") + '"></label><button class="button" type="submit">Apply filters</button><button class="button button-secondary" type="button" data-action="clear-filters">Clear</button></form>' +
    listHeader("Incidents", items.length, "create-incident", canWrite(["Admin","TeamLead","Engineer"]), "incident actions follow the backend lifecycle rules") +
    '<section class="panel list-panel"><div class="panel-heading"><h2>Incident records</h2><span class="sub-count">GET ' + esc(orgPath("incidents")) + "</span></div>" + resourceTable(items, "incident") + "</section>";
}

function renderDeployments() {
  const items = searchable(state.data);
  return '<form id="filter-form" class="filter-bar panel"><label>Environment<select name="environment"><option value="">All</option>' + ENUMS.environments.map((x) => '<option ' + (state.filters?.environment === x ? "selected" : "") + ">" + esc(x) + "</option>").join("") + '</select></label><label>Service ID<input name="serviceId" placeholder="Optional service GUID" value="' + esc(state.filters?.serviceId || "") + '"></label><button class="button" type="submit">Apply filters</button><button class="button button-secondary" type="button" data-action="clear-filters">Clear</button></form>' +
    listHeader("Deployments", items.length, "create-deployment", canWrite(["Admin","TeamLead","Engineer"])) +
    '<section class="panel list-panel"><div class="panel-heading"><h2>Deployment records</h2><span class="sub-count">GET ' + esc(orgPath("deployments")) + "</span></div>" + resourceTable(items, "deployment") + "</section>";
}

function renderUsers() {
  if (!isAdmin()) return '<section class="panel"><div class="panel-heading"><h2>My account</h2></div><p class="subtitle">This role can read its own profile. User list and role administration require Admin.</p>' + resourceTable(rows(state.data), "user") + "</section>";
  const items = searchable(state.data);
  return `<div class="resource-heading"><div><span class="eyebrow">API-backed resource</span><h2>Users &amp; access</h2><p class="subtitle">${esc(items.length)} accounts in this organization. Create an account here, then sign out and sign in with its email and password.</p></div><button class="button" data-action="create-user">＋ Create account</button></div>` +
    '<div class="notice">Account creation is Admin-only. New accounts are added to your current organization; this is not public self-registration.</div>' +
    '<div class="list-toolbar"><input id="search" class="search" type="search" placeholder="Search loaded rows…" value="' + esc(state.search) + '"><span class="sub-count">Search filters this response; it does not call a different API.</span></div>' +
    '<section class="panel list-panel"><div class="panel-heading"><h2>Organization users</h2><span class="sub-count">GET /api/users</span></div>' + resourceTable(items, "user") + "</section>";
}

function renderAudit() {
  const items = searchable(state.data);
  return listHeader("Audit log", items.length, "", false, "read-only") +
    '<section class="panel list-panel"><div class="panel-heading"><h2>Recorded actions</h2><span class="sub-count">GET ' + esc(orgPath("audit-logs")) + "</span></div>" + resourceTable(items, "audit") + "</section>";
}

function renderIntegrations() {
  return '<section class="integration-grid">' +
    '<article class="panel integration-card"><span class="integration-mark">PG</span><span class="eyebrow">Persistence</span><h2>PostgreSQL</h2><p>Organization, team, service, incident, deployment, account, and audit records are stored through the backend persistence layer.</p><div class="flow-label">API → EF Core → PostgreSQL</div></article>' +
    '<article class="panel integration-card"><span class="integration-mark">R</span><span class="eyebrow">Cache</span><h2>Redis</h2><p>The Services page calls the existing organization service-list endpoint. That endpoint uses the Redis-backed cache path in the backend; the browser does not connect to Redis directly.</p><div class="flow-label">Browser → API → Redis / PostgreSQL fallback</div><button class="text-button" data-page="services">Open services →</button></article>' +
    '<article class="panel integration-card"><span class="integration-mark">MQ</span><span class="eyebrow">Background messages</span><h2>RabbitMQ + Worker</h2><p>Creating an incident or changing its status invokes the existing API workflow. The API publishes incident events; the separate Worker consumes them and logs its notification processing.</p><div class="flow-label">Incident action → API → RabbitMQ → Worker</div><button class="text-button" data-page="incidents">Open incidents →</button></article>' +
    '<article class="panel integration-card"><span class="integration-mark">LOG</span><span class="eyebrow">History</span><h2>Audit log</h2><p>Admin accounts can read organization audit records through the Audit Logs controller. The UI displays the API response; it does not fabricate audit entries.</p><div class="flow-label">Admin → GET audit logs → PostgreSQL</div>' + (isAdmin() ? '<button class="text-button" data-page="audit">Open audit log →</button>' : '<span class="badge">Admin access required</span>') + "</article>" +
    '<article class="panel integration-card integration-note"><span class="eyebrow">Important boundary</span><h2>Configured flow is not live monitoring</h2><p>This page explains the implementation path. It does not check queue depth, Redis hit ratio, database health, worker health, or end-to-end delivery in real time. Use deployment health checks and service tooling for those signals.</p><div class="notice">Incident lifecycle rules and role checks still run in the backend. A successful button click is not proof that a downstream notification was delivered to an external person.</div></article>' +
    "</section>";
}

function resourceTable(items, kind) {
  const columns = {
    organization: [["name","Organization"],["description","Description"],["createdAt","Created"]],
    team: [["name","Team"],["description","Description"],["createdAt","Created"]],
    service: [["name","Service"],["status","Status"],["teamId","Team ID"],["description","Description"]],
    incident: [["title","Incident"],["severity","Severity"],["status","Status"],["serviceId","Service ID"],["detectedAt","Detected"]],
    deployment: [["version","Version"],["environment","Environment"],["status","Status"],["serviceId","Service ID"],["deployedAt","Deployed"]],
    user: [["firstName","Name"],["email","Email"],["role","Role"],["createdAt","Created"]],
    audit: [["action","Action"],["entityType","Resource"],["details","Details"],["createdAt","When"]],
  }[kind];
  if (!items.length) return '<div class="empty-state"><div class="empty-symbol">＋</div><h3>No records returned</h3><p>Create the first ' + esc(kind) + ' or check your filters and permissions.</p></div>';
  return '<div class="table-wrap"><table class="resource-table"><thead><tr>' + columns.map(([,label]) => "<th>" + esc(label) + "</th>").join("") + "<th>Actions</th></tr></thead><tbody>" +
    items.map((item) => {
      const id = item.id;
      const cells = columns.map(([key]) => {
        let value = item[key];
        if (key === "firstName") value = [item.firstName,item.lastName].filter(Boolean).join(" ");
        if (key.endsWith("At")) value = date(value, key === "detectedAt" || key === "createdAt");
        if (["status","severity","environment","role"].includes(key)) return "<td>" + badge(value) + "</td>";
        return '<td class="' + (key === "name" || key === "title" || key === "version" || key === "firstName" ? "primary-cell" : "muted-cell") + '">' + esc(value || "—") + "</td>";
      }).join("");
      return "<tr>" + cells + '<td><div class="row-actions">' + actionButton("Open", "open-" + kind, id) + resourceActions(kind, item) + "</div></td></tr>";
    }).join("") + "</tbody></table></div>";
}

function resourceActions(kind, item) {
  const id = item.id;
  if (kind === "organization") return isAdmin() ? actionButton("Edit","edit-organization",id) + actionButton("Delete","delete-organization",id) : "";
  if (kind === "team") return (canWrite(["Admin","TeamLead"]) ? actionButton("Members","members",id) + actionButton("Edit","edit-team",id) : actionButton("Members","members",id)) + (isAdmin() ? actionButton("Delete","delete-team",id) : "");
  if (kind === "service") return (canWrite(["Admin","TeamLead"]) ? actionButton("Edit","edit-service",id) : "") +
    (canWrite(["Admin","TeamLead","Engineer"]) ? actionButton("Status","service-status",id) : "") + (isAdmin() ? actionButton("Delete","delete-service",id) : "");
  if (kind === "incident") return actionButton("Open incident","open-incident",id);
  if (kind === "deployment") return canWrite(["Admin","TeamLead","Engineer"]) ? actionButton("Update status","deployment-status",id) : "";
  if (kind === "user" && isAdmin()) return actionButton("Role","user-role",id);
  if (kind === "audit") return actionButton("Details","open-audit",id);
  return "";
}

function renderExplorer() {
  const selected = API_ENDPOINTS[state.explorerIndex] || API_ENDPOINTS[0];
  const example = selected.body ? JSON.stringify(selected.body, null, 2)
    .replaceAll("{orgId}", state.organizationId || "{orgId}")
    .replaceAll("{accessToken}", state.token || "{accessToken}")
    .replaceAll("{refreshToken}", state.refreshToken || "{refreshToken}") : "";
  const pathValue = state.explorerPath || selected.path.replaceAll("{orgId}", state.organizationId || "{orgId}");
  const bodyValue = state.explorerBody || example;
  const grouped = [...new Set(API_ENDPOINTS.map((x) => x.group))];
  return '<section class="explorer-grid"><div class="panel explorer-controls"><div class="panel-heading"><div><span class="eyebrow">Try the real API</span><h2>Request builder</h2></div><span class="badge">Bearer token attached</span></div>' +
    '<div class="field"><label for="endpoint-select">Endpoint</label><select id="endpoint-select">' + grouped.map((group) => '<optgroup label="' + esc(group) + '">' + API_ENDPOINTS.map((x, i) => x.group === group ? '<option value="' + i + '" ' + (i === state.explorerIndex ? "selected" : "") + ">" + esc(x.method + " " + x.path) + "</option>" : "").join("") + "</optgroup>").join("") + "</select></div>" +
    '<div class="endpoint-note"><span class="method-chip ' + esc(selected.method.toLowerCase()) + '">' + esc(selected.method) + "</span><span>" + esc(selected.note) + '</span><span class="badge">' + esc(selected.access) + "</span></div>" +
    '<div class="field"><label for="explorer-path">Request path</label><input id="explorer-path" value="' + esc(pathValue) + '" spellcheck="false"><span class="field-hint">Current organization ID is filled in automatically. Replace {id} with a record ID.</span></div>' +
    '<div class="field"><label for="explorer-query">Query string (optional)</label><input id="explorer-query" value="' + esc(state.explorerQuery) + '" placeholder="status=Investigating&amp;severity=High"></div>' +
    '<div class="field"><label for="explorer-body">JSON body</label><textarea id="explorer-body" rows="10" spellcheck="false" placeholder="This method does not require a body">' + esc(bodyValue) + "</textarea></div>" +
    '<div class="explorer-actions"><button class="button" data-action="run-explorer">▶ Send request</button><span class="field-hint">Request is sent to this local OpsFlow API.</span></div>' +
    '<div class="notice"><b>About “Instance”:</b> there is no Instance endpoint in this API. Use GET Incident or GET Deployment by its ID. Each collection row has an Open action.</div></div>' +
    '<div class="panel explorer-result"><div class="panel-heading"><div><span class="eyebrow">Response</span><h2>' + (state.explorerResponse ? "HTTP " + esc(state.explorerResponse.status) : "Not run yet") + "</h2></div></div>" +
    (state.explorerResponse ? '<pre class="json-view">' + json(state.explorerResponse.data ?? state.explorerResponse.error) + "</pre>" : '<div class="empty-state"><div class="empty-symbol">{ }</div><h3>Run an endpoint</h3><p>The HTTP status and response body will appear here.</p></div>') +
    '<div class="endpoint-catalog"><h3>What is available</h3><p class="subtitle">' + API_ENDPOINTS.length + ' routes from the actual controllers. Method and role labels are a guide; the server makes the authorization decision.</p>' +
    API_ENDPOINTS.map((x) => '<div class="catalog-row"><span class="method-chip ' + esc(x.method.toLowerCase()) + '">' + esc(x.method) + "</span><code>" + esc(x.path) + "</code><span>" + esc(x.access) + "</span></div>").join("") + "</div></div></section>";
}

function formField(field) {
  const value = field.value ?? "";
  const hint = field.hint ? '<span class="field-hint">' + esc(field.hint) + "</span>" : "";
  let control;
  if (field.options) control = '<select name="' + esc(field.name) + '" ' + (field.required ? "required" : "") + (field.disabled ? "disabled" : "") + (field.placeholder ? '<option value="">' + esc(field.placeholder) + "</option>" : "") +
    field.options.map((x) => { const option = typeof x === "string" ? { value: x, label: x } : x; return '<option value="' + esc(option.value) + '" ' + (String(option.value) === String(value) ? "selected" : "") + ">" + esc(option.label) + "</option>"; }).join("") + "</select>";
  else if (field.type === "textarea") control = '<textarea name="' + esc(field.name) + '" rows="' + (field.rows || 4) + '" ' + (field.required ? "required" : "") + ">" + esc(value) + "</textarea>";
  else control = '<input name="' + esc(field.name) + '" type="' + esc(field.type || "text") + '" value="' + esc(value) + '" ' + (field.required ? "required" : "") + (field.placeholder ? ' placeholder="' + esc(field.placeholder) + '"' : "") + (field.minlength ? ' minlength="' + field.minlength + '"' : "") + ">";
  return '<label class="field"><span>' + esc(field.label) + (field.required ? " *" : "") + "</span>" + control + hint + "</label>";
}

function openForm(title, fields, submitLabel, onSubmit) {
  state.formSubmit = onSubmit;
  dialogTitle.textContent = title;
  dialogContent.innerHTML = '<form id="entity-form" class="dialog-form">' + fields.map(formField).join("") +
    '<div class="form-actions"><button class="button" type="submit">' + esc(submitLabel) + '</button><button class="button button-secondary" type="button" data-action="close-dialog">Cancel</button></div><p id="modal-error" class="form-error" role="alert"></p></form>';
  dialog.showModal();
}
function openPanel(title, content) {
  state.formSubmit = null;
  dialogTitle.textContent = title;
  dialogContent.innerHTML = content;
  dialog.showModal();
}
function closeDialog() { if (dialog.open) dialog.close(); }
function formValues(form) { return Object.fromEntries(new FormData(form).entries()); }
function optGuid(value) { return value ? value : null; }

function showRawRecord(kind, item) {
  openPanel(titleCase(kind) + " · " + (item.name || item.title || item.version || item.email || item.id), '<div class="detail-toolbar">' + actionButton("Copy ID","copy-id",item.id) + '</div><pre class="json-view">' + json(item) + "</pre>");
  state.copyValue = item.id;
}
async function openIncident(id) {
  openPanel("Loading incident…", '<div class="empty">GET ' + esc(orgPath("incidents") + "/" + id) + "</div>");
  try {
    const result = await apiData(orgPath("incidents") + "/" + encodeURIComponent(id));
    const incident = result.incident || result;
    const eventRows = rows(result.events).map((x) => '<div class="timeline-item"><span class="timeline-dot"></span><div><b>' + esc(x.eventType) + "</b><p>" + esc(x.description) + '</p><small>' + date(x.createdAt,true) + "</small></div></div>").join("");
    const comments = rows(result.comments).map((x) => '<div class="comment-item"><p>' + esc(x.body) + '</p><small>' + date(x.createdAt,true) + " · User " + esc(x.userId) + "</small></div>").join("");
    let controls = '<div class="detail-actions">' +
      (canWrite(["Admin","TeamLead","Engineer"]) ? actionButton("Change status","incident-status",id) + actionButton("Resolve","incident-resolve",id) + actionButton("Add comment","incident-comment",id) : "") +
      (canWrite(["Admin","TeamLead"]) ? actionButton("Change severity","incident-severity",id) + actionButton("Assign","incident-assign",id) + actionButton("Reopen","incident-reopen",id) + actionButton("Postmortem","incident-postmortem",id) : "") + "</div>";
    openPanel("Incident details", '<div class="detail-hero"><div>' + badge(incident.severity) + " " + badge(incident.status) + '</div><h2>' + esc(incident.title) + '</h2><p>' + esc(incident.description || "No description") + '</p><code>' + esc(incident.id) + "</code></div>" + controls +
      '<div class="detail-block"><h3>Record</h3><div class="detail-grid"><div><small>Service ID</small><code>' + esc(incident.serviceId) + "</code></div><div><small>Assigned user</small><code>" + esc(incident.assignedToUserId || "Unassigned") + "</code></div><div><small>Assigned team</small><code>" + esc(incident.assignedToTeamId || "Unassigned") + "</code></div><div><small>Detected</small><span>" + date(incident.detectedAt,true) + "</span></div><div><small>Resolved</small><span>" + date(incident.resolvedAt,true) + "</span></div></div></div>" +
      '<div class="detail-block"><h3>Timeline (' + rows(result.events).length + ")</h3>" + (eventRows || '<p class="empty">No timeline events.</p>') + "</div>" +
      '<div class="detail-block"><div class="panel-heading"><h3>Comments (' + rows(result.comments).length + ")</h3>" + (canWrite(["Admin","TeamLead","Engineer"]) ? actionButton("+ Add comment","incident-comment",id) : "") + "</div>" + (comments || '<p class="empty">No comments yet.</p>') + "</div>");
  } catch (error) { openPanel("Could not load incident", '<p class="form-error">' + esc(error.message) + "</p>"); }
}

function openMembers(teamId) {
  openPanel("Team members", '<div class="empty">Loading team membership…</div>');
  apiData(orgPath("teams") + "/" + encodeURIComponent(teamId) + "/members").then((members) => {
    const list = rows(members).map((u) => '<div class="member-row"><div><b>' + esc(u.firstName + " " + u.lastName) + '</b><small>' + esc(u.email) + " · " + esc(u.role) + "</small></div>" +
      (canWrite(["Admin","TeamLead"]) ? actionButton("Remove","remove-member",u.id) : "") + "</div>").join("");
    openPanel("Team members", '<div class="detail-toolbar"><code>Team ID: ' + esc(teamId) + "</code>" + (canWrite(["Admin","TeamLead"]) ? actionButton("＋ Add member","add-member",teamId) : "") + "</div>" + (list || '<div class="empty-state"><h3>No members yet</h3><p>Add a registered user using their User ID.</p></div>'));
    state.activeTeamId = teamId;
  }).catch((error) => openPanel("Could not load members", '<p class="form-error">' + esc(error.message) + "</p>"));
}

async function createResource(kind, item = null) {
  const editing = Boolean(item);
  try {
    if (["incident", "deployment"].includes(kind)) state.knownServices = await apiData(orgPath("services"));
    if (kind === "service") state.knownTeams = await apiData(orgPath("teams"));
  } catch (error) {
    toast("Could not load choices: " + error.message);
    return;
  }
  const schema = {
    organization: { label: "Organization", path: "/organizations", fields: [{ name:"name",label:"Name",required:true,value:item?.name },{ name:"description",label:"Description",type:"textarea",value:item?.description }] },
    team: { label: "Team", path: orgPath("teams"), fields: [{ name:"name",label:"Name",required:true,value:item?.name },{ name:"description",label:"Description",type:"textarea",value:item?.description }] },
    service: { label: "Service", path: orgPath("services"), fields: [{ name:"name",label:"Name",required:true,value:item?.name },{ name:"description",label:"Description",type:"textarea",value:item?.description },{ name:"teamId",label:"Team (optional)",options:[{value:"",label:"No team"},...rows(state.knownTeams).map((x)=>({value:x.id,label:x.name}))],value:item?.teamId || "",hint:"Choose a team if this service is owned by one." }] },
    incident: { label: "Incident", path: orgPath("incidents"), fields: [{ name:"title",label:"Title",required:true,value:item?.title },{ name:"description",label:"Description",type:"textarea",required:true,value:item?.description },{ name:"severity",label:"Severity",options:ENUMS.severities,required:true,value:item?.severity || "Medium" },{ name:"serviceId",label:"Service",options:rows(state.knownServices).map((x)=>({value:x.id,label:x.name})),value:item?.serviceId,required:true,hint:"An incident must be linked to a service." }] },
    deployment: { label: "Deployment", path: orgPath("deployments"), fields: [{ name:"serviceId",label:"Service",options:rows(state.knownServices).map((x)=>({value:x.id,label:x.name})),value:item?.serviceId,required:true },{ name:"version",label:"Version",required:true,value:item?.version,placeholder:"v1.0.0" },{ name:"environment",label:"Environment",options:ENUMS.environments,required:true,value:item?.environment || "Development" }] },
    user: { label: "account", path: "/auth/register", fields: [{ name:"fullName",label:"Full name",required:true },{ name:"email",label:"Email",type:"email",required:true },{ name:"password",label:"Temporary password",type:"password",required:true,minlength:8,hint:"Send this password to the user securely." },{ name:"role",label:"Role",options:ENUMS.roles,required:true,value:"Engineer" }] },
  }[kind];
  if (!schema) return;
  const fields = editing && kind === "service" ? schema.fields : schema.fields;
  openForm((editing ? "Edit " : "Create ") + schema.label, fields, editing ? "Save changes" : "Create " + schema.label.toLowerCase(), async (values) => {
    const method = editing ? "PUT" : "POST";
    const path = editing ? schema.path + "/" + encodeURIComponent(item.id) : schema.path;
    let payload = { ...values };
    if (kind === "service") payload.teamId = optGuid(values.teamId);
    if (kind === "user") payload = { ...payload, organizationId: state.organizationId };
    await apiRequest(path, { method, body: JSON.stringify(payload) });
    closeDialog(); toast(kind === "user" ? "Account created. Sign out, then sign in with its email and password." : schema.label + (editing ? " updated." : " created."));
    await loadPageData();
  });
}

function changeStatus(kind, item) {
  const configs = {
    service: { label: "Service status", options: ENUMS.serviceStatuses, path: orgPath("services") + "/" + item.id + "/status", body: (v) => ({ status:v }) },
    deployment: { label: "Deployment status", options: ENUMS.deploymentStatuses, path: orgPath("deployments") + "/" + item.id + "/status", body: (v) => ({ status:v }) },
    "incident-status": { label: "Incident status", options: ENUMS.incidentStatuses, path: orgPath("incidents") + "/" + item.id + "/status", body: (v) => ({ status:v }) },
    "incident-severity": { label: "Incident severity", options: ENUMS.severities, path: orgPath("incidents") + "/" + item.id + "/severity", body: (v) => ({ severity:v }) },
  }[kind];
  if (!configs) return;
  openForm(configs.label, [{ name:"value",label:configs.label,options:configs.options,required:true,value:item.status || item.severity }], "Update", async (value) => {
    await apiRequest(configs.path, { method: kind.startsWith("incident") ? "PATCH" : "PATCH", body: JSON.stringify(configs.body(value.value)) });
    closeDialog(); toast(configs.label + " updated."); await loadPageData();
  });
}

function incidentAction(action, incidentId) {
  const base = orgPath("incidents") + "/" + encodeURIComponent(incidentId);
  if (action === "incident-reopen") return runAction(base + "/reopen", "POST", undefined, "Incident reopened.");
  const forms = {
    "incident-resolve": { title:"Resolve incident", path:base+"/resolve", fields:[{name:"resolutionNotes",label:"Resolution notes",type:"textarea",required:true}], body:(v)=>v },
    "incident-comment": { title:"Add incident comment", path:base+"/comments", fields:[{name:"body",label:"Comment",type:"textarea",required:true}], body:(v)=>v },
    "incident-postmortem": { title:"Add postmortem", path:base+"/postmortem", fields:[{name:"body",label:"Postmortem",type:"textarea",required:true}], body:(v)=>v },
    "incident-assign": { title:"Assign incident", path:base+"/assign", fields:[{name:"userId",label:"User ID (optional)",placeholder:"Leave blank to assign to a team"},{name:"teamId",label:"Team ID (optional)",placeholder:"Leave blank to assign to a user",hint:"The API rejects setting both. Leave both blank to unassign."}], body:(v)=>({userId:optGuid(v.userId),teamId:optGuid(v.teamId)}) },
  }[action];
  if (!forms) return;
  openForm(forms.title, forms.fields, "Submit", async (values) => {
    await apiRequest(forms.path, { method:"POST", body:JSON.stringify(forms.body(values)) });
    closeDialog(); toast(forms.title + " submitted."); await loadPageData();
    if (state.page === "incidents") await openIncident(incidentId);
  });
}

async function runAction(path, method, body, success) {
  try {
    await apiRequest(path, { method, ...(body !== undefined ? {body:JSON.stringify(body)} : {}) });
    closeDialog(); toast(success); await loadPageData();
  } catch (error) { toast(error.message); }
}

function confirmDelete(kind, item) {
  if (!window.confirm("Delete this " + kind + "? This operation cannot be undone.")) return;
  const paths = { organization:"/organizations/", team:orgPath("teams")+"/", service:orgPath("services")+"/" };
  runAction(paths[kind] + encodeURIComponent(item.id), "DELETE", undefined, kind + " deleted.");
}

function handleClick(event) {
  const authAction = event.target.closest("[data-action]")?.dataset.action;
  if (authAction === "show-register") return showRegister();
  if (authAction === "show-login") return showLogin();
  const nav = event.target.closest("[data-page]");
  if (nav) { state.page = nav.dataset.page; state.search = ""; state.filters = {}; state.explorerResponse = null; loadPageData(); return; }
  const button = event.target.closest("[data-action]");
  if (!button) return;
  const { action, id } = button.dataset;
  if (action === "reload") return loadPageData();
  if (action === "signout") return signOut();
  if (action === "close-dialog") return closeDialog();
  if (action === "create-organization") return createResource("organization");
  if (action === "create-team") return createResource("team");
  if (action === "create-service") return createResource("service");
  if (action === "create-incident") return createResource("incident");
  if (action === "create-deployment") return createResource("deployment");
  if (action === "create-user") return createResource("user");
  if (action === "run-explorer") return runExplorer();
  if (action === "clear-filters") { state.filters = {}; return loadPageData(); }
  if (action === "copy-id") { navigator.clipboard?.writeText(state.copyValue || id); return toast("ID copied."); }
  if (action === "add-member") return openForm("Add team member", [{name:"userId",label:"Registered user ID",required:true,placeholder:"GUID from Users"}], "Add member", async (v) => {
    await apiRequest(orgPath("teams") + "/" + encodeURIComponent(id || state.activeTeamId) + "/members", {method:"POST",body:JSON.stringify({userId:v.userId})});
    toast("Member added."); openMembers(id || state.activeTeamId);
  });
  if (action === "remove-member") {
    const teamId = state.activeTeamId;
    if (!window.confirm("Remove this user from the team?")) return;
    return runAction(orgPath("teams") + "/" + encodeURIComponent(teamId) + "/members/" + encodeURIComponent(id), "DELETE", undefined, "Member removed.");
  }
  if (action.startsWith("open-")) {
    const kind = action.slice(5);
    const found = rows(state.data).find((x) => x.id === id);
    if (kind === "incident") return openIncident(id);
    if (kind === "audit") return showRawRecord(kind, found || { id });
    return apiData(routeFor(kind,id)).then((value)=>showRawRecord(kind,value)).catch((error)=>toast(error.message));
  }
  const item = rows(state.data).find((x) => x.id === id) || { id };
  if (action === "members") return openMembers(id);
  if (action === "edit-organization") return createResource("organization",item);
  if (action === "edit-team") return createResource("team",item);
  if (action === "edit-service") return createResource("service",item);
  if (action === "delete-organization") return confirmDelete("organization",item);
  if (action === "delete-team") return confirmDelete("team",item);
  if (action === "delete-service") return confirmDelete("service",item);
  if (action === "service-status" || action === "deployment-status") return changeStatus(action === "service-status" ? "service" : "deployment",item);
  if (["incident-status","incident-severity","incident-assign","incident-resolve","incident-comment","incident-postmortem","incident-reopen"].includes(action)) return incidentAction(action,id);
  if (action === "user-role") return openForm("Update user role",[{name:"role",label:"Role",options:ENUMS.roles,required:true,value:item.role}],"Save role",async (v)=>{
    await apiRequest("/users/"+encodeURIComponent(id)+"/role",{method:"PUT",body:JSON.stringify({role:v.role})});
    closeDialog();toast("User role updated.");await loadPageData();
  });
}

function routeFor(kind,id) {
  const routes = { organization:"/organizations/", team:orgPath("teams")+"/", service:orgPath("services")+"/", deployment:orgPath("deployments")+"/", user:"/users/", audit:orgPath("audit-logs") };
  return routes[kind] ? routes[kind] + encodeURIComponent(id) : "";
}

async function runExplorer() {
  const pathInput = document.querySelector("#explorer-path");
  const queryInput = document.querySelector("#explorer-query");
  const bodyInput = document.querySelector("#explorer-body");
  let path = pathInput.value.trim();
  const query = queryInput.value.trim().replace(/^\?/, "");
  state.explorerPath = path;
  state.explorerQuery = queryInput.value;
  state.explorerBody = bodyInput.value;
  if (/\{[^}]+\}/.test(path)) {
    state.explorerResponse = { status:"Not sent", error:"Replace every {id} placeholder with an actual record ID." };
    renderShell(); return;
  }
  if (!path.startsWith("/")) path = "/" + path;
  if (query) path += "?" + query;
  const endpoint = API_ENDPOINTS[state.explorerIndex];
  const bodyText = bodyInput.value.trim();
  try {
    let body;
    if (bodyText && !["GET","DELETE"].includes(endpoint.method)) body = JSON.stringify(JSON.parse(bodyText));
    const result = await apiRequest(path, { method:endpoint.method, ...(body !== undefined ? {body} : {}) });
    state.explorerResponse = result;
  } catch (error) {
    state.explorerResponse = { status:error.status || "Client error", error:error.message, data:error.response };
  }
  renderShell();
}

function toast(message) {
  document.querySelector(".toast")?.remove();
  const node = document.createElement("div"); node.className = "toast"; node.textContent = message; document.body.append(node);
  window.setTimeout(() => node.remove(), 4500);
}

app.addEventListener("submit", async (event) => {
  if (event.target.id === "entity-form") {
    event.preventDefault();
    const button = event.target.querySelector('button[type="submit"]');
    const error = document.querySelector("#modal-error");
    button.disabled = true; error.textContent = "";
    try { await state.formSubmit(formValues(event.target)); }
    catch (e) { error.textContent = e.message; }
    finally { if (button.isConnected) button.disabled = false; }
  }
  if (event.target.id === "filter-form") {
    event.preventDefault(); state.filters = Object.fromEntries(new FormData(event.target).entries()); await loadPageData();
  }
});
dialog.addEventListener("submit", async (event) => {
  if (event.target.id !== "entity-form") return;
  event.preventDefault();
  const button = event.target.querySelector('button[type="submit"]');
  const error = document.querySelector("#modal-error");
  button.disabled = true; error.textContent = "";
  try { await state.formSubmit(formValues(event.target)); }
  catch (e) { error.textContent = e.message; }
  finally { if (button.isConnected) button.disabled = false; }
});
app.addEventListener("click", handleClick);
dialog.addEventListener("click", handleClick);
app.addEventListener("input", (event) => {
  if (event.target.id !== "search") return;
  const at = event.target.selectionStart;
  state.search = event.target.value;
  const content = document.querySelector("#content");
  if (content) content.innerHTML = renderPage();
  const next = document.querySelector("#search"); next?.focus(); next?.setSelectionRange(at,at);
});
app.addEventListener("change", (event) => {
  if (event.target.id !== "endpoint-select") return;
  state.explorerIndex = Number(event.target.value);
  state.explorerPath = ""; state.explorerQuery = ""; state.explorerBody = ""; state.explorerResponse = null;
  renderShell();
});
dialog.addEventListener("click", (event) => { if (event.target === dialog) closeDialog(); });

state.organizationId = state.token ? orgIdFromToken(state.token) : null;
if (state.token && state.organizationId) loadUserAndPage();
else { clearSession(); showLogin(); }
