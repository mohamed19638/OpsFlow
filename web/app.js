const app = document.querySelector("#app");
const dialog = document.querySelector("#detail-dialog");
const detailContent = document.querySelector("#detail-content");
const tokenKey = "opsflow.accessToken";
const apiRoot = "/api";

const state = {
  token: sessionStorage.getItem(tokenKey),
  user: null,
  organizationId: null,
  services: [],
  incidents: [],
  deployments: [],
  page: "overview",
  search: "",
  loading: false,
};

const icons = { overview: "⌂", services: "◈", incidents: "◉", deployments: "↗" };

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, (character) => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;",
  })[character]);
}

function decodeToken(token) {
  try {
    let payload = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    payload += "=".repeat((4 - (payload.length % 4)) % 4);
    return JSON.parse(decodeURIComponent(Array.from(atob(payload), (char) =>
      `%${char.charCodeAt(0).toString(16).padStart(2, "0")}`).join("")));
  } catch {
    return null;
  }
}

function organizationIdFromToken(token) {
  const claims = decodeToken(token);
  return claims?.orgId ?? claims?.["orgId"] ?? null;
}

async function request(path, options = {}) {
  const response = await fetch(`${apiRoot}${path}`, {
    ...options,
    headers: {
      ...(options.body ? { "Content-Type": "application/json" } : {}),
      ...(state.token ? { Authorization: `Bearer ${state.token}` } : {}),
      ...options.headers,
    },
  });

  if (response.status === 401 && state.token) {
    signOut(false);
    throw new Error("Your session expired. Please sign in again.");
  }
  if (!response.ok) {
    const body = await response.text();
    let message = body || `Request failed (${response.status})`;
    try {
      const problem = JSON.parse(body);
      message = problem.detail || problem.title || problem.message || message;
    } catch { /* The response may be plain text. */ }
    throw new Error(message);
  }
  if (response.status === 204) return null;
  return response.json();
}

function showLogin(message = "") {
  app.innerHTML = `
    <section class="login-wrap">
      <div class="login-story">
        <div class="login-brand"><span class="brand-mark">O</span> OpsFlow</div>
        <div class="story-copy">
          <div class="eyebrow" style="color:#a9d8d1">Incident operations, made clear</div>
          <h1>Stay calm.<br>Keep the flow.</h1>
          <p>A quiet workspace for services, incidents, and deployments across your organization.</p>
        </div>
        <div class="story-foot">A portfolio project for modern operations teams</div>
      </div>
      <div class="login-side">
        <form class="login-card" id="login-form">
          <div class="eyebrow">Welcome back</div>
          <h2>Sign in to OpsFlow</h2>
          <p>Use a local demo account to explore the workspace.</p>
          <div class="field"><label for="email">Email address</label><input id="email" name="email" type="email" autocomplete="username" value="admin@acmetech.io" required></div>
          <div class="field"><label for="password">Password</label><input id="password" name="password" type="password" autocomplete="current-password" placeholder="Enter your password" required></div>
          <button class="button login-submit" type="submit">Sign in <span aria-hidden="true">→</span></button>
          ${message ? `<p class="form-error" role="alert">${escapeHtml(message)}</p>` : ""}
          <div class="login-hint">Demo credentials are seeded only into a fresh local development database. Never use these accounts in a public deployment.</div>
        </form>
      </div>
    </section>`;

  document.querySelector("#login-form").addEventListener("submit", login);
}

async function login(event) {
  event.preventDefault();
  const form = new FormData(event.currentTarget);
  const button = event.currentTarget.querySelector("button[type=submit]");
  button.disabled = true;
  button.textContent = "Signing in…";
  try {
    const tokens = await request("/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: form.get("email"), password: form.get("password") }),
    });
    state.token = tokens.accessToken;
    state.organizationId = organizationIdFromToken(tokens.accessToken);
    sessionStorage.setItem(tokenKey, tokens.accessToken);
    if (!state.organizationId) throw new Error("The access token did not include an organization ID.");
    await loadWorkspace();
  } catch (error) {
    state.token = null;
    state.organizationId = null;
    sessionStorage.removeItem(tokenKey);
    showLogin(error.message || "Sign in failed. Check the API and your credentials.");
  }
}

async function loadWorkspace() {
  if (!state.token) return showLogin();
  state.loading = true;
  renderShell();
  try {
    const org = `/organizations/${encodeURIComponent(state.organizationId)}`;
    const [user, services, incidents, deployments] = await Promise.all([
      request("/users/me"),
      request(`${org}/services`),
      request(`${org}/incidents`),
      request(`${org}/deployments`),
    ]);
    state.user = user;
    state.services = services;
    state.incidents = incidents;
    state.deployments = deployments;
    state.loading = false;
    renderShell();
  } catch (error) {
    state.loading = false;
    renderShell();
    showToast(error.message || "Could not load OpsFlow data.");
  }
}

function signOut(show = true) {
  state.token = null;
  state.user = null;
  state.organizationId = null;
  state.services = [];
  state.incidents = [];
  state.deployments = [];
  sessionStorage.removeItem(tokenKey);
  if (show) showToast("You have signed out.");
  showLogin();
}

function badge(value) {
  const label = String(value ?? "Unknown");
  const tone = label.toLowerCase().replace(/[^a-z]/g, "");
  return `<span class="badge ${tone}">${escapeHtml(label)}</span>`;
}

function dateLabel(value, withTime = false) {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : new Intl.DateTimeFormat(undefined, withTime
    ? { month: "short", day: "numeric", hour: "numeric", minute: "2-digit" }
    : { month: "short", day: "numeric", year: "numeric" }).format(date);
}

function renderShell() {
  if (!state.token) return showLogin();
  const pageTitle = { overview: "Operations overview", services: "Services", incidents: "Incidents", deployments: "Deployments" }[state.page];
  const name = state.user ? `${state.user.firstName} ${state.user.lastName}` : "Loading profile…";
  const initials = state.user ? `${state.user.firstName?.[0] ?? "O"}${state.user.lastName?.[0] ?? ""}` : "O";
  app.innerHTML = `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand"><span class="brand-mark">O</span><div class="brand-copy"><span class="brand-name">OpsFlow</span><span class="brand-caption">Operations workspace</span></div></div>
        <div class="nav-label">Workspace</div>
        <nav class="nav-list" aria-label="Main navigation">
          ${["overview", "services", "incidents", "deployments"].map((page) => `<button class="nav-item ${state.page === page ? "active" : ""}" data-page="${page}"><span class="nav-icon">${icons[page]}</span><span class="nav-text">${page[0].toUpperCase()}${page.slice(1)}</span></button>`).join("")}
        </nav>
        <div class="sidebar-bottom">
          <div class="connection"><span class="dot"></span><span>API connected</span></div>
          <div class="user-card"><span class="avatar">${escapeHtml(initials.toUpperCase())}</span><div class="user-meta"><div class="user-name">${escapeHtml(name)}</div><div class="user-role">${escapeHtml(state.user?.role ?? "")}</div></div><button class="logout" id="logout" title="Sign out" aria-label="Sign out">↪</button></div>
        </div>
      </aside>
      <section class="main">
        <header class="topbar">
          <div><div class="eyebrow">OpsFlow / Workspace</div><h1>${pageTitle}</h1><p class="subtitle">A clear view of the systems your team is looking after.</p></div>
          <div class="top-actions"><span class="date-pill">${dateLabel(new Date())}</span><button class="button" id="refresh" ${state.loading ? "disabled" : ""}>${state.loading ? "Loading…" : "↻ &nbsp;Refresh"}</button></div>
        </header>
        <div class="content" id="content">${state.loading ? `<div class="panel empty">Loading your organization…</div>` : renderPage()}</div>
      </section>
    </div>`;
  app.querySelectorAll("[data-page]").forEach((button) => button.addEventListener("click", () => {
    state.page = button.dataset.page;
    state.search = "";
    renderShell();
  }));
  document.querySelector("#refresh")?.addEventListener("click", loadWorkspace);
  document.querySelector("#logout")?.addEventListener("click", () => signOut());
  document.querySelector("#search")?.addEventListener("input", (event) => {
    state.search = event.target.value;
    const selection = event.target.selectionStart;
    renderShell();
    const next = document.querySelector("#search");
    next?.focus();
    next?.setSelectionRange(selection, selection);
  });
  document.querySelectorAll("[data-incident-id]").forEach((button) => button.addEventListener("click", () => openIncident(button.dataset.incidentId)));
}

function renderPage() {
  if (state.page === "services") return renderList("services");
  if (state.page === "incidents") return renderList("incidents");
  if (state.page === "deployments") return renderList("deployments");

  const activeIncidents = state.incidents.filter((incident) => !["resolved", "closed"].includes(String(incident.status).toLowerCase()));
  const operational = state.services.filter((service) => String(service.status).toLowerCase() === "operational").length;
  const latestServices = [...state.services].sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)).slice(0, 5);
  const latestIncidents = [...state.incidents].sort((a, b) => new Date(b.detectedAt) - new Date(a.detectedAt)).slice(0, 5);

  return `<section class="stat-grid">
      ${stat("Services", state.services.length, "Across your organization", "◈")}
      ${stat("Operational", operational, `${state.services.length ? Math.round((operational / state.services.length) * 100) : 0}% of services`, "✓")}
      ${stat("Open incidents", activeIncidents.length, "Needs attention", "◉", activeIncidents.length ? "warning" : "good")}
      ${stat("Deployments", state.deployments.length, "Recorded releases", "↗")}
    </section>
    <section class="dashboard-grid">
      <div class="panel"><div class="panel-heading"><h2>Service health</h2><button class="text-button" data-go="services">View all →</button></div>${serviceTable(latestServices)}</div>
      <div class="panel"><div class="panel-heading"><h2>Recent incidents</h2><button class="text-button" data-go="incidents">View all →</button></div>${incidentList(latestIncidents)}</div>
      <div class="panel"><div class="panel-heading"><h2>Recent deployments</h2><button class="text-button" data-go="deployments">View all →</button></div>${deploymentTable([...state.deployments].sort((a, b) => new Date(b.deployedAt) - new Date(a.deployedAt)).slice(0, 5))}</div>
      <div class="panel"><div class="panel-heading"><h2>About this workspace</h2></div><p class="subtitle">This view reads live data from the OpsFlow API after authentication. It brings together service health, incident activity, and deployment history.</p><div class="connection" style="margin-top:18px"><span class="dot"></span><span>Signed in as ${escapeHtml(state.user?.role ?? "user")}</span></div></div>
    </section>`;
}

function stat(label, value, note, icon, mood = "") {
  return `<article class="stat-card"><div class="stat-top"><span>${label}</span><span class="stat-icon ${mood}">${icon}</span></div><div class="stat-value">${value}</div><div class="stat-note">${note}</div></article>`;
}

function serviceTable(services) {
  if (!services.length) return `<div class="empty">No services found for this organization.</div>`;
  return `<div class="table-wrap"><table><thead><tr><th>Service</th><th>Status</th><th>Created</th></tr></thead><tbody>${services.map((service) => `<tr><td><span class="primary-cell">${escapeHtml(service.name)}</span><span class="secondary-cell">${escapeHtml(service.description || "No description")}</span></td><td>${badge(service.status)}</td><td>${dateLabel(service.createdAt)}</td></tr>`).join("")}</tbody></table></div>`;
}

function incidentList(incidents) {
  if (!incidents.length) return `<div class="empty">No incidents have been reported.</div>`;
  return `<div class="incident-list">${incidents.map((incident) => `<button class="incident-item" data-incident-id="${escapeHtml(incident.id)}"><span class="severity-mark ${escapeHtml(String(incident.severity).toLowerCase())}"></span><span><span class="incident-title">${escapeHtml(incident.title)}</span><span class="incident-meta">${escapeHtml(incident.severity)} severity · ${dateLabel(incident.detectedAt, true)}</span></span>${badge(incident.status)}</button>`).join("")}</div>`;
}

function deploymentTable(deployments) {
  if (!deployments.length) return `<div class="empty">No deployments have been recorded.</div>`;
  return `<div class="table-wrap"><table><thead><tr><th>Version</th><th>Environment</th><th>Status</th><th>Deployed</th></tr></thead><tbody>${deployments.map((deployment) => `<tr><td class="primary-cell">${escapeHtml(deployment.version)}</td><td>${badge(deployment.environment)}</td><td>${badge(deployment.status)}</td><td>${dateLabel(deployment.deployedAt)}</td></tr>`).join("")}</tbody></table></div>`;
}

function renderList(kind) {
  const data = kind === "services" ? state.services : kind === "incidents" ? state.incidents : state.deployments;
  const term = state.search.trim().toLowerCase();
  const filtered = data.filter((item) => JSON.stringify(item).toLowerCase().includes(term));
  const title = kind[0].toUpperCase() + kind.slice(1);
  const table = kind === "services" ? serviceTable(filtered) : kind === "incidents" ? incidentList(filtered) : deploymentTable(filtered);
  return `<div class="list-toolbar"><div class="subtitle">${data.length} ${kind} in your organization</div><input class="search" id="search" type="search" placeholder="Search ${kind}…" value="${escapeHtml(state.search)}" aria-label="Search ${kind}"></div><section class="panel list-panel"><div class="panel-heading"><h2>${title}</h2><span class="sub-count">${filtered.length} shown</span></div>${table}</section>`;
}

async function openIncident(id) {
  detailContent.innerHTML = `<div class="eyebrow">Incident detail</div><h2>Loading incident…</h2>`;
  dialog.showModal();
  try {
    const incident = await request(`/organizations/${encodeURIComponent(state.organizationId)}/incidents/${encodeURIComponent(id)}`);
    const record = incident.incident;
    detailContent.innerHTML = `<div class="eyebrow">${escapeHtml(record.severity)} severity · ${escapeHtml(record.status)}</div><h2>${escapeHtml(record.title)}</h2><p class="subtitle">${escapeHtml(record.description || "No description provided.")}</p>
      <div class="detail-block"><h3>Incident details</h3><div class="detail-event">Detected ${dateLabel(record.detectedAt, true)}<time>Incident ID: ${escapeHtml(record.id)}</time></div><div class="detail-event">Service ID: ${escapeHtml(record.serviceId)}<time>Assigned team: ${escapeHtml(record.assignedToTeamId || "Unassigned")}</time></div></div>
      <div class="detail-block"><h3>Timeline · ${(incident.events || []).length} events</h3>${(incident.events || []).length ? incident.events.map((event) => `<div class="detail-event">${escapeHtml(event.description || event.eventType)}<time>${dateLabel(event.createdAt, true)}</time></div>`).join("") : `<div class="empty">No timeline events.</div>`}</div>
      <div class="detail-block"><h3>Comments · ${(incident.comments || []).length}</h3>${(incident.comments || []).length ? incident.comments.map((comment) => `<div class="detail-event">${escapeHtml(comment.body)}<time>${dateLabel(comment.createdAt, true)}</time></div>`).join("") : `<div class="empty">No comments yet.</div>`}</div>`;
  } catch (error) {
    detailContent.innerHTML = `<div class="eyebrow">Incident detail</div><h2>Could not load incident</h2><p class="form-error">${escapeHtml(error.message)}</p>`;
  }
}

function showToast(message) {
  document.querySelector(".toast")?.remove();
  const toast = document.createElement("div");
  toast.className = "toast";
  toast.textContent = message;
  document.body.append(toast);
  window.setTimeout(() => toast.remove(), 4200);
}

app.addEventListener("click", (event) => {
  const button = event.target.closest("[data-go]");
  if (button) {
    state.page = button.dataset.go;
    state.search = "";
    renderShell();
  }
});

if (state.token) {
  state.organizationId = organizationIdFromToken(state.token);
  if (state.organizationId) loadWorkspace();
  else signOut(false);
} else {
  showLogin();
}
