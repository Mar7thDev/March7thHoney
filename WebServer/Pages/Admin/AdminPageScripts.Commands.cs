namespace March7thHoney.WebServer.Pages;

public static partial class AdminPageScripts
{
    private static string CommandsModule()
    {
        return """
      // ============================================================ commands
      const commandsState = {
        catalog: null,
        sessions: [],
        target: "",
        text: "",
        history: [],
        historyIndex: -1,
        output: [],
        filter: "",
        selected: null
      };

      try {
        commandsState.history = JSON.parse(readStorage(STORAGE.history) || "[]");
        if (!Array.isArray(commandsState.history)) commandsState.history = [];
      } catch {
        commandsState.history = [];
      }

      registerModule({
        id: "commands",
        icon: ICONS.commands,
        async load({ force, params }) {
          if (!commandsState.catalog || force) commandsState.catalog = (await api("/admin/api/commands/catalog")).commands || [];
          await refreshCommandSessions();
          if (params.target) commandsState.target = String(params.target);
        },
        render(root) {
          renderCommands(root);
        }
      });

      async function refreshCommandSessions() {
        try {
          commandsState.sessions = ((await api("/admin/api/sessions")).sessions || []).filter(session => session.has_player);
        } catch {
          commandsState.sessions = [];
        }
      }

      function renderCommands(root) {
        root.innerHTML = String(html`
          <div class="split console-split">
            <section class="panel console-panel">
              <div class="panel-head">
                <h3>${t("commands.console")}</h3>
                <div class="row">
                  <button class="btn ghost small" type="button" id="cmd-refresh-sessions" title="${t("commands.refresh_players")}">${t("common.refresh")}</button>
                  <button class="btn ghost small" type="button" id="cmd-clear">${t("commands.clear_output")}</button>
                </div>
              </div>
              <div class="panel-body stack">
                <div class="input-row">
                  <label class="field compact">
                    <span>${t("commands.target")}</span>
                    <input id="cmd-target" type="number" min="1" placeholder="${t("commands.target_placeholder")}" value="${commandsState.target}">
                  </label>
                  <label class="field compact grow">
                    <span>${t("commands.online_players")}</span>
                    <select id="cmd-target-select">
                      ${option("", t("commands.pick_player"), "")}
                      ${commandsState.sessions.map(session => option(session.uid, `${session.nickname || t("common.no_nickname")} (${session.uid})`, commandsState.target))}
                    </select>
                  </label>
                </div>
                <div id="cmd-output" class="console-output" aria-live="polite">${renderCommandOutput()}</div>
                <form id="cmd-form" class="input-row">
                  <span class="console-prompt">&gt;</span>
                  <input id="cmd-input" class="mono" autocomplete="off" spellcheck="false" placeholder="${t("commands.input_placeholder")}" value="${commandsState.text}">
                  <button class="btn primary" type="submit">${t("commands.run")}</button>
                </form>
                <div class="muted small">${t("commands.hint")}</div>
              </div>
            </section>

            <section class="panel">
              <div class="panel-head">
                <h3>${t("commands.reference")}</h3>
                ${chip(t("commands.count", { count: (commandsState.catalog || []).length }), "subtle")}
              </div>
              <div class="panel-body stack">
                <input id="cmd-filter" type="search" placeholder="${t("commands.filter_placeholder")}" value="${commandsState.filter}">
                <div id="cmd-catalog" class="catalog-list">${renderCommandCatalog()}</div>
                <div id="cmd-detail">${renderCommandDetail()}</div>
              </div>
            </section>
          </div>`);

        const input = $("#cmd-input");
        const targetInput = $("#cmd-target");
        const targetSelect = $("#cmd-target-select");
        targetInput.addEventListener("input", () => {
          commandsState.target = targetInput.value.trim();
          targetSelect.value = commandsState.sessions.some(session => String(session.uid) === commandsState.target) ? commandsState.target : "";
        });
        targetSelect.addEventListener("change", () => {
          commandsState.target = targetSelect.value;
          targetInput.value = commandsState.target;
        });
        input.addEventListener("input", () => commandsState.text = input.value);
        input.addEventListener("keydown", event => {
          if (event.key === "ArrowUp" || event.key === "ArrowDown") {
            if (!commandsState.history.length) return;
            event.preventDefault();
            if (event.key === "ArrowUp") {
              commandsState.historyIndex = commandsState.historyIndex < 0
                ? commandsState.history.length - 1
                : Math.max(0, commandsState.historyIndex - 1);
            } else {
              commandsState.historyIndex = commandsState.historyIndex < 0 || commandsState.historyIndex >= commandsState.history.length - 1
                ? -1
                : commandsState.historyIndex + 1;
            }
            input.value = commandsState.historyIndex < 0 ? "" : commandsState.history[commandsState.historyIndex];
            commandsState.text = input.value;
          }
        });
        $("#cmd-form").addEventListener("submit", event => {
          event.preventDefault();
          executeCommand($("button[type=submit]", event.currentTarget));
        });
        $("#cmd-clear").addEventListener("click", () => {
          commandsState.output = [];
          $("#cmd-output").innerHTML = String(renderCommandOutput());
        });
        $("#cmd-refresh-sessions").addEventListener("click", async event => {
          await run(event.currentTarget, async () => {
            await refreshCommandSessions();
            renderCommands(root);
          });
        });
        $("#cmd-filter").addEventListener("input", event => {
          commandsState.filter = event.currentTarget.value;
          $("#cmd-catalog").innerHTML = String(renderCommandCatalog());
          bindCommandCatalog(root);
        });
        bindCommandCatalog(root);
        bindCommandDetail(root);
        input.focus();
      }

      function renderCommandOutput() {
        if (!commandsState.output.length) return emptyState(t("commands.no_output"));
        return html`${commandsState.output.map(entry => html`
          <div class="console-entry ${entry.ok ? "" : "bad"}">
            <div class="console-line"><span class="console-time">${entry.time}</span><span class="console-prompt">&gt;</span><span class="console-command">${entry.command}</span>${entry.target ? chip(`@${entry.target}`, "subtle") : ""}</div>
            <pre class="console-result">${entry.output}</pre>
          </div>`)}`;
      }

      async function executeCommand(button) {
        const command = commandsState.text.trim();
        if (!command) {
          toast(t("commands.empty"), "bad");
          return;
        }
        const target = commandsState.target ? Number(commandsState.target) : null;
        commandsState.history = commandsState.history.filter(item => item !== command);
        commandsState.history.push(command);
        if (commandsState.history.length > 100) commandsState.history.shift();
        commandsState.historyIndex = -1;
        writeStorage(STORAGE.history, JSON.stringify(commandsState.history));

        const time = new Date().toLocaleTimeString(state.lang, { hour12: false });
        await run(button, async () => {
          try {
            const data = await api("/admin/api/commands", { method: "POST", body: { command, target_uid: target } });
            commandsState.output.push({ command, target, output: data.output || t("commands.no_result"), ok: true, time });
          } catch (error) {
            commandsState.output.push({ command, target, output: errorMessage(error), ok: false, time });
            throw error;
          } finally {
            if (commandsState.output.length > 200) commandsState.output.splice(0, commandsState.output.length - 200);
            const host = $("#cmd-output");
            if (host) {
              host.innerHTML = String(renderCommandOutput());
              host.scrollTop = host.scrollHeight;
            }
            const input = $("#cmd-input");
            if (input) {
              input.value = "";
              commandsState.text = "";
              input.focus();
            }
          }
        });
      }

      function filteredCatalog() {
        const query = commandsState.filter.trim().toLowerCase();
        const catalog = commandsState.catalog || [];
        if (!query) return catalog;
        return catalog.filter(command => [command.name, command.description, command.permission, ...(command.aliases || []), ...(command.subcommands || [])]
          .join(" ").toLowerCase().includes(query));
      }

      function renderCommandCatalog() {
        const commands = filteredCatalog();
        if (!commands.length) return emptyState(t("common.no_results"));
        return html`${commands.map(command => html`
          <button type="button" class="catalog-item ${commandsState.selected === command.name ? "active" : ""}" data-command="${command.name}">
            <span class="mono">/${command.name}</span>
            <span class="muted small">${(command.description || "").split("\n")[0]}</span>
            ${command.permission ? chip(command.permission, "subtle") : chip(t("commands.public"), "good")}
          </button>`)}`;
      }

      function renderCommandDetail() {
        const command = (commandsState.catalog || []).find(item => item.name === commandsState.selected);
        if (!command) return html`<div class="muted small">${t("commands.pick_command")}</div>`;
        return html`
          <div class="command-detail">
            <div class="row space-between">
              <strong class="mono">/${command.name}</strong>
              <button class="btn ghost small" type="button" data-insert="/${command.name} ">${t("commands.insert")}</button>
            </div>
            <p class="pre-wrap">${command.description}</p>
            ${command.usage ? html`<pre class="usage">${command.usage}</pre>` : ""}
            <div class="kv-grid">
              ${kv(t("commands.permission"), command.permission || t("commands.public"))}
              ${kv(t("commands.aliases"), (command.aliases || []).length ? command.aliases.join(", ") : t("common.none"))}
            </div>
            ${(command.subcommands || []).length ? html`
              <div class="field">
                <span>${t("commands.subcommands")}</span>
                <span class="chip-list">${command.subcommands.map(sub => html`<button type="button" class="chip clickable" data-insert="/${command.name} ${sub} ">${sub}</button>`)}</span>
              </div>` : ""}
          </div>`;
      }

      function bindCommandCatalog(root) {
        on(root, "[data-command]", "click", event => {
          commandsState.selected = event.currentTarget.dataset.command;
          for (const item of $$("[data-command]", root)) item.classList.toggle("active", item === event.currentTarget);
          $("#cmd-detail").innerHTML = String(renderCommandDetail());
          bindCommandDetail(root);
        });
      }

      function bindCommandDetail(root) {
        on($("#cmd-detail", root), "[data-insert]", "click", event => {
          const input = $("#cmd-input");
          input.value = event.currentTarget.dataset.insert;
          commandsState.text = input.value;
          input.focus();
          input.setSelectionRange(input.value.length, input.value.length);
        });
      }

""";
    }
}
