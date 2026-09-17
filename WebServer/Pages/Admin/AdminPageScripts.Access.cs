namespace March7thHoney.WebServer.Pages;

public static partial class AdminPageScripts
{
    private static string AccessModule()
    {
        return """
      // ============================================================== access
      const accessState = {
        data: null,
        edits: {},
        defaultPermissions: [],
        expanded: new Set()
      };

      registerModule({
        id: "access",
        icon: ICONS.access,
        async load({ force }) {
          if (!accessState.data || force) await loadAccess();
        },
        render(root) {
          renderAccess(root);
        }
      });

      async function loadAccess() {
        accessState.data = await api("/admin/api/access");
        accessState.edits = {};
        for (const role of accessState.data.roles || []) {
          accessState.edits[role.name] = [...(role.configured_permissions || [])];
        }
        accessState.defaultPermissions = [...(accessState.data.default_permissions || [])];
      }

      function knownPermissionOptions(excludeRole) {
        const roles = (accessState.data?.roles || []).map(role => role.name).filter(name => name !== excludeRole);
        const known = accessState.data?.known_permissions || config.knownPermissions || [];
        return [...known, ...roles.map(name => `role:${name}`)];
      }

      function renderAccess(root) {
        const data = accessState.data || {};
        const roles = data.roles || [];
        const defaultRole = data.default_role;
        const datalist = html`<datalist id="permission-options">${knownPermissionOptions().map(permission => html`<option value="${permission}"></option>`)}</datalist>`;

        root.innerHTML = String(html`
          ${datalist}
          <div class="grid two">
            <section class="panel">
              <div class="panel-head"><h3>${t("access.defaults")}</h3></div>
              <form id="access-default-role-form" class="panel-body form-grid">
                <label class="field">
                  <span>${t("access.default_role")}</span>
                  <select id="access-default-role">${roles.map(role => option(role.name, role.name, defaultRole))}</select>
                  <span class="muted small">${t("access.default_role_hint")}</span>
                </label>
                <div class="actions"><button class="btn primary" type="submit">${t("common.save")}</button></div>
              </form>
              <form id="access-default-permissions-form" class="panel-body form-grid">
                <div class="field wide">
                  <span>${t("access.default_permissions")}</span>
                  ${permissionChips(accessState.defaultPermissions, { removable: true })}
                  <span class="muted small">${t("access.default_permissions_hint")}</span>
                </div>
                <div class="field wide">
                  <div class="input-row">
                    <input id="access-default-add" list="permission-options" placeholder="${t("access.add_permission_placeholder")}" autocomplete="off">
                    <button class="btn ghost small" type="button" id="access-default-add-button">${t("common.add")}</button>
                  </div>
                </div>
                <div class="actions wide"><button class="btn primary" type="submit">${t("common.save")}</button></div>
              </form>
            </section>

            <section class="panel">
              <div class="panel-head"><h3>${t("access.new_role")}</h3></div>
              <form id="access-new-role-form" class="panel-body form-grid">
                <label class="field"><span>${t("access.role_name")}</span><input id="access-new-name" maxlength="32" pattern="[A-Za-z0-9_-]+" required placeholder="Helper"></label>
                <label class="field"><span>${t("access.inherits")}</span><select id="access-new-inherit">${option("", t("common.none"), "")}${roles.map(role => option(role.name, role.name, ""))}</select></label>
                <label class="field wide"><span>${t("access.permissions")}</span><textarea id="access-new-permissions" spellcheck="false" placeholder="${t("accounts.permissions_placeholder")}"></textarea></label>
                <div class="actions wide"><button class="btn primary" type="submit">${t("access.create_role")}</button></div>
              </form>
              <div class="panel-body">
                <div class="section-title">${t("access.known_permissions")}</div>
                <span class="chip-list">${(data.known_permissions || []).map(permission => html`<button type="button" class="chip clickable" data-copy="${permission}" title="${t("common.copy")}">${permission}</button>`)}</span>
              </div>
            </section>
          </div>

          <div class="role-grid">
            ${roles.map(role => renderRoleCard(role, defaultRole))}
          </div>`);

        $("#access-default-role-form").addEventListener("submit", async event => {
          event.preventDefault();
          await run($("button[type=submit]", event.currentTarget), async () => {
            accessState.data = await api("/admin/api/access/default-role", { method: "PUT", body: { role: $("#access-default-role").value } });
            await loadAccess();
            accountsState.loaded = false;
            renderAccess(root);
          }, t("common.saved"));
        });

        const defaultForm = $("#access-default-permissions-form");
        on(defaultForm, "[data-remove-permission]", "click", event => {
          const permission = event.currentTarget.dataset.removePermission;
          accessState.defaultPermissions = accessState.defaultPermissions.filter(item => item !== permission);
          renderAccess(root);
        });
        const addDefault = () => {
          const input = $("#access-default-add");
          const value = input.value.trim();
          if (!value) return;
          if (!accessState.defaultPermissions.includes(value)) accessState.defaultPermissions.push(value);
          input.value = "";
          renderAccess(root);
          $("#access-default-add")?.focus();
        };
        $("#access-default-add-button").addEventListener("click", addDefault);
        $("#access-default-add").addEventListener("keydown", event => {
          if (event.key === "Enter") {
            event.preventDefault();
            addDefault();
          }
        });
        defaultForm.addEventListener("submit", async event => {
          event.preventDefault();
          await run($("button[type=submit]", defaultForm), async () => {
            await api("/admin/api/access/default-permissions", { method: "PUT", body: { permissions: accessState.defaultPermissions } });
            await loadAccess();
            accountsState.loaded = false;
            renderAccess(root);
          }, t("common.saved"));
        });

        $("#access-new-role-form").addEventListener("submit", async event => {
          event.preventDefault();
          const name = $("#access-new-name").value.trim();
          const inherit = $("#access-new-inherit").value;
          const permissions = $("#access-new-permissions").value.split(/[,\s;]+/).filter(Boolean);
          if (inherit) permissions.unshift(`role:${inherit}`);
          await run($("button[type=submit]", event.currentTarget), async () => {
            await api(`/admin/api/access/roles/${encodeURIComponent(name)}`, { method: "PUT", body: { permissions } });
            await loadAccess();
            accountsState.loaded = false;
            renderAccess(root);
          }, t("access.role_created", { name }));
        });

        on(root, "[data-copy]", "click", event => copyText(event.currentTarget.dataset.copy));
        on(root, "[data-role-remove]", "click", event => {
          const { roleRemove: role, removePermission: permission } = event.currentTarget.dataset;
          accessState.edits[role] = (accessState.edits[role] || []).filter(item => item !== permission);
          renderAccess(root);
        });
        on(root, "[data-role-add]", "click", event => addRolePermission(root, event.currentTarget.dataset.roleAdd));
        on(root, "[data-role-input]", "keydown", event => {
          if (event.key === "Enter") {
            event.preventDefault();
            addRolePermission(root, event.currentTarget.dataset.roleInput);
          }
        });
        on(root, "[data-role-toggle]", "click", event => {
          const role = event.currentTarget.dataset.roleToggle;
          if (accessState.expanded.has(role)) accessState.expanded.delete(role);
          else accessState.expanded.add(role);
          renderAccess(root);
        });
        on(root, "[data-role-save]", "click", async event => {
          const role = event.currentTarget.dataset.roleSave;
          await run(event.currentTarget, async () => {
            await api(`/admin/api/access/roles/${encodeURIComponent(role)}`, { method: "PUT", body: { permissions: accessState.edits[role] || [] } });
            await loadAccess();
            accountsState.loaded = false;
            renderAccess(root);
          }, t("access.role_saved", { name: role }));
        });
        on(root, "[data-role-delete]", "click", async event => {
          const role = event.currentTarget.dataset.roleDelete;
          if (!await confirmDialog({ title: t("access.delete_role_title", { name: role }), message: t("access.delete_role_message"), danger: true, requireText: role, confirmLabel: t("common.delete") })) return;
          await run(event.currentTarget, async () => {
            await api(`/admin/api/access/roles/${encodeURIComponent(role)}`, { method: "DELETE" });
            await loadAccess();
            accountsState.loaded = false;
            renderAccess(root);
          }, t("access.role_deleted", { name: role }));
        });
      }

      function addRolePermission(root, role) {
        const input = $(`[data-role-input="${role}"]`, root);
        const value = input?.value.trim();
        if (!value) return;
        const list = accessState.edits[role] || (accessState.edits[role] = []);
        if (!list.includes(value)) list.push(value);
        input.value = "";
        renderAccess(root);
        $(`[data-role-input="${role}"]`, root)?.focus();
      }

      function renderRoleCard(role, defaultRole) {
        const edits = accessState.edits[role.name] || [];
        const original = role.configured_permissions || [];
        const dirty = edits.length !== original.length || edits.some(permission => !original.includes(permission));
        const isDefault = role.name === defaultRole;
        const expanded = accessState.expanded.has(role.name);
        return html`
          <section class="panel role-card ${dirty ? "dirty" : ""}">
            <div class="panel-head">
              <div>
                <h3>${role.name}</h3>
                <div class="muted small">${t("access.effective_count", { count: (role.effective_permissions || []).length })}</div>
              </div>
              <span class="chip-list">
                ${isDefault ? chip(t("access.default"), "accent") : ""}
                ${dirty ? chip(t("common.unsaved"), "warn") : ""}
              </span>
            </div>
            <div class="panel-body stack">
              <div class="field">
                <span>${t("access.configured")}</span>
                <span class="chip-list">${edits.length ? edits.map(permission => html`
                  <span class="chip removable ${permission.startsWith("role:") ? "accent" : ""}">${permission}<button type="button" class="chip-remove" data-role-remove="${role.name}" data-remove-permission="${permission}" aria-label="${t("common.remove")}">&times;</button></span>`) : html`<span class="chip subtle">${t("common.none")}</span>`}</span>
              </div>
              <div class="input-row">
                <input list="permission-options" data-role-input="${role.name}" placeholder="${t("access.add_permission_placeholder")}" autocomplete="off">
                <button class="btn ghost small" type="button" data-role-add="${role.name}">${t("common.add")}</button>
              </div>
              <button class="link-button" type="button" data-role-toggle="${role.name}">${expanded ? t("access.hide_effective") : t("access.show_effective")}</button>
              ${expanded ? html`<div class="field"><span>${t("access.effective")}</span>${permissionChips(role.effective_permissions)}</div>` : ""}
              <div class="actions">
                <button class="btn primary small" type="button" data-role-save="${role.name}" ${dirty ? "" : "disabled"}>${t("common.save")}</button>
                <button class="btn danger small" type="button" data-role-delete="${role.name}" ${isDefault || role.name === "User" ? "disabled" : ""}>${t("common.delete")}</button>
              </div>
            </div>
          </section>`;
      }

""";
    }
}
