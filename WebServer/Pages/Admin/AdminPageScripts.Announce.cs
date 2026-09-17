namespace March7thHoney.WebServer.Pages;

public static partial class AdminPageScripts
{
    private static string AnnounceModule()
    {
        return """
      // ============================================================ announce
      const announceState = {
        config: null,
        content: "",
        duration: "",
        frequency: "",
        target: "",
        sessions: [],
        lastResult: null
      };

      registerModule({
        id: "announce",
        icon: ICONS.announce,
        async load({ force, params }) {
          if (!announceState.config || force) announceState.config = await api("/admin/api/announce/config");
          try {
            announceState.sessions = ((await api("/admin/api/sessions")).sessions || []).filter(session => session.has_player);
          } catch {
            announceState.sessions = [];
          }
          if (params.target) announceState.target = String(params.target);
          if (!announceState.content) announceState.content = announceState.config.content || "";
        },
        render(root) {
          renderAnnounce(root);
        }
      });

      function renderAnnounce(root) {
        const cfg = announceState.config || {};
        const players = announceState.sessions;
        root.innerHTML = String(html`
          <div class="split">
            <section class="panel">
              <div class="panel-head">
                <h3>${t("announce.send_title")}</h3>
                ${chip(t("announce.online_count", { count: players.length }), players.length ? "good" : "subtle")}
              </div>
              <form id="announce-form" class="panel-body form-grid">
                <label class="field wide"><span>${t("announce.content")}</span><textarea id="announce-content" rows="5" required>${announceState.content}</textarea></label>
                <label class="field"><span>${t("announce.duration")}</span><input id="announce-duration" placeholder="${cfg.duration || "30d"}" value="${announceState.duration}"><span class="muted small">${t("announce.duration_hint", { value: fmt.duration(cfg.duration_seconds) })}</span></label>
                <label class="field"><span>${t("announce.frequency")}</span><input id="announce-frequency" placeholder="${cfg.frequency || "1h"}" value="${announceState.frequency}"><span class="muted small">${t("announce.frequency_hint", { value: fmt.duration(cfg.frequency_seconds) })}</span></label>
                <label class="field wide">
                  <span>${t("announce.target")}</span>
                  <select id="announce-target">
                    ${option("", t("announce.target_all"), announceState.target)}
                    ${players.map(player => option(player.uid, `${player.nickname || t("common.no_nickname")} (${player.uid})`, announceState.target))}
                    ${announceState.target && !players.some(player => String(player.uid) === String(announceState.target)) ? option(announceState.target, `UID ${announceState.target} (${t("common.offline")})`, announceState.target) : ""}
                  </select>
                </label>
                <div class="actions wide">
                  <button class="btn primary" type="submit" ${players.length ? "" : "disabled"}>${t("announce.send")}</button>
                  <span class="muted small">${t("announce.send_hint")}</span>
                </div>
                ${announceState.lastResult ? html`<div class="wide notice good">${t("announce.sent_result", { count: announceState.lastResult.sent, duration: fmt.duration(announceState.lastResult.duration_seconds), frequency: fmt.duration(announceState.lastResult.frequency_seconds) })}</div>` : ""}
              </form>
            </section>

            <section class="panel">
              <div class="panel-head">
                <h3>${t("announce.config_title")}</h3>
                ${chip(cfg.enabled ? t("common.enabled") : t("common.disabled"), cfg.enabled ? "good" : "subtle")}
              </div>
              <form id="announce-config-form" class="panel-body form-grid">
                <div class="field wide"><span>${t("announce.login_announce")}</span>${toggleSwitch("announce-enabled", cfg.enabled, t("announce.login_announce_hint"))}</div>
                <label class="field wide"><span>${t("announce.content")}</span><textarea id="announce-config-content" rows="4">${cfg.content || ""}</textarea></label>
                <label class="field"><span>${t("announce.duration")}</span><input id="announce-config-duration" value="${cfg.duration || ""}" placeholder="30d"></label>
                <label class="field"><span>${t("announce.frequency")}</span><input id="announce-config-frequency" value="${cfg.frequency || ""}" placeholder="1h"></label>
                <div class="actions wide">
                  <button class="btn primary" type="submit">${t("common.save")}</button>
                  <button class="btn ghost" type="button" id="announce-use-config">${t("announce.use_config")}</button>
                </div>
              </form>
            </section>
          </div>`);

        $("#announce-content").addEventListener("input", event => announceState.content = event.currentTarget.value);
        $("#announce-duration").addEventListener("input", event => announceState.duration = event.currentTarget.value);
        $("#announce-frequency").addEventListener("input", event => announceState.frequency = event.currentTarget.value);
        $("#announce-target").addEventListener("change", event => announceState.target = event.currentTarget.value);
        $("#announce-form").addEventListener("submit", async event => {
          event.preventDefault();
          const content = announceState.content.trim();
          if (!content) {
            toast(t("announce.content_required"), "bad");
            return;
          }
          await run($("button[type=submit]", event.currentTarget), async () => {
            announceState.lastResult = await api("/admin/api/announce", {
              method: "POST",
              body: {
                content,
                duration: announceState.duration.trim() || null,
                frequency: announceState.frequency.trim() || null,
                target_uid: announceState.target ? Number(announceState.target) : null
              }
            });
            renderAnnounce(root);
            return announceState.lastResult;
          }, result => t("announce.sent_toast", { count: result?.sent ?? 0 }));
        });
        $("#announce-use-config").addEventListener("click", () => {
          announceState.content = $("#announce-config-content").value;
          announceState.duration = $("#announce-config-duration").value;
          announceState.frequency = $("#announce-config-frequency").value;
          renderAnnounce(root);
        });
        $("#announce-config-form").addEventListener("submit", async event => {
          event.preventDefault();
          await run($("button[type=submit]", event.currentTarget), async () => {
            announceState.config = await api("/admin/api/announce/config", {
              method: "PUT",
              body: {
                enabled: $("#announce-enabled").checked,
                content: $("#announce-config-content").value,
                duration: $("#announce-config-duration").value.trim() || null,
                frequency: $("#announce-config-frequency").value.trim() || null
              }
            });
            state.cache.overview = null;
            renderAnnounce(root);
          }, t("common.saved"));
        });
      }

""";
    }
}
