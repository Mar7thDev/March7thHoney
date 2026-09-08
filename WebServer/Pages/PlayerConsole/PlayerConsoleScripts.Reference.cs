namespace March7thHoney.WebServer.Pages;

public static partial class PlayerConsoleScripts
{
    private static string ReferenceView()
    {
        return """
      // ============================================================ reference
      const referenceState = { filter: "", selected: null, onlyAllowed: false };

      registerView({
        id: "reference",
        render(root) {
          renderReference(root);
        }
      });

      function filteredCommands() {
        const query = referenceState.filter.trim().toLowerCase();
        return state.catalog.filter(command => {
          if (referenceState.onlyAllowed && !command.allowed) return false;
          if (!query) return true;
          return [command.name, command.description, command.permission, ...(command.aliases || []), ...(command.subcommands || [])]
            .join(" ").toLowerCase().includes(query);
        });
      }

      function renderReference(root) {
        root.innerHTML = String(html`
          <div class="split">
            <section class="panel">
              <div class="panel-head">
                <h3>${t("reference.title")}</h3>
                ${chip(t("reference.count", { count: state.catalog.length, allowed: state.catalogMeta?.allowed ?? 0 }), "subtle")}
              </div>
              <div class="panel-body stack">
                <div class="row">
                  <input id="ref-filter" class="grow" type="search" placeholder="${t("reference.filter")}" value="${referenceState.filter}">
                  <label class="row small muted" style="gap:6px">
                    <input id="ref-only" type="checkbox" ${referenceState.onlyAllowed ? "checked" : ""}>
                    <span>${t("reference.only_allowed")}</span>
                  </label>
                </div>
                <div id="ref-list" class="catalog-list">${renderCommandList()}</div>
              </div>
            </section>
            <section class="panel">
              <div class="panel-head"><h3 id="ref-detail-title">${t("reference.usage")}</h3></div>
              <div class="panel-body" id="ref-detail">${renderCommandDetail()}</div>
            </section>
          </div>`);

        // Typing must never rebuild the search box: replacing the live input drops focus and IME composition.
        $("#ref-filter", root).addEventListener("input", debounce(event => {
          referenceState.filter = event.target.value;
          repaintCommandList(root);
        }, 120));
        // Same reason the accounts table does it this way: a checkbox must not re-render its own container,
        // or the page jumps back to the top under the player's cursor.
        $("#ref-only", root).addEventListener("change", event => {
          referenceState.onlyAllowed = event.currentTarget.checked;
          repaintCommandList(root);
        });
        bindCommandList(root);
        bindCommandDetail(root);
      }

      function repaintCommandList(root) {
        const host = $("#ref-list", root);
        if (!host) return;
        host.innerHTML = String(renderCommandList());
        bindCommandList(root);
      }

      function renderCommandList() {
        const commands = filteredCommands();
        if (!commands.length) return emptyState(t("common.no_results"));
        return html`${commands.map(command => html`
          <button type="button" class="catalog-item ${command.allowed ? "" : "locked"} ${referenceState.selected === command.name ? "active" : ""}" data-command="${command.name}">
            <span class="mono">/${command.name}</span>
            <span class="muted small">${(command.description || "").split("\n")[0]}</span>
            ${command.allowed ? chip(command.permission || t("reference.public"), "subtle") : chip(t("common.locked"), "warn")}
          </button>`)}`;
      }

      function renderCommandDetail() {
        const command = commandModel.resolve(referenceState.selected);
        if (!command) return html`<div class="muted small">${t("reference.pick")}</div>`;
        return html`
          <div class="command-detail">
            <div class="row space-between">
              <strong class="mono">/${command.name}</strong>
              ${command.allowed ? chip(t("reference.available"), "good") : chip(t("common.locked"), "warn")}
            </div>
            <p class="muted" style="white-space:pre-wrap">${command.description}</p>
            ${command.signatures.length ? html`
              <div class="stack">
                ${command.signatures.map(signature => html`
                  <div class="usage-line">
                    <code>${signature.text}</code>
                    <button class="btn ghost small" type="button" data-insert="${insertTemplate(signature)}">${t("common.insert")}</button>
                  </div>`)}
              </div>`
              : (command.usage ? html`<pre class="usage">${command.usage}</pre>` : "")}
            <div class="kv-grid">
              ${kv(t("reference.permission"), command.permission || t("reference.public"))}
              ${kv(t("reference.aliases"), (command.aliases || []).length ? command.aliases.join(", ") : t("common.none"))}
            </div>
            ${(command.subcommands || []).length ? html`
              <div class="field">
                <span>${t("reference.subcommands")}</span>
                <span class="chip-list">${command.subcommands.map(sub => html`
                  <button type="button" class="chip clickable" data-insert="/${command.name} ${sub} ">${sub}</button>`)}</span>
              </div>` : ""}
          </div>`;
      }

      /// Keeps the literal words of a usage line and stops at the first placeholder, so the player lands
      /// exactly where they have to type — "/scene prop " rather than "/scene prop <GroupID> <PropID>".
      function insertTemplate(signature) {
        const parts = [];
        for (const token of signature.tokens) {
          if (token.startsWith("<") || token.startsWith("[")) break;
          parts.push(token);
        }
        return `${parts.join(" ")} `;
      }

      function bindCommandList(root) {
        on(root, "[data-command]", "click", event => {
          referenceState.selected = event.currentTarget.dataset.command;
          for (const item of $$("[data-command]", root)) item.classList.toggle("active", item === event.currentTarget);
          $("#ref-detail", root).innerHTML = String(renderCommandDetail());
          bindCommandDetail(root);
        });
      }

      function bindCommandDetail(root) {
        on($("#ref-detail", root), "[data-insert]", "click", event => sendToConsole(event.currentTarget.dataset.insert));
      }

""";
    }
}
