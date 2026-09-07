namespace March7thHoney.WebServer.Pages;

public static partial class AdminPageScripts
{
    private static string LogsModule()
    {
        return """
      // ================================================================ logs
      const LOG_LEVELS = ["INFO", "WARN", "ERROR", "FATAL", "DEBUG"];
      const logsState = {
        entries: [],
        latest: 0,
        oldest: 0,
        buffered: 0,
        levels: new Set(["INFO", "WARN", "ERROR", "FATAL"]),
        search: "",
        follow: true,
        paused: false,
        files: null,
        limit: 300,
        maxLines: 3000,
        loading: false
      };

      registerModule({
        id: "logs",
        icon: ICONS.logs,
        poll: 2000,
        async load({ force }) {
          await fetchLogs(true);
          if (!logsState.files || force) await fetchLogFiles();
        },
        async tick() {
          if (logsState.paused || logsState.loading) return;
          await fetchLogs(false);
        },
        render(root) {
          renderLogs(root);
        }
      });

      async function fetchLogs(reset) {
        logsState.loading = true;
        try {
          const data = await api("/admin/api/logs", {
            query: {
              after: reset ? 0 : logsState.latest,
              limit: reset ? logsState.limit : 500,
              levels: Array.from(logsState.levels).join(","),
              search: logsState.search.trim()
            }
          });
          const entries = data.entries || [];
          if (reset) logsState.entries = entries;
          else logsState.entries.push(...entries);
          if (logsState.entries.length > logsState.maxLines) {
            logsState.entries.splice(0, logsState.entries.length - logsState.maxLines);
          }
          logsState.latest = data.latest_seq || logsState.latest;
          logsState.oldest = data.oldest_seq || 0;
          logsState.buffered = data.buffered || 0;
          if (reset) renderLogLines();
          else if (entries.length) appendLogLines(entries);
          updateLogMeta();
        } finally {
          logsState.loading = false;
        }
      }

      async function fetchLogFiles() {
        try {
          logsState.files = await api("/admin/api/logs/files");
        } catch {
          logsState.files = { files: [], directory: "" };
        }
      }

      function renderLogs(root) {
        root.innerHTML = String(html`
          <section class="panel log-panel">
            <div class="toolbar wrap">
              <div class="segmented" id="log-levels">
                ${LOG_LEVELS.map(level => html`<button type="button" class="lvl-${level} ${logsState.levels.has(level) ? "active" : ""}" data-log-level="${level}">${level}</button>`)}
              </div>
              <input id="log-search" type="search" placeholder="${t("logs.search_placeholder")}" value="${logsState.search}">
              ${toggleSwitch("log-follow", logsState.follow, t("logs.follow"))}
              <button class="btn ghost small" type="button" id="log-pause">${logsState.paused ? t("logs.resume") : t("logs.pause")}</button>
              <button class="btn ghost small" type="button" id="log-clear">${t("logs.clear")}</button>
              <span class="muted small" id="log-meta"></span>
            </div>
            <div id="log-lines" class="log-lines"></div>
          </section>

          <section class="panel">
            <div class="panel-head">
              <h3>${t("logs.files")}</h3>
              <div class="row">
                <span class="muted small mono">${logsState.files?.directory || ""}</span>
                <button class="btn ghost small" type="button" id="log-files-refresh">${t("common.refresh")}</button>
              </div>
            </div>
            <div id="log-files">${renderLogFiles()}</div>
          </section>`);

        renderLogLines();
        updateLogMeta();

        on(root, "[data-log-level]", "click", async event => {
          const level = event.currentTarget.dataset.logLevel;
          if (logsState.levels.has(level)) logsState.levels.delete(level);
          else logsState.levels.add(level);
          event.currentTarget.classList.toggle("active", logsState.levels.has(level));
          await fetchLogs(true).catch(error => toast(errorMessage(error), "bad"));
        });
        $("#log-search").addEventListener("input", debounce(async event => {
          logsState.search = event.target.value;
          await fetchLogs(true).catch(error => toast(errorMessage(error), "bad"));
        }, 300));
        $("#log-follow").addEventListener("change", event => {
          logsState.follow = event.currentTarget.checked;
          if (logsState.follow) scrollLogsToBottom();
        });
        $("#log-pause").addEventListener("click", event => {
          logsState.paused = !logsState.paused;
          event.currentTarget.textContent = logsState.paused ? t("logs.resume") : t("logs.pause");
          updateLogMeta();
        });
        $("#log-clear").addEventListener("click", () => {
          logsState.entries = [];
          renderLogLines();
          updateLogMeta();
        });
        $("#log-files-refresh").addEventListener("click", async event => {
          await run(event.currentTarget, async () => {
            await fetchLogFiles();
            $("#log-files").innerHTML = String(renderLogFiles());
            bindLogFiles(root);
          });
        });
        bindLogFiles(root);
        const lines = $("#log-lines");
        lines.addEventListener("scroll", () => {
          // Scrolling up pauses auto-follow; scrolling back to the bottom resumes it.
          const atBottom = lines.scrollHeight - lines.scrollTop - lines.clientHeight < 24;
          if (logsState.follow !== atBottom) {
            logsState.follow = atBottom;
            const toggle = $("#log-follow");
            if (toggle) toggle.checked = atBottom;
          }
        });
      }

      function logLine(entry) {
        return html`<div class="log-line lvl-${entry.level}"><span class="log-time">${fmt.unixMs(entry.time)}</span><span class="log-level">${entry.level}</span><span class="log-module">${entry.module}</span><span class="log-message">${entry.message}</span></div>`;
      }

      function renderLogLines() {
        const host = $("#log-lines");
        if (!host) return;
        host.innerHTML = logsState.entries.length
          ? String(html`${logsState.entries.map(logLine)}`)
          : String(emptyState(t("logs.empty")));
        scrollLogsToBottom();
      }

      function appendLogLines(entries) {
        const host = $("#log-lines");
        if (!host) return;
        if (host.querySelector(".empty")) host.innerHTML = "";
        host.insertAdjacentHTML("beforeend", String(html`${entries.map(logLine)}`));
        while (host.children.length > logsState.maxLines) host.removeChild(host.firstChild);
        scrollLogsToBottom();
      }

      function scrollLogsToBottom() {
        const host = $("#log-lines");
        if (host && logsState.follow) host.scrollTop = host.scrollHeight;
      }

      function updateLogMeta() {
        const meta = $("#log-meta");
        if (!meta) return;
        meta.textContent = t("logs.meta", { shown: logsState.entries.length, buffered: logsState.buffered }) + (logsState.paused ? ` · ${t("logs.paused")}` : "");
      }

      function renderLogFiles() {
        const files = logsState.files?.files || [];
        if (!files.length) return emptyState(t("logs.no_files"));
        return html`
          <div class="table-wrap short">
            <table>
              <thead><tr><th>${t("common.name")}</th><th class="w-md">${t("logs.size")}</th><th class="w-lg">${t("logs.modified")}</th><th class="w-actions"></th></tr></thead>
              <tbody>
                ${files.map(file => html`
                  <tr>
                    <td class="mono">${file.name} ${file.active ? chip(t("logs.active"), "good") : ""}</td>
                    <td>${fmt.bytes(file.size)}</td>
                    <td class="muted">${fmt.unix(file.modified)}</td>
                    <td class="row-actions"><button class="btn ghost small" type="button" data-log-download="${file.name}">${t("common.download")}</button></td>
                  </tr>`)}
              </tbody>
            </table>
          </div>`;
      }

      function bindLogFiles(root) {
        on(root, "[data-log-download]", "click", async event => {
          const name = event.currentTarget.dataset.logDownload;
          await run(event.currentTarget, async () => {
            const blob = await apiBlob(`/admin/api/logs/files/${encodeURIComponent(name)}`);
            downloadBlob(name, blob);
          });
        });
      }

""";
    }
}
