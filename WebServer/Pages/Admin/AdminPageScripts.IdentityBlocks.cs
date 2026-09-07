namespace March7thHoney.WebServer.Pages;

public static partial class AdminPageScripts
{
    private static string IdentityBlocksModule() => """
      const blocksState = { list: [], loaded: false, query: "", draft: {} };
      registerModule({
        id: "blocks", icon: ICONS.accounts,
        async load({ force, params }) {
          if (!blocksState.loaded || force) await loadIdentityBlocks();
          blocksState.draft = params.kind ? params : {};
        },
        render(root) { renderIdentityBlocks(root); }
      });

      async function loadIdentityBlocks() {
        const data = await api("/admin/api/identity-blocks");
        blocksState.list = data.blocks || [];
        blocksState.loaded = true;
      }

      function renderIdentityBlocks(root) {
        const draft = blocksState.draft;
        root.innerHTML = String(html`
          <div class="split">
            <section class="panel">
              <div class="panel-head"><h3>${t("nav.blocks")}</h3></div>
              <div class="toolbar"><input id="block-search" type="search" placeholder="${t("blocks.search")}" value="${blocksState.query}"></div>
              <div id="block-results"></div>
            </section>
            <section class="panel editor">
              <div class="panel-head"><h3>${t("blocks.add")}</h3></div>
              <div class="panel-body">
              <p class="muted">${t("blocks.hint")}</p>
              <form id="block-form" class="form-grid">
                <label class="field"><span>${t("blocks.kind")}</span><select id="block-kind">
                  ${option("ip", t("blocks.ip"), draft.kind || "ip")}
                  ${option("device", t("blocks.device"), draft.kind || "ip")}
                </select></label>
                <label class="field"><span>${t("blocks.value")}</span><input id="block-value" value="${draft.value || draft.identity_key || ""}" required maxlength="512" autocomplete="off"></label>
                <label class="field wide"><span>${t("common.reason")}</span><input id="block-reason" maxlength="512"></label>
                <label class="field"><span>${t("blocks.duration")}</span><input id="block-duration" placeholder="7d"></label>
                <div class="actions wide"><button class="btn danger" type="submit">${t("blocks.add")}</button></div>
              </form>
              </div>
            </section>
          </div>`);
        renderIdentityBlockResults();
        $("#block-search").addEventListener("input", event => {
          blocksState.query = event.currentTarget.value;
          renderIdentityBlockResults();
        });
        $("#block-form").addEventListener("submit", async event => {
          event.preventDefault();
          const kind = $("#block-kind").value;
          const value = $("#block-value").value.trim();
          const duration = $("#block-duration").value.trim();
          const reason = $("#block-reason").value.trim();
          const isKey = value === draft.identity_key && kind === draft.kind;
          if (!await confirmDialog({ title: t("blocks.add"), message: t("blocks.confirm", { value }), danger: true, confirmLabel: t("blocks.add") })) return;
          await run($("#block-form button[type=submit]"), async () => {
            await api("/admin/api/identity-blocks", { method: "POST", body: {
              kind, ...(isKey ? { identity_key: value } : { value }), reason, permanent: !duration, duration: duration || null
            }});
            await loadIdentityBlocks();
            accountsState.loaded = false;
            state.cache.overview = null;
            renderIdentityBlockResults();
          }, t("blocks.saved"));
        });
      }

      function renderIdentityBlockResults() {
        const host = $("#block-results");
        if (!host) return;
        const query = blocksState.query.toLowerCase().trim();
        const rows = blocksState.list.filter(block => [block.value, block.kind, block.reason].join(" ").toLowerCase().includes(query));
        host.innerHTML = String(rows.length ? html`
          <div class="table-wrap"><table>
            <thead><tr><th>${t("blocks.kind")}</th><th>${t("blocks.value")}</th><th>${t("common.reason")}</th><th>${t("blocks.expiry")}</th><th>${t("common.actions")}</th></tr></thead>
            <tbody>${rows.map(block => html`<tr>
              <td>${t(`blocks.${block.kind}`)}</td><td class="mono wrap">${block.value}</td><td>${block.reason || "-"}</td>
              <td>${!block.active ? chip(t("blocks.expired"), "subtle") : block.expires_at ? fmt.unix(block.expires_at) : chip(t("accounts.ban_permanent"), "bad")}</td>
              <td><button class="btn ghost small" type="button" data-unblock="${block.id}">${t("blocks.remove")}</button></td>
            </tr>`)}</tbody>
          </table></div>` : emptyState(t("blocks.empty")));
        on(host, "[data-unblock]", "click", async event => {
          const block = blocksState.list.find(item => item.id === Number(event.currentTarget.dataset.unblock));
          if (!block || !await confirmDialog({ title: t("blocks.remove"), message: block.value, confirmLabel: t("blocks.remove") })) return;
          await run(event.currentTarget, async () => {
            await api(`/admin/api/identity-blocks/${block.id}`, { method: "DELETE" });
            await loadIdentityBlocks();
            renderIdentityBlockResults();
          }, t("blocks.removed"));
        });
      }

      function renderAccountIdentityRecords(account) {
        const ips = [...new Set([account.registration_ip, account.last_ip, account.current_ip, ...(account.ip_addresses || [])].filter(Boolean))];
        const devices = account.device_ids || [];
        return html`<div class="stack">
          <div class="section-title">${t("blocks.records")}</div>
          <p class="muted small">${t("blocks.records_hint")}</p>
          ${ips.map(ip => html`<div class="row space-between"><span class="mono wrap">${ip}</span><button class="btn danger small" type="button" data-block-kind="ip" data-block-value="${ip}">${t("blocks.block_ip")}</button></div>`)}
          ${devices.map(device => html`<div class="row space-between"><span class="mono wrap">${device}</span><button class="btn danger small" type="button" data-block-kind="device" data-block-value="${device}">${t("blocks.block_device")}</button></div>`)}
          ${(account.device_keys || []).length ? html`<details><summary>${t("blocks.device_keys")}</summary><div class="stack">
            ${account.device_keys.map(key => html`<div class="row space-between"><span class="mono wrap">${key}</span><button class="btn danger small" type="button" data-block-kind="device" data-block-key="${key}">${t("blocks.block_device")}</button></div>`)}
          </div></details>` : ""}
          ${!ips.length && !devices.length && !account.device_keys?.length ? emptyState(t("blocks.no_records")) : ""}
          <button class="btn ghost" type="button" data-open-blocks>${t("nav.blocks")}</button>
        </div>`;
      }
      """;
}
