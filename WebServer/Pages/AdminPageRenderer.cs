using System.Net;
using March7thHoney.Command;

namespace March7thHoney.WebServer.Pages;

public static class AdminPageRenderer
{
    public static string Render(string serverName)
    {
        var displayName = string.IsNullOrWhiteSpace(serverName) ? "March7thHoney" : serverName.Trim();
        var encodedDisplayName = WebUtility.HtmlEncode(displayName);
        var encodedTitle = WebUtility.HtmlEncode($"Admin - {displayName}");
        var encodedPermissionHint = WebUtility.HtmlEncode(
            $"Permission overrides: {string.Join(", ", CommandPermissions.KnownPermissions())}.");

        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{{encodedTitle}}</title>
  <style>
    :root {
      color-scheme: dark;
      --bg: #0d111a;
      --band: #111827;
      --surface: #161b26;
      --surface-2: #1d2533;
      --surface-3: #222b3c;
      --ink: #f5f7fb;
      --muted: #a9b2c4;
      --line: #303849;
      --accent: #4f8cff;
      --accent-strong: #3674eb;
      --danger: #ff6f68;
      --success: #72d79b;
      --warning: #ffc56d;
      --shadow: 0 18px 55px rgba(0, 0, 0, 0.32);
    }

    * {
      box-sizing: border-box;
    }

    body {
      margin: 0;
      min-height: 100vh;
      font-family: "Inter", "Segoe UI", Arial, sans-serif;
      background:
        linear-gradient(135deg, rgba(79, 140, 255, 0.14), rgba(114, 215, 155, 0.08)),
        var(--bg);
      color: var(--ink);
    }

    button,
    input,
    select,
    textarea {
      font: inherit;
    }

    button {
      min-height: 38px;
      border: 0;
      border-radius: 6px;
      background: var(--surface-3);
      color: var(--ink);
      cursor: pointer;
      padding: 8px 12px;
      font-weight: 800;
    }

    button:hover:not(:disabled) {
      background: #2d3850;
    }

    button:disabled {
      cursor: not-allowed;
      opacity: 0.6;
    }

    .primary {
      background: var(--accent);
      color: #fff;
    }

    .primary:hover:not(:disabled) {
      background: var(--accent-strong);
    }

    .danger {
      background: rgba(255, 111, 104, 0.14);
      color: #ffb3ae;
    }

    .danger:hover:not(:disabled) {
      background: rgba(255, 111, 104, 0.22);
    }

    input,
    select,
    textarea {
      width: 100%;
      min-height: 40px;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: var(--surface-2);
      color: var(--ink);
      outline: none;
      padding: 9px 11px;
    }

    textarea {
      min-height: 88px;
      resize: vertical;
    }

    input:focus,
    select:focus,
    textarea:focus {
      border-color: var(--accent);
      box-shadow: 0 0 0 3px rgba(79, 140, 255, 0.22);
    }

    label {
      display: grid;
      gap: 7px;
      color: var(--muted);
      font-size: 13px;
      font-weight: 700;
    }

    .check-row {
      display: flex;
      align-items: center;
      gap: 10px;
      color: var(--muted);
      font-size: 13px;
      font-weight: 800;
    }

    .check-row input {
      width: 18px;
      min-height: 18px;
      padding: 0;
    }

    .shell {
      min-height: 100vh;
    }

    header {
      border-bottom: 1px solid rgba(255, 255, 255, 0.08);
      background: rgba(13, 17, 26, 0.78);
      backdrop-filter: blur(14px);
    }

    .header-inner {
      width: min(1180px, calc(100% - 40px));
      min-height: 68px;
      margin: 0 auto;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 18px;
    }

    .brand {
      min-width: 0;
    }

    h1 {
      margin: 0;
      font-size: 22px;
      line-height: 1.2;
      letter-spacing: 0;
    }

    .server {
      margin: 4px 0 0;
      color: var(--muted);
      font-size: 13px;
    }

    main {
      width: min(1180px, calc(100% - 40px));
      margin: 28px auto;
      display: grid;
      gap: 18px;
    }

    .login-panel,
    .toolbar,
    .table-wrap,
    .form-panel,
    .empty {
      background: var(--surface);
      border: 1px solid rgba(255, 255, 255, 0.08);
      border-radius: 8px;
      box-shadow: var(--shadow);
    }

    .login-panel {
      width: min(100%, 440px);
      margin: 90px auto 0;
      padding: 26px;
      display: grid;
      gap: 18px;
    }

    .login-panel h2,
    .form-panel h2 {
      margin: 0;
      font-size: 18px;
      letter-spacing: 0;
    }

    .login-panel p,
    .hint {
      margin: 0;
      color: var(--muted);
      font-size: 13px;
      line-height: 1.5;
    }

    .workspace {
      display: grid;
      grid-template-columns: minmax(0, 1fr) 360px;
      gap: 18px;
      align-items: start;
    }

    .toolbar {
      padding: 14px;
      display: grid;
      grid-template-columns: minmax(220px, 1fr) auto auto;
      gap: 10px;
      align-items: center;
    }

    .table-wrap {
      overflow: hidden;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      table-layout: fixed;
    }

    th,
    td {
      padding: 12px 14px;
      border-bottom: 1px solid var(--line);
      text-align: left;
      vertical-align: middle;
      font-size: 13px;
    }

    th {
      color: var(--muted);
      background: rgba(255, 255, 255, 0.03);
      font-weight: 800;
    }

    tr {
      cursor: pointer;
    }

    tbody tr:hover {
      background: rgba(79, 140, 255, 0.08);
    }

    tbody tr.selected {
      background: rgba(79, 140, 255, 0.14);
    }

    td {
      color: var(--ink);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .muted {
      color: var(--muted);
    }

    .pill {
      display: inline-flex;
      align-items: center;
      min-height: 24px;
      border-radius: 999px;
      padding: 3px 8px;
      background: rgba(255, 255, 255, 0.07);
      color: var(--muted);
      font-size: 12px;
      font-weight: 800;
    }

    .pill.good {
      background: rgba(114, 215, 155, 0.13);
      color: var(--success);
    }

    .pill.warn {
      background: rgba(255, 197, 109, 0.13);
      color: var(--warning);
    }

    .side {
      display: grid;
      gap: 18px;
    }

    .form-panel {
      padding: 18px;
      display: grid;
      gap: 14px;
    }

    .form-grid {
      display: grid;
      gap: 12px;
    }

    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
      align-items: center;
    }

    .message {
      min-height: 20px;
      color: var(--muted);
      font-size: 13px;
      line-height: 1.45;
    }

    .message.good {
      color: var(--success);
    }

    .message.bad {
      color: var(--danger);
    }

    .empty {
      display: none;
      padding: 24px;
      color: var(--muted);
      text-align: center;
    }

    .hidden {
      display: none !important;
    }

    @media (max-width: 980px) {
      .workspace {
        grid-template-columns: 1fr;
      }

      .toolbar {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 680px) {
      .header-inner,
      main {
        width: min(100% - 28px, 1180px);
      }

      .header-inner {
        align-items: flex-start;
        flex-direction: column;
        padding: 16px 0;
      }

      th:nth-child(5),
      td:nth-child(5),
      th:nth-child(6),
      td:nth-child(6) {
        display: none;
      }
    }
  </style>
</head>
<body>
  <div class="shell">
    <header>
      <div class="header-inner">
        <div class="brand">
          <h1>User administration</h1>
          <p class="server">{{encodedDisplayName}}</p>
        </div>
        <button id="logout-button" class="hidden" type="button">Log out</button>
      </div>
    </header>

    <main>
      <section id="login-panel" class="login-panel">
        <div>
          <h2>Admin login</h2>
          <p>Use the configured MUIP admin key to manage accounts.</p>
        </div>
        <form id="login-form" class="form-grid">
          <label>
            Admin key
            <input id="admin-key" name="admin_key" type="password" autocomplete="current-password" required>
          </label>
          <button class="primary" type="submit">Log in</button>
          <div id="login-message" class="message" role="status" aria-live="polite"></div>
        </form>
      </section>

      <section id="admin-workspace" class="workspace hidden">
        <div>
          <div class="toolbar">
            <input id="search" type="search" placeholder="Search users by UID, username, email, role, nickname, or permissions">
            <button id="refresh-button" type="button">Refresh</button>
            <span id="account-count" class="pill">0 users</span>
          </div>

          <div id="empty" class="empty">No users found.</div>
          <div class="table-wrap">
            <table aria-label="User accounts">
              <thead>
                <tr>
                  <th style="width: 90px;">UID</th>
                  <th>Username</th>
                  <th>Email</th>
                  <th style="width: 130px;">Role</th>
                  <th>Nickname</th>
                  <th style="width: 90px;">Level</th>
                  <th style="width: 120px;">Password</th>
                  <th style="width: 110px;">Verified</th>
                </tr>
              </thead>
              <tbody id="accounts-body"></tbody>
            </table>
          </div>
        </div>

        <aside class="side">
          <section class="form-panel">
            <h2>Create user</h2>
            <form id="create-form" class="form-grid">
              <label>
                Username
                <input id="create-username" required maxlength="64" autocomplete="off">
              </label>
              <label>
                Email
                <input id="create-email" type="email" required maxlength="254" autocomplete="off">
              </label>
              <label>
                Password
                <input id="create-password" type="password" required autocomplete="new-password">
              </label>
              <label class="check-row">
                <input id="create-email-verified" type="checkbox">
                Email verified
              </label>
              <label>
                UID
                <input id="create-uid" type="number" min="1" placeholder="Auto">
              </label>
              <label>
                Role
                <select id="create-role"></select>
              </label>
              <label>
                Permissions
                <textarea id="create-permissions" spellcheck="false" placeholder="Optional permission overrides"></textarea>
              </label>
              <p class="hint">{{encodedPermissionHint}}</p>
              <button class="primary" type="submit">Create user</button>
              <div id="create-message" class="message" role="status" aria-live="polite"></div>
            </form>
          </section>

          <section class="form-panel">
            <h2>Edit selected user</h2>
            <p id="selected-hint" class="hint">Select a user from the table.</p>
            <form id="edit-form" class="form-grid">
              <label>
                Username
                <input id="edit-username" disabled maxlength="64" autocomplete="off">
              </label>
              <label>
                Email
                <input id="edit-email" type="email" disabled maxlength="254" autocomplete="off">
              </label>
              <label class="check-row">
                <input id="edit-email-verified" type="checkbox" disabled>
                Email verified
              </label>
              <label>
                Role
                <select id="edit-role" disabled></select>
              </label>
              <label>
                Permissions
                <textarea id="edit-permissions" disabled spellcheck="false"></textarea>
              </label>
              <p class="hint">{{encodedPermissionHint}}</p>
              <label>
                Effective permissions
                <textarea id="edit-effective-permissions" disabled spellcheck="false"></textarea>
              </label>
              <div class="actions">
                <button id="save-user-button" class="primary" type="submit" disabled>Save changes</button>
                <button id="send-verification-button" type="button" disabled>Send verification email</button>
              </div>
            </form>
            <form id="password-form" class="form-grid">
              <label>
                New password
                <input id="edit-password" type="password" disabled autocomplete="new-password">
              </label>
              <button id="set-password-button" type="submit" disabled>Set password</button>
            </form>
            <form id="delete-form" class="form-grid">
              <label>
                Type DELETE to remove this account and its data
                <input id="delete-confirm" disabled autocomplete="off">
              </label>
              <button id="delete-user-button" class="danger" type="submit" disabled>Delete user</button>
            </form>
            <div id="edit-message" class="message" role="status" aria-live="polite"></div>
          </section>
        </aside>
      </section>
    </main>
  </div>

  <script>
    const state = {
      token: localStorage.getItem("m7-admin-token") || "",
      accounts: [],
      roles: [],
      defaultRole: "User",
      selectedUid: null
    };

    const loginPanel = document.getElementById("login-panel");
    const workspace = document.getElementById("admin-workspace");
    const logoutButton = document.getElementById("logout-button");
    const loginMessage = document.getElementById("login-message");
    const createMessage = document.getElementById("create-message");
    const editMessage = document.getElementById("edit-message");
    const accountsBody = document.getElementById("accounts-body");
    const search = document.getElementById("search");
    const empty = document.getElementById("empty");
    const accountCount = document.getElementById("account-count");

    function setMessage(element, text, kind = "") {
      element.textContent = text;
      element.className = kind ? `message ${kind}` : "message";
    }

    async function api(path, options = {}) {
      const headers = {
        ...(options.headers || {})
      };

      if (state.token) {
        headers.Authorization = `Bearer ${state.token}`;
      }

      if (options.body && !headers["Content-Type"]) {
        headers["Content-Type"] = "application/json";
      }

      const response = await fetch(path, { ...options, headers });
      const body = await response.json().catch(() => ({ message: "Request failed" }));
      if (!response.ok || body.retcode !== 0) {
        if (response.status === 401) {
          clearSession();
        }

        throw new Error(body.message || "Request failed");
      }

      return body.data;
    }

    function showWorkspace() {
      loginPanel.classList.add("hidden");
      workspace.classList.remove("hidden");
      logoutButton.classList.remove("hidden");
    }

    function showLogin() {
      workspace.classList.add("hidden");
      logoutButton.classList.add("hidden");
      loginPanel.classList.remove("hidden");
    }

    function clearSession() {
      state.token = "";
      state.selectedUid = null;
      localStorage.removeItem("m7-admin-token");
      showLogin();
    }

    async function loadAccounts() {
      const data = await api("/admin/api/accounts");
      state.accounts = data.accounts || [];
      state.roles = data.roles || [];
      state.defaultRole = data.default_role || state.roles[0] || "User";
      populateRoleSelects();
      renderAccounts();
    }

    function populateRoleSelects() {
      const roles = state.roles.length ? state.roles : [state.defaultRole];
      for (const id of ["create-role", "edit-role"]) {
        const select = document.getElementById(id);
        const previous = select.value;
        select.innerHTML = "";

        for (const role of roles) {
          const option = document.createElement("option");
          option.value = role;
          option.textContent = role;
          select.appendChild(option);
        }

        select.value = roles.includes(previous) ? previous : state.defaultRole;
      }
    }

    function renderAccounts() {
      const query = search.value.trim().toLowerCase();
      const rows = state.accounts.filter(account => {
        const text = [
          account.uid,
          account.username,
          account.email,
          account.role,
          account.nickname,
          account.permissions,
          account.effective_permissions
        ].join(" ").toLowerCase();
        return !query || text.includes(query);
      });

      accountCount.textContent = `${rows.length} ${rows.length === 1 ? "user" : "users"}`;
      empty.style.display = rows.length === 0 ? "block" : "none";
      accountsBody.innerHTML = "";

      for (const account of rows) {
        const tr = document.createElement("tr");
        tr.dataset.uid = String(account.uid);
        if (account.uid === state.selectedUid) {
          tr.classList.add("selected");
        }

        tr.innerHTML = `
          <td>${escapeHtml(account.uid)}</td>
          <td>${escapeHtml(account.username || "")}</td>
          <td class="muted">${escapeHtml(account.email || "")}</td>
          <td><span class="pill">${escapeHtml(account.role || "")}</span></td>
          <td class="muted">${escapeHtml(account.nickname || "")}</td>
          <td>${account.level ?? ""}</td>
          <td><span class="pill ${account.has_password ? "good" : "warn"}">${account.has_password ? "Set" : "Missing"}</span></td>
          <td><span class="pill ${account.email_verified ? "good" : "warn"}">${account.email_verified ? "Yes" : "No"}</span></td>
        `;
        tr.addEventListener("click", () => selectAccount(account.uid));
        accountsBody.appendChild(tr);
      }
    }

    function selectAccount(uid) {
      const account = state.accounts.find(item => item.uid === uid);
      if (!account) {
        return;
      }

      state.selectedUid = uid;
      document.getElementById("selected-hint").textContent = `UID ${account.uid}`;
      document.getElementById("edit-username").value = account.username || "";
      document.getElementById("edit-email").value = account.email || "";
      document.getElementById("edit-email-verified").checked = Boolean(account.email_verified);
      document.getElementById("edit-role").value = account.role || state.defaultRole;
      document.getElementById("edit-permissions").value = account.permissions || "";
      document.getElementById("edit-effective-permissions").value = account.effective_permissions || "";
      document.getElementById("edit-password").value = "";
      document.getElementById("delete-confirm").value = "";

      for (const id of ["edit-username", "edit-email", "edit-email-verified", "edit-role", "edit-permissions", "edit-password", "delete-confirm", "save-user-button", "send-verification-button", "set-password-button", "delete-user-button"]) {
        document.getElementById(id).disabled = false;
      }

      setMessage(editMessage, "", "");
      renderAccounts();
    }

    function selectedUidOrThrow() {
      if (!state.selectedUid) {
        throw new Error("Select a user first");
      }

      return state.selectedUid;
    }

    function escapeHtml(value) {
      return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
    }

    document.getElementById("login-form").addEventListener("submit", async event => {
      event.preventDefault();
      setMessage(loginMessage, "Logging in...", "");

      try {
        const data = await api("/admin/api/login", {
          method: "POST",
          body: JSON.stringify({
            admin_key: document.getElementById("admin-key").value
          })
        });

        state.token = data.token;
        localStorage.setItem("m7-admin-token", state.token);
        document.getElementById("admin-key").value = "";
        showWorkspace();
        await loadAccounts();
        setMessage(loginMessage, "", "");
      } catch (error) {
        setMessage(loginMessage, error instanceof Error ? error.message : "Login failed", "bad");
      }
    });

    logoutButton.addEventListener("click", async () => {
      try {
        await api("/admin/api/logout", { method: "POST" });
      } catch {
      }
      clearSession();
    });

    document.getElementById("refresh-button").addEventListener("click", async () => {
      try {
        await loadAccounts();
      } catch (error) {
        setMessage(editMessage, error instanceof Error ? error.message : "Refresh failed", "bad");
      }
    });

    search.addEventListener("input", renderAccounts);

    document.getElementById("create-form").addEventListener("submit", async event => {
      event.preventDefault();
      setMessage(createMessage, "Creating user...", "");

      const uidValue = document.getElementById("create-uid").value.trim();
      try {
        await api("/admin/api/accounts", {
          method: "POST",
          body: JSON.stringify({
            username: document.getElementById("create-username").value.trim(),
            email: document.getElementById("create-email").value.trim(),
            password: document.getElementById("create-password").value,
            uid: uidValue ? Number(uidValue) : null,
            role: document.getElementById("create-role").value,
            permissions: document.getElementById("create-permissions").value,
            email_verified: document.getElementById("create-email-verified").checked
          })
        });

        event.currentTarget.reset();
        await loadAccounts();
        setMessage(createMessage, "User created.", "good");
      } catch (error) {
        setMessage(createMessage, error instanceof Error ? error.message : "Create failed", "bad");
      }
    });

    document.getElementById("edit-form").addEventListener("submit", async event => {
      event.preventDefault();
      setMessage(editMessage, "Saving user...", "");

      try {
        const uid = selectedUidOrThrow();
        const data = await api(`/admin/api/accounts/${uid}`, {
          method: "PUT",
          body: JSON.stringify({
            username: document.getElementById("edit-username").value.trim(),
            email: document.getElementById("edit-email").value.trim(),
            role: document.getElementById("edit-role").value,
            permissions: document.getElementById("edit-permissions").value,
            email_verified: document.getElementById("edit-email-verified").checked
          })
        });

        await loadAccounts();
        selectAccount(uid);
        setMessage(editMessage, data.email_sent ? "User updated. Verification email sent." : "User updated.", "good");
      } catch (error) {
        setMessage(editMessage, error instanceof Error ? error.message : "Save failed", "bad");
      }
    });

    document.getElementById("password-form").addEventListener("submit", async event => {
      event.preventDefault();
      setMessage(editMessage, "Setting password...", "");

      try {
        const uid = selectedUidOrThrow();
        await api(`/admin/api/accounts/${uid}/password`, {
          method: "PUT",
          body: JSON.stringify({
            password: document.getElementById("edit-password").value
          })
        });

        document.getElementById("edit-password").value = "";
        await loadAccounts();
        selectAccount(uid);
        setMessage(editMessage, "Password updated.", "good");
      } catch (error) {
        setMessage(editMessage, error instanceof Error ? error.message : "Password update failed", "bad");
      }
    });

    document.getElementById("send-verification-button").addEventListener("click", async () => {
      setMessage(editMessage, "Sending verification email...", "");

      try {
        const uid = selectedUidOrThrow();
        const data = await api(`/admin/api/accounts/${uid}/email/verification`, {
          method: "POST"
        });

        await loadAccounts();
        selectAccount(uid);
        setMessage(editMessage,
          data.email_sent ? "Verification email sent." : "Email marked unverified, but the email could not be sent.",
          data.email_sent ? "good" : "bad");
      } catch (error) {
        setMessage(editMessage, error instanceof Error ? error.message : "Verification email failed", "bad");
      }
    });

    document.getElementById("delete-form").addEventListener("submit", async event => {
      event.preventDefault();
      setMessage(editMessage, "", "");

      try {
        const uid = selectedUidOrThrow();
        if (document.getElementById("delete-confirm").value !== "DELETE") {
          throw new Error("Type DELETE to confirm");
        }

        await api(`/admin/api/accounts/${uid}`, { method: "DELETE" });
        state.selectedUid = null;
        await loadAccounts();
        for (const id of ["edit-username", "edit-email", "edit-email-verified", "edit-role", "edit-permissions", "edit-effective-permissions", "edit-password", "delete-confirm", "save-user-button", "send-verification-button", "set-password-button", "delete-user-button"]) {
          const element = document.getElementById(id);
          element.disabled = true;
          if ("value" in element) {
            element.value = "";
          }
          if ("checked" in element) {
            element.checked = false;
          }
        }
        document.getElementById("selected-hint").textContent = "Select a user from the table.";
        setMessage(editMessage, "User deleted.", "good");
      } catch (error) {
        setMessage(editMessage, error instanceof Error ? error.message : "Delete failed", "bad");
      }
    });

    if (state.token) {
      showWorkspace();
      loadAccounts().catch(error => {
        setMessage(loginMessage, error instanceof Error ? error.message : "Session expired", "bad");
      });
    } else {
      showLogin();
    }
  </script>
</body>
</html>
""";
    }
}
