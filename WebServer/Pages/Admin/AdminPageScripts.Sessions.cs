namespace March7thHoney.WebServer.Pages;

public static partial class AdminPageScripts
{
    private static string SessionsModule()
    {
        return """
      // ============================================================ sessions
      registerModule({
        id: "sessions",
        icon: ICONS.sessions,
        poll: 5000,
        async load({ force }) {
          if (!state.cache.sessions || force) state.cache.sessions = await api("/admin/api/sessions");
          handbook.ensureDefault().then(() => {
            if (state.activeModule === "sessions") rerender();
          });
        },
        async tick() {
          state.cache.sessions = await api("/admin/api/sessions");
          rerender();
        },
        render(root) {
          renderSessions(root);
        }
      });

      function renderSessions(root) {
        const data = state.cache.sessions || {};
        const sessions = data.sessions || [];
        const players = sessions.filter(session => session.has_player);

        setModuleActions(html`
          ${chip(t("sessions.summary", { players: players.length, connections: sessions.length }), players.length ? "good" : "subtle")}
          <button class="btn danger small" type="button" id="sessions-kick-all" ${players.length ? "" : "disabled"}>${t("sessions.kick_all")}</button>`);

        root.innerHTML = String(html`
          <section class="panel">
            ${sessions.length ? html`
              <div class="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th class="w-uid">UID</th>
                      <th>${t("common.player")}</th>
                      <th class="w-md">${t("common.state")}</th>
                      <th>${t("sessions.scene")}</th>
                      <th>${t("sessions.lineup")}</th>
                      <th>${t("common.endpoint")}</th>
                      <th class="w-md">${t("sessions.last_active")}</th>
                      <th class="w-actions"></th>
                    </tr>
                  </thead>
                  <tbody>
                    ${sessions.map(session => html`
                      <tr>
                        <td class="mono">${session.has_player ? session.uid : "-"}</td>
                        <td>
                          ${session.has_player ? html`
                            <strong>${session.nickname || t("common.no_nickname")}</strong>
                            <div class="muted small">${t("common.level")} ${session.level} &middot; ${t("sessions.world_level")} ${session.world_level}${session.language ? ` &middot; ${session.language}` : ""}</div>` : html`<span class="muted">${t("sessions.no_player")}</span>`}
                        </td>
                        <td>
                          <span class="chip-list">
                            ${chip(session.state, session.state === "ACTIVE" ? "good" : "subtle")}
                            ${session.in_battle ? chip(t("sessions.battle"), "warn") : ""}
                            ${session.in_raid ? chip(t("sessions.raid"), "accent") : ""}
                          </span>
                        </td>
                        <td class="mono muted">${session.has_player ? `${session.plane_id} / ${session.floor_id} / ${session.entry_id}` : "-"}</td>
                        <td>${session.lineup?.length ? html`<span class="chip-list">${session.lineup.map(id => chip(handbook.label("Avatar", id), "subtle"))}</span>` : html`<span class="muted">-</span>`}</td>
                        <td class="mono muted">${session.endpoint}<div class="small">#${session.conversation_id}</div></td>
                        <td class="muted">${session.has_player ? fmt.relative(session.last_active) : "-"}</td>
                        <td class="row-actions">
                          ${session.has_player ? html`
                            <button class="btn ghost small" type="button" data-open-account="${session.uid}">${t("common.account")}</button>
                            <button class="btn ghost small" type="button" data-open-mail="${session.uid}">${t("common.mail")}</button>
                            <button class="btn ghost small" type="button" data-open-announce="${session.uid}">${t("common.announce")}</button>
                            <button class="btn ghost small" type="button" data-open-command="${session.uid}">${t("common.command")}</button>
                            <button class="btn danger small" type="button" data-kick="${session.uid}">${t("common.kick")}</button>` : ""}
                        </td>
                      </tr>`)}
                  </tbody>
                </table>
              </div>` : emptyState(t("sessions.empty"))}
          </section>`);

        on(root, "[data-open-account]", "click", event => navigate("accounts", { id: event.currentTarget.dataset.openAccount }));
        on(root, "[data-open-mail]", "click", event => navigate("mail", { uid: event.currentTarget.dataset.openMail }));
        on(root, "[data-open-announce]", "click", event => navigate("announce", { target: event.currentTarget.dataset.openAnnounce }));
        on(root, "[data-open-command]", "click", event => navigate("commands", { target: event.currentTarget.dataset.openCommand }));
        on(root, "[data-kick]", "click", async event => {
          const uid = event.currentTarget.dataset.kick;
          if (!await confirmDialog({ title: t("sessions.kick_title", { uid }), message: t("sessions.kick_message"), danger: true, confirmLabel: t("common.kick") })) return;
          await run(event.currentTarget, async () => {
            await api(`/admin/api/sessions/${uid}/kick`, { method: "POST" });
            state.cache.sessions = await api("/admin/api/sessions");
            renderSessions(root);
          }, t("sessions.kicked"));
        });
        const kickAll = $("#sessions-kick-all");
        if (kickAll) {
          kickAll.addEventListener("click", async () => {
            if (!await confirmDialog({ title: t("sessions.kick_all"), message: t("sessions.kick_all_message", { count: players.length }), danger: true, requireText: "KICK", confirmLabel: t("sessions.kick_all") })) return;
            await run(kickAll, async () => {
              const result = await api("/admin/api/sessions/kick-all", { method: "POST" });
              state.cache.sessions = await api("/admin/api/sessions");
              renderSessions(root);
              return result;
            }, result => t("sessions.kicked_count", { count: result?.kicked ?? 0 }));
          });
        }
      }

""";
    }
}
