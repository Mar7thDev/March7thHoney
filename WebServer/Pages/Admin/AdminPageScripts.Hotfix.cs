namespace March7thHoney.WebServer.Pages;

public static partial class AdminPageScripts
{
    private static string HotfixModule()
    {
        return """
      // ============================================================== hotfix
      const HOTFIX_FIELDS = ["asset_bundle_url", "ex_asset_bundle_url", "ex_resource_url", "lua_url", "ifix_url"];
      const hotfixState = {
        data: null,
        edits: {},
        selected: new Set(),
        bulk: Object.fromEntries(HOTFIX_FIELDS.map(field => [field, ""])),
        filter: ""
      };

      registerModule({
        id: "hotfix",
        icon: ICONS.hotfix,
        async load({ force }) {
          if (!hotfixState.data || force) {
            hotfixState.data = await api("/admin/api/hotfix");
            hotfixState.edits = {};
          }
        },
        render(root) {
          renderHotfix(root);
        }
      });

      function hotfixEntries() {
        const query = hotfixState.filter.trim().toLowerCase();
        return (hotfixState.data?.entries || []).filter(entry => !query || entry.version.toLowerCase().includes(query));
      }

      function hotfixValue(entry, field) {
        return hotfixState.edits[entry.version]?.[field] ?? entry[field] ?? "";
      }

      function hotfixDirty(entry) {
        const edit = hotfixState.edits[entry.version];
        return Boolean(edit) && HOTFIX_FIELDS.some(field => (edit[field] ?? entry[field]) !== entry[field]);
      }

      function renderHotfix(root) {
        const data = hotfixState.data || {};
        const entries = hotfixEntries();
        const selectedCount = hotfixState.selected.size;

        root.innerHTML = String(html`
          <div class="grid two">
            <section class="panel">
              <div class="panel-head"><h3>${t("hotfix.delivery")}</h3></div>
              <div class="panel-body stack">
                <div class="switch-row">
                  <div><strong>${t("hotfix.send")}</strong><div class="muted small">${t("hotfix.send_hint")}</div></div>
                  <label class="switch"><input type="checkbox" id="hotfix-send" ${data.send_hotfix ? "checked" : ""}><span class="switch-track"></span></label>
                </div>
                <div class="switch-row">
                  <div><strong>${t("hotfix.fetch")}</strong><div class="muted small">${t("hotfix.fetch_hint")}</div></div>
                  <label class="switch"><input type="checkbox" id="hotfix-fetch" ${data.fetch_remote ? "checked" : ""}><span class="switch-track"></span></label>
                </div>
                <div class="switch-row">
                  <div><strong>${t("hotfix.region")}</strong><div class="muted small">${t("hotfix.region_hint")}</div></div>
                  <select id="hotfix-region" class="inline-select">${(data.regions || ["None", "CN", "OS"]).map(region => option(region, region, data.region))}</select>
                </div>
              </div>
            </section>

            <section class="panel">
              <div class="panel-head">
                <h3>${t("hotfix.bulk")}</h3>
                ${chip(t("hotfix.selected", { count: selectedCount }), selectedCount ? "accent" : "subtle")}
              </div>
              <form id="hotfix-bulk-form" class="panel-body form-grid">
                ${HOTFIX_FIELDS.map(field => html`<label class="field wide compact"><span>${t(`hotfix.field.${field}`)}</span><input class="mono" data-bulk-field="${field}" value="${hotfixState.bulk[field]}" placeholder="https://"></label>`)}
                <div class="actions wide">
                  <button class="btn primary" type="submit" ${selectedCount ? "" : "disabled"}>${t("hotfix.apply_selected", { count: selectedCount })}</button>
                  <span class="muted small">${t("hotfix.bulk_hint")}</span>
                </div>
              </form>
            </section>
          </div>

          <section class="panel">
            <div class="panel-head">
              <h3>${t("hotfix.versions")}</h3>
              <div class="row">
                <input id="hotfix-filter" type="search" placeholder="${t("hotfix.filter_placeholder")}" value="${hotfixState.filter}">
                <button class="btn ghost small" type="button" id="hotfix-select-visible">${t("hotfix.select_visible")}</button>
                <button class="btn ghost small" type="button" id="hotfix-select-none">${t("hotfix.select_none")}</button>
              </div>
            </div>
            ${entries.length ? html`
              <div class="table-wrap">
                <table class="hotfix-table">
                  <thead>
                    <tr>
                      <th class="w-xs"></th>
                      <th class="w-lg">${t("hotfix.version")}</th>
                      ${HOTFIX_FIELDS.map(field => html`<th>${t(`hotfix.field.${field}`)}</th>`)}
                      <th class="w-sm"></th>
                    </tr>
                  </thead>
                  <tbody>
                    ${entries.map(entry => html`
                      <tr class="${hotfixDirty(entry) ? "dirty" : ""}">
                        <td><input type="checkbox" data-hf-select="${entry.version}" ${hotfixState.selected.has(entry.version) ? "checked" : ""}></td>
                        <td class="mono"><strong>${entry.version}</strong>${HOTFIX_FIELDS.every(field => !entry[field]) ? html`<div class="muted small">${t("hotfix.empty")}</div>` : ""}</td>
                        ${HOTFIX_FIELDS.map(field => html`<td><input class="mono compact" data-hf-version="${entry.version}" data-hf-field="${field}" value="${hotfixValue(entry, field)}" title="${hotfixValue(entry, field)}"></td>`)}
                        <td class="row-actions"><button class="btn primary small" type="button" data-hf-save="${entry.version}" ${hotfixDirty(entry) ? "" : "disabled"}>${t("common.save")}</button></td>
                      </tr>`)}
                  </tbody>
                </table>
              </div>` : emptyState(t("common.no_results"))}
          </section>`);

        const saveFlags = async () => {
          try {
            hotfixState.data = await api("/admin/api/hotfix", {
              method: "PUT",
              body: { send_hotfix: $("#hotfix-send").checked, fetch_remote: $("#hotfix-fetch").checked }
            });
            toast(t("common.saved"), "good");
          } catch (error) {
            toast(errorMessage(error), "bad");
            renderHotfix(root);
          }
        };
        $("#hotfix-send").addEventListener("change", saveFlags);
        $("#hotfix-fetch").addEventListener("change", saveFlags);
        $("#hotfix-region").addEventListener("change", async event => {
          try {
            hotfixState.data = await api("/admin/api/hotfix", { method: "PUT", body: { region: event.currentTarget.value } });
            toast(t("common.saved"), "good");
          } catch (error) {
            toast(errorMessage(error), "bad");
            renderHotfix(root);
          }
        });

        on(root, "[data-bulk-field]", "input", event => {
          hotfixState.bulk[event.currentTarget.dataset.bulkField] = event.currentTarget.value;
        });
        $("#hotfix-bulk-form").addEventListener("submit", async event => {
          event.preventDefault();
          const versions = Array.from(hotfixState.selected);
          if (!versions.length) return;
          if (!await confirmDialog({ title: t("hotfix.apply_selected", { count: versions.length }), message: versions.join(", "), confirmLabel: t("common.apply") })) return;
          await run($("button[type=submit]", event.currentTarget), async () => {
            hotfixState.data = await api("/admin/api/hotfix", { method: "PUT", body: { versions, ...hotfixState.bulk } });
            for (const version of versions) delete hotfixState.edits[version];
            renderHotfix(root);
          }, t("hotfix.applied", { count: versions.length }));
        });

        $("#hotfix-filter").addEventListener("input", debounce(event => {
          hotfixState.filter = event.target.value;
          renderHotfix(root);
          $("#hotfix-filter").focus();
        }, 200));
        $("#hotfix-select-visible").addEventListener("click", () => {
          for (const entry of hotfixEntries()) hotfixState.selected.add(entry.version);
          renderHotfix(root);
        });
        $("#hotfix-select-none").addEventListener("click", () => {
          hotfixState.selected.clear();
          renderHotfix(root);
        });
        on(root, "[data-hf-select]", "change", event => {
          const version = event.currentTarget.dataset.hfSelect;
          if (event.currentTarget.checked) hotfixState.selected.add(version);
          else hotfixState.selected.delete(version);
          const count = hotfixState.selected.size;
          const submit = $("#hotfix-bulk-form button[type=submit]");
          submit.disabled = !count;
          submit.textContent = t("hotfix.apply_selected", { count });
        });
        on(root, "[data-hf-version]", "input", event => {
          const { hfVersion: version, hfField: field } = event.currentTarget.dataset;
          const entry = (hotfixState.data.entries || []).find(item => item.version === version);
          if (!entry) return;
          hotfixState.edits[version] = { ...(hotfixState.edits[version] || {}), [field]: event.currentTarget.value.trim() };
          const row = event.currentTarget.closest("tr");
          const dirty = hotfixDirty(entry);
          row.classList.toggle("dirty", dirty);
          $(`[data-hf-save="${version}"]`, row).disabled = !dirty;
        });
        on(root, "[data-hf-save]", "click", async event => {
          const version = event.currentTarget.dataset.hfSave;
          const edit = hotfixState.edits[version];
          const entry = (hotfixState.data.entries || []).find(item => item.version === version);
          if (!entry) return;
          const payload = { versions: [version] };
          for (const field of HOTFIX_FIELDS) payload[field] = edit?.[field] ?? entry[field] ?? "";
          await run(event.currentTarget, async () => {
            hotfixState.data = await api("/admin/api/hotfix", { method: "PUT", body: payload });
            delete hotfixState.edits[version];
            renderHotfix(root);
          }, t("hotfix.saved", { version }));
        });
      }

""";
    }
}
