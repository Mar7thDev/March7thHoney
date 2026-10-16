namespace March7thHoney.WebServer.Pages;

public static partial class AdminPageScripts
{
    private static string GridFightModule()
    {
        return """
      // ============================================================ gridfight
      // Currency War panel: the status block re-renders on every poll, the edit forms are rendered once so polling
      // never wipes what the operator is typing. Every edit runs a /grid subcommand against the selected player.
      const gridFight = { uid: null, catalog: null, status: null, log: [] };

      registerModule({
        id: "gridfight",
        icon: ICONS.gridfight,
        poll: 5000,
        async load({ force }) {
          if (!state.cache.sessions || force) state.cache.sessions = await api("/admin/api/sessions");
          if (!gridFight.catalog || force) gridFight.catalog = await api("/admin/api/gridfight/catalog");
          const players = gridFightPlayers();
          const requested = Number(state.moduleParams?.uid || 0);
          if (requested) gridFight.uid = requested;
          if (!gridFight.uid || !players.some(player => player.uid === gridFight.uid)) gridFight.uid = players[0]?.uid ?? null;
          await refreshGridFightStatus();
        },
        async tick() {
          state.cache.sessions = await api("/admin/api/sessions");
          await refreshGridFightStatus();
          renderGridFightStatus();
        },
        render(root) {
          renderGridFight(root);
        }
      });

      function gridFightPlayers() {
        return (state.cache.sessions?.sessions || []).filter(session => session.has_player);
      }

      async function refreshGridFightStatus() {
        gridFight.status = gridFight.uid ? await api(`/admin/api/gridfight/${gridFight.uid}`) : null;
      }

      function gfName(kind, id) {
        const names = gridFight.catalog?.names || {};
        const table = names[kind] || {};
        return table[String(id)] || "";
      }

      function gfLabel(kind, id) {
        const name = gfName(kind, id);
        return name ? `${name} (${id})` : `#${id}`;
      }

      function gfRoleLabel(roleId, avatarId) {
        const name = gfName("avatars", avatarId) || (avatarId ? handbook.label("Avatar", avatarId) : "");
        return name && !/^#?\d+$/.test(String(name)) ? `${name} (${roleId})` : `#${roleId}`;
      }

      function gfCatalogRole(roleId) {
        return gridFight.catalog?.roles?.find(role => role.id === roleId) || null;
      }

      async function gridFightCommand(button, command) {
        if (!gridFight.uid) return;
        const result = await run(button, async () => {
          const data = await api("/admin/api/commands", { method: "POST", body: { command, target_uid: gridFight.uid } });
          gridFight.log.unshift({ command, output: data?.output || "", at: Date.now() });
          gridFight.log = gridFight.log.slice(0, 12);
          await refreshGridFightStatus();
          return data;
        }, data => data?.output || t("gridfight.done"));
        if (result !== undefined) {
          renderGridFightStatus();
          renderGridFightLog();
        }
      }

      function renderGridFight(root) {
        const players = gridFightPlayers();
        setModuleActions(html`
          ${players.length ? html`
            <select id="gf-player" aria-label="${t("gridfight.player")}">
              ${players.map(player => option(player.uid, `${player.nickname || t("common.no_nickname")} (${player.uid})`, gridFight.uid))}
            </select>` : ""}
          <button class="btn ghost small" type="button" id="gf-reload">${t("shell.refresh")}</button>`);

        if (!players.length) {
          root.innerHTML = String(html`<section class="panel">${emptyState(t("gridfight.no_players"))}</section>`);
          $("#gf-reload")?.addEventListener("click", () => activateModule("gridfight", state.moduleParams, { force: true }));
          return;
        }

        root.innerHTML = String(html`
          <div id="gf-status" class="stack"></div>
          <div class="grid two">
            ${gridFightAdjustPanel()}
            <div class="stack">
              ${gridFightRolePanel()}
              ${gridFightAugmentPanel()}
            </div>
          </div>
          <div class="grid two">
            ${gridFightItemPanel()}
            <section class="panel">
              <div class="panel-head"><h3>${t("gridfight.log")}</h3></div>
              <div class="panel-body" id="gf-log"></div>
            </section>
          </div>`);

        renderGridFightStatus();
        renderGridFightLog();
        filterGridFightRoles();
        filterGridFightAugments();
        filterGridFightItems();

        $("#gf-player")?.addEventListener("change", async event => {
          gridFight.uid = Number(event.currentTarget.value);
          await run(null, refreshGridFightStatus);
          renderGridFightStatus();
        });
        $("#gf-reload")?.addEventListener("click", () => activateModule("gridfight", state.moduleParams, { force: true }));

        on(root, "[data-gf-adjust]", "click", event => {
          const button = event.currentTarget;
          const kind = button.dataset.gfAdjust;
          const input = $(`#gf-adjust-${kind}`);
          const amount = Number.parseInt(input?.value || "", 10);
          if (!Number.isFinite(amount) || amount === 0) {
            toast(t("gridfight.amount_invalid"), "bad");
            return;
          }
          gridFightCommand(button, `/grid ${kind} ${amount}`);
        });

        $("#gf-role-search")?.addEventListener("input", filterGridFightRoles);
        $("#gf-role-cost")?.addEventListener("change", filterGridFightRoles);
        $("#gf-role-add")?.addEventListener("click", event => {
          const roleId = $("#gf-role-select")?.value;
          const star = $("#gf-role-star")?.value || "1";
          if (!roleId) { toast(t("gridfight.pick_first"), "bad"); return; }
          gridFightCommand(event.currentTarget, `/grid role ${roleId} ${star}`);
        });

        $("#gf-augment-search")?.addEventListener("input", filterGridFightAugments);
        $("#gf-augment-quality")?.addEventListener("change", filterGridFightAugments);
        $("#gf-augment-add")?.addEventListener("click", event => {
          const augmentId = $("#gf-augment-select")?.value;
          if (!augmentId) { toast(t("gridfight.pick_first"), "bad"); return; }
          gridFightCommand(event.currentTarget, `/grid augment ${augmentId}`);
        });

        $("#gf-item-kind")?.addEventListener("change", filterGridFightItems);
        $("#gf-item-search")?.addEventListener("input", filterGridFightItems);
        $("#gf-item-add")?.addEventListener("click", event => {
          const kind = $("#gf-item-kind")?.value || "equip";
          const itemId = kind === "orb" ? $("#gf-item-id")?.value : $("#gf-item-select")?.value;
          if (!itemId) { toast(t("gridfight.pick_first"), "bad"); return; }
          gridFightCommand(event.currentTarget, `/grid ${kind} ${itemId}`);
        });

        $("#gf-section-go")?.addEventListener("click", event => {
          const chapter = Number.parseInt($("#gf-chapter")?.value || "", 10);
          const section = Number.parseInt($("#gf-section")?.value || "", 10);
          if (!(chapter > 0 && section > 0)) { toast(t("gridfight.amount_invalid"), "bad"); return; }
          gridFightCommand(event.currentTarget, `/grid section ${chapter} ${section}`);
        });
      }

      function renderGridFightStatus() {
        const host = $("#gf-status");
        if (!host) return;
        const status = gridFight.status;
        const run = status?.run;
        if (!status || !status.online) {
          host.innerHTML = String(html`<section class="panel">${emptyState(t("gridfight.offline"))}</section>`);
          return;
        }
        if (!run) {
          host.innerHTML = String(html`<section class="panel">${emptyState(t("gridfight.not_in_run", { name: status.nickname || status.uid }))}</section>`);
          return;
        }

        const board = run.roles.filter(role => role.on_board);
        const bench = run.roles.filter(role => !role.on_board);
        const activeTraits = run.traits.filter(trait => trait.layer > 0);
        const idleTraits = run.traits.filter(trait => trait.layer === 0);
        const hpKind = run.hp * 3 <= run.max_hp ? "bad" : run.hp * 3 <= run.max_hp * 2 ? "warn" : "good";

        const roleRow = role => html`
          <tr>
            <td class="mono">${role.on_board ? role.position : t("gridfight.bench")}</td>
            <td><strong>${gfRoleLabel(role.role_id, role.avatar_id)}</strong></td>
            <td>${chip(t("gridfight.cost", { cost: role.cost }), role.cost >= 4 ? "accent" : "subtle")} ${chip("★".repeat(Math.min(role.star, 4)), role.star >= 3 ? "warn" : "subtle")}</td>
            <td><span class="chip-list">${(gfCatalogRole(role.role_id)?.traits || []).map(id => chip(gfName("traits", id) || `#${id}`, "subtle"))}</span></td>
            <td><span class="chip-list">${role.equipment_ids.map(id => chip(gfName("items", id) || `#${id}`, "subtle"))}</span></td>
          </tr>`;

        host.innerHTML = String(html`
          <section class="panel">
            <div class="panel-head">
              <h3>${status.nickname || status.uid} · ${t("gridfight.node", { chapter: run.chapter, section: run.section })}</h3>
              <span class="chip-list">
                ${chip(t("gridfight.season", { season: run.season }), "accent")}
                ${chip(run.phase, "subtle")}
                ${run.portal_id ? chip(gfLabel("portals", run.portal_id), "subtle") : ""}
              </span>
            </div>
            <div class="panel-body">
              <div class="metric-grid compact">
                ${metric(t("gridfight.gold"), run.gold)}
                ${metric(t("gridfight.level"), run.level, { hint: t("gridfight.exp", { exp: run.exp, max: run.max_level }) })}
                ${metric(t("gridfight.hp"), `${run.hp}/${run.max_hp}`, { kind: hpKind })}
                ${metric(t("gridfight.board"), `${board.length}/${run.max_active}`)}
                ${metric(t("gridfight.free_refresh"), run.free_refresh)}
                ${metric(t("gridfight.win_streak"), run.win_streak)}
                ${run.trailblaze_coin || run.trailblaze_spent ? metric(t("gridfight.trailblaze"), run.trailblaze_coin, { hint: t("gridfight.trailblaze_spent", { spent: run.trailblaze_spent }) }) : ""}
              </div>
            </div>
            <div class="panel-body">
              <div class="stack">
                <div>
                  <p class="section-title">${t("gridfight.traits")}</p>
                  <span class="chip-list">
                    ${activeTraits.length ? activeTraits.map(trait => chip(`${gfName("traits", trait.trait_id) || `#${trait.trait_id}`} · ${t("gridfight.layer", { layer: trait.layer, members: trait.members })}`, "good")) : html`<span class="muted">${t("gridfight.no_traits")}</span>`}
                    ${idleTraits.map(trait => chip(`${gfName("traits", trait.trait_id) || `#${trait.trait_id}`} ${trait.members}`, "subtle"))}
                  </span>
                </div>
                <div>
                  <p class="section-title">${t("gridfight.investments")}</p>
                  <span class="chip-list">
                    ${run.investments.length ? run.investments.map(id => chip(gfLabel("augments", id), "accent")) : html`<span class="muted">${t("gridfight.none")}</span>`}
                  </span>
                </div>
                <div>
                  <p class="section-title">${t("gridfight.inventory")}</p>
                  <span class="chip-list">
                    ${run.equipments.filter(item => !item.equipped).map(item => chip(gfName("items", item.equipment_id) || `#${item.equipment_id}`, "subtle"))}
                    ${run.consumables.map(item => chip(`${gfName("items", item.item_id) || `#${item.item_id}`} ×${item.count}`, "subtle"))}
                    ${!run.consumables.length && !run.equipments.some(item => !item.equipped) ? html`<span class="muted">${t("gridfight.none")}</span>` : ""}
                  </span>
                </div>
              </div>
            </div>
            <div class="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th class="w-uid">${t("gridfight.position")}</th>
                    <th>${t("gridfight.role")}</th>
                    <th class="w-md">${t("gridfight.cost_star")}</th>
                    <th>${t("gridfight.traits")}</th>
                    <th>${t("gridfight.equipment")}</th>
                  </tr>
                </thead>
                <tbody>
                  ${board.map(roleRow)}
                  ${bench.map(roleRow)}
                </tbody>
              </table>
            </div>
          </section>`);
      }

      function gridFightAdjustPanel() {
        const rows = [
          ["gold", t("gridfight.gold"), 10],
          ["exp", t("gridfight.exp_label"), 4],
          ["hp", t("gridfight.hp_change"), 20],
          ["maxhp", t("gridfight.max_hp"), 20],
          ["refresh", t("gridfight.free_refresh"), 3],
          ["emblem", t("gridfight.emblem"), 1],
          ["streak", t("gridfight.win_streak"), 1],
          ["trailblaze", t("gridfight.trailblaze"), 100]
        ];
        return html`
          <section class="panel">
            <div class="panel-head"><h3>${t("gridfight.adjust")}</h3></div>
            <div class="panel-body">
              ${rows.map(([kind, label, value]) => html`
                <div class="action-row">
                  <span>${label}</span>
                  <span class="row">
                    <input id="gf-adjust-${kind}" type="number" step="1" value="${value}" style="width: 96px" aria-label="${label}">
                    <button class="btn small" type="button" data-gf-adjust="${kind}">${t("gridfight.apply")}</button>
                  </span>
                </div>`)}
              <p class="muted small">${t("gridfight.adjust_hint")}</p>
            </div>
            <div class="panel-body">
              <div class="action-row">
                <span>${t("gridfight.jump")}</span>
                <span class="row">
                  <input id="gf-chapter" type="number" min="1" value="1" style="width: 64px" aria-label="${t("gridfight.chapter")}">
                  <span class="muted">-</span>
                  <input id="gf-section" type="number" min="1" value="1" style="width: 64px" aria-label="${t("gridfight.section")}">
                  <button class="btn small" type="button" id="gf-section-go">${t("gridfight.go")}</button>
                </span>
              </div>
            </div>
          </section>`;
      }

      function gridFightRolePanel() {
        return html`
          <section class="panel">
            <div class="panel-head"><h3>${t("gridfight.add_role")}</h3></div>
            <div class="toolbar">
              <input id="gf-role-search" type="search" placeholder="${t("gridfight.search")}">
              <select id="gf-role-cost" aria-label="${t("gridfight.cost_filter")}">
                ${option("", t("gridfight.all_costs"), "")}
                ${[1, 2, 3, 4, 5].map(cost => option(cost, t("gridfight.cost", { cost }), ""))}
              </select>
            </div>
            <div class="panel-body row">
              <select id="gf-role-select" style="flex: 1 1 220px; width: auto" aria-label="${t("gridfight.role")}"></select>
              <select id="gf-role-star" style="flex: 0 0 auto; width: auto" aria-label="${t("gridfight.star")}">
                ${[1, 2, 3].map(star => option(star, "★".repeat(star), 1))}
              </select>
              <button class="btn primary small" type="button" id="gf-role-add">${t("gridfight.add")}</button>
            </div>
          </section>`;
      }

      function gridFightAugmentPanel() {
        return html`
          <section class="panel">
            <div class="panel-head"><h3>${t("gridfight.add_augment")}</h3></div>
            <div class="toolbar">
              <input id="gf-augment-search" type="search" placeholder="${t("gridfight.search")}">
              <select id="gf-augment-quality" aria-label="${t("gridfight.quality")}">
                ${option("", t("gridfight.all_qualities"), "")}
                ${["Silver", "Gold", "Prismatic"].map(quality => option(quality, t(`gridfight.quality.${quality}`), ""))}
              </select>
            </div>
            <div class="panel-body row">
              <select id="gf-augment-select" style="flex: 1 1 260px; width: auto" aria-label="${t("gridfight.add_augment")}"></select>
              <button class="btn primary small" type="button" id="gf-augment-add">${t("gridfight.add")}</button>
            </div>
            <p class="panel-body muted small">${t("gridfight.augment_hint")}</p>
          </section>`;
      }

      function gridFightItemPanel() {
        return html`
          <section class="panel">
            <div class="panel-head"><h3>${t("gridfight.add_item")}</h3></div>
            <div class="toolbar">
              <select id="gf-item-kind" aria-label="${t("gridfight.item_kind")}">
                ${option("equip", t("gridfight.kind.equip"), "equip")}
                ${option("consumable", t("gridfight.kind.consumable"), "")}
                ${option("orb", t("gridfight.kind.orb"), "")}
              </select>
              <input id="gf-item-search" type="search" placeholder="${t("gridfight.search")}">
            </div>
            <div class="panel-body row">
              <select id="gf-item-select" style="flex: 1 1 260px; width: auto" aria-label="${t("gridfight.add_item")}"></select>
              <input id="gf-item-id" class="hidden" type="number" min="1" placeholder="${t("gridfight.orb_id")}" style="flex: 1 1 160px; width: auto">
              <button class="btn primary small" type="button" id="gf-item-add">${t("gridfight.add")}</button>
            </div>
          </section>`;
      }

      function gfMatches(query, ...parts) {
        if (!query) return true;
        return parts.some(part => String(part ?? "").toLowerCase().includes(query));
      }

      function fillGridFightSelect(select, entries) {
        if (!select) return;
        const previous = select.value;
        select.innerHTML = entries.length
          ? entries.slice(0, 400).map(([value, label]) => `<option value="${value}">${escapeHtml(label)}</option>`).join("")
          : `<option value="">${escapeHtml(t("gridfight.no_match"))}</option>`;
        if (entries.some(([value]) => String(value) === previous)) select.value = previous;
      }

      function filterGridFightRoles() {
        const query = ($("#gf-role-search")?.value || "").trim().toLowerCase();
        const cost = Number($("#gf-role-cost")?.value || 0);
        const roles = (gridFight.catalog?.roles || []).filter(role =>
          (!cost || role.cost === cost) &&
          gfMatches(query, role.id, gfName("avatars", role.avatar_id), ...role.traits.map(id => gfName("traits", id))));
        fillGridFightSelect($("#gf-role-select"), roles.map(role => [role.id,
          `${t("gridfight.cost", { cost: role.cost })} · ${gfRoleLabel(role.id, role.avatar_id)}`]));
      }

      function filterGridFightAugments() {
        const query = ($("#gf-augment-search")?.value || "").trim().toLowerCase();
        const quality = $("#gf-augment-quality")?.value || "";
        const owned = new Set(gridFight.status?.run?.investments || []);
        const augments = (gridFight.catalog?.augments || []).filter(augment =>
          (!quality || augment.quality === quality) && !owned.has(augment.id) &&
          gfMatches(query, augment.id, gfName("augments", augment.id)));
        fillGridFightSelect($("#gf-augment-select"), augments.map(augment => [augment.id,
          `${t(`gridfight.quality.${augment.quality}`)} · ${gfLabel("augments", augment.id)}`]));
      }

      function filterGridFightItems() {
        const kind = $("#gf-item-kind")?.value || "equip";
        const query = ($("#gf-item-search")?.value || "").trim().toLowerCase();
        const isOrb = kind === "orb";
        $("#gf-item-select")?.classList.toggle("hidden", isOrb);
        $("#gf-item-id")?.classList.toggle("hidden", !isOrb);
        $("#gf-item-search")?.classList.toggle("hidden", isOrb);
        if (isOrb) return;
        const catalog = gridFight.catalog || {};
        const entries = kind === "equip"
          ? (catalog.equipments || []).filter(item => gfMatches(query, item.id, item.category, gfName("items", item.id)))
            .map(item => [item.id, `${item.category} · ${gfLabel("items", item.id)}`])
          : (catalog.consumables || []).filter(id => gfMatches(query, id, gfName("items", id)))
            .map(id => [id, gfLabel("items", id)]);
        fillGridFightSelect($("#gf-item-select"), entries);
      }

      function renderGridFightLog() {
        const host = $("#gf-log");
        if (!host) return;
        host.innerHTML = String(gridFight.log.length ? html`
          <div class="stack">
            ${gridFight.log.map(entry => html`
              <div>
                <div class="row space-between"><code class="mono">${entry.command}</code><span class="muted small">${fmt.relative(Math.floor(entry.at / 1000))}</span></div>
                ${entry.output ? html`<pre class="muted small" style="white-space: pre-wrap; margin: 4px 0 0">${entry.output}</pre>` : ""}
              </div>`)}
          </div>` : html`<span class="muted">${t("gridfight.log_empty")}</span>`);
      }

""";
    }
}
