namespace March7thHoney.WebServer.Pages;

public static partial class PlayerConsoleScripts
{
    private static string ConsoleView()
    {
        return """
      // ============================================================== console
      const consoleState = {
        text: "",
        output: [],
        history: [],
        historyIndex: -1,
        suggestions: [],
        suggestIndex: -1,
        suggestOpen: false
      };

      try {
        consoleState.history = JSON.parse(readStorage(STORAGE.history) || "[]");
        if (!Array.isArray(consoleState.history)) consoleState.history = [];
      } catch {
        consoleState.history = [];
      }

      registerView({
        id: "console",
        async load() {
          // Names make the id completions readable; a missing handbook only costs the suggestions.
          await handbook.ensureDefault();
        },
        render(root) {
          renderConsole(root);
        }
      });

      function renderConsole(root) {
        root.innerHTML = String(html`
          ${state.player?.online ? "" : html`<div class="notice warn">${t("shell.offline_hint")}</div>`}
          <section class="panel">
            <div class="panel-head">
              <h3>${t("console.title")}</h3>
              <div class="row">
                ${chip(t("reference.count", { count: state.catalog.length, allowed: state.catalogMeta?.allowed ?? 0 }), "subtle")}
                <button class="btn ghost small" type="button" id="cmd-clear">${t("console.clear")}</button>
              </div>
            </div>
            <div class="panel-body stack">
              <div id="cmd-output" class="console-output" aria-live="polite">${renderConsoleOutput()}</div>
              <form id="cmd-form" class="console-form" autocomplete="off">
                <span class="console-prompt">&gt;</span>
                <input id="cmd-input" class="mono" autocomplete="off" spellcheck="false"
                       aria-autocomplete="list" placeholder="${t("console.placeholder")}" value="${consoleState.text}">
                <button class="btn primary" type="submit">${t("console.run")}</button>
                <div id="cmd-suggest" class="suggest hidden"></div>
              </form>
              <div id="cmd-hint" class="console-hint"></div>
              <div class="muted small">${t("console.hint")}</div>
            </div>
          </section>`);

        const input = $("#cmd-input", root);
        input.addEventListener("input", () => {
          consoleState.text = input.value;
          consoleState.historyIndex = -1;
          updateSuggestions(input);
          renderSignature(input);
        });
        input.addEventListener("click", () => {
          updateSuggestions(input);
          renderSignature(input);
        });
        input.addEventListener("keydown", event => onConsoleKeyDown(event, input));
        input.addEventListener("blur", () => setTimeout(closeSuggestions, 120));

        $("#cmd-form", root).addEventListener("submit", async event => {
          event.preventDefault();
          closeSuggestions();
          const button = $("button[type=submit]", event.currentTarget);
          const command = input.value.trim();
          if (!command) {
            toast(t("console.empty"), "bad");
            return;
          }
          rememberHistory(command);
          const result = await execute(command, button);
          if (result) {
            input.value = "";
            consoleState.text = "";
            renderSignature(input);
          }
          input.focus();
        });

        $("#cmd-clear", root).addEventListener("click", () => {
          consoleState.output = [];
          $("#cmd-output").innerHTML = String(renderConsoleOutput());
        });

        scrollConsoleToEnd();
        // Focus and park the caret at the end first: the signature hint reads selectionStart, which is 0 on an
        // input that has never been focused, and would otherwise show nothing for a pre-filled command.
        input.focus();
        input.setSelectionRange(input.value.length, input.value.length);
        renderSignature(input);
      }

      function renderConsoleOutput() {
        if (!consoleState.output.length) return emptyState(t("console.no_output"));
        return html`${consoleState.output.map(entry => html`
          <div class="console-entry ${entry.ok ? "" : "bad"}">
            <div class="console-line">
              <span class="console-time">${entry.time}</span>
              <span class="console-prompt">&gt;</span>
              <span class="console-command">${entry.command}</span>
            </div>
            <pre class="console-result">${entry.output}</pre>
          </div>`)}`;
      }

      /// Appends one result. Called from every tab, so it refreshes the log only when it is on screen.
      function pushConsoleEntry(command, output, ok) {
        consoleState.output.push({
          command,
          output,
          ok: ok !== false,
          time: new Date().toLocaleTimeString(state.lang, { hour12: false })
        });
        if (consoleState.output.length > 200) consoleState.output.splice(0, consoleState.output.length - 200);
        const host = $("#cmd-output");
        if (host) {
          host.innerHTML = String(renderConsoleOutput());
          scrollConsoleToEnd();
        }
      }

      function scrollConsoleToEnd() {
        const host = $("#cmd-output");
        if (host) host.scrollTop = host.scrollHeight;
      }

      function rememberHistory(command) {
        consoleState.history = consoleState.history.filter(item => item !== command);
        consoleState.history.push(command);
        if (consoleState.history.length > 100) consoleState.history.shift();
        consoleState.historyIndex = -1;
        writeStorage(STORAGE.history, JSON.stringify(consoleState.history));
      }

      /// Fills the console input from another tab and brings it forward.
      function sendToConsole(command) {
        consoleState.text = String(command || "");
        if (state.activeTab === "console") {
          const input = $("#cmd-input");
          if (input) {
            input.value = consoleState.text;
            input.focus();
            input.setSelectionRange(input.value.length, input.value.length);
            renderSignature(input);
            return;
          }
        }
        activateTab("console");
      }

      // ---------------------------------------------------------- completion
      function onConsoleKeyDown(event, input) {
        if (consoleState.suggestOpen && consoleState.suggestions.length) {
          if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            const delta = event.key === "ArrowDown" ? 1 : -1;
            const total = consoleState.suggestions.length;
            consoleState.suggestIndex = (consoleState.suggestIndex + delta + total) % total;
            paintSuggestions();
            return;
          }
          if (event.key === "Tab" || (event.key === "Enter" && consoleState.suggestIndex >= 0)) {
            event.preventDefault();
            applySuggestion(input, consoleState.suggestions[Math.max(consoleState.suggestIndex, 0)]);
            return;
          }
          if (event.key === "Escape") {
            event.preventDefault();
            closeSuggestions();
            return;
          }
        }

        if (event.key === "Tab") {
          // Nothing open yet: open the list so Tab always means "help me finish this".
          event.preventDefault();
          updateSuggestions(input, true);
          if (consoleState.suggestions.length === 1) applySuggestion(input, consoleState.suggestions[0]);
          return;
        }

        if (event.key === "Enter") {
          // Submit explicitly rather than relying on the form's implicit submission, which the game
          // client's embedded browser does not always perform for a single-input form.
          event.preventDefault();
          const form = input.closest("form");
          if (!form) return;
          if (form.requestSubmit) form.requestSubmit();
          else form.dispatchEvent(new Event("submit", { cancelable: true }));
          return;
        }

        if (event.key === "ArrowUp" || event.key === "ArrowDown") {
          if (!consoleState.history.length) return;
          event.preventDefault();
          if (event.key === "ArrowUp") {
            consoleState.historyIndex = consoleState.historyIndex < 0
              ? consoleState.history.length - 1
              : Math.max(0, consoleState.historyIndex - 1);
          } else {
            consoleState.historyIndex =
              consoleState.historyIndex < 0 || consoleState.historyIndex >= consoleState.history.length - 1
                ? -1
                : consoleState.historyIndex + 1;
          }
          input.value = consoleState.historyIndex < 0 ? "" : consoleState.history[consoleState.historyIndex];
          consoleState.text = input.value;
          input.setSelectionRange(input.value.length, input.value.length);
          renderSignature(input);
        }
      }

      /// Builds the candidate list for the token the caret sits on.
      function buildSuggestions(text, caret) {
        const info = describeInput(text, caret);
        const tokens = info.tokens;
        const typing = /\s$/.test(String(text).slice(0, caret)) ? "" : String(tokens[tokens.length - 1] || "");

        if (info.index === 0) {
          const prefix = typing.replace(/^\//, "").toLowerCase();
          const items = [];
          for (const command of state.catalog) {
            if (prefix && !command.name.toLowerCase().startsWith(prefix)) {
              const alias = (command.aliases || []).find(value => String(value).toLowerCase().startsWith(prefix));
              if (!alias) continue;
              items.push({ value: `/${alias}`, label: `/${alias}`, desc: command.description, kind: t("console.suggest_alias"), allowed: command.allowed });
              continue;
            }
            items.push({ value: `/${command.name}`, label: `/${command.name}`, desc: command.description, kind: t("console.suggest_command"), allowed: command.allowed });
          }
          items.sort((a, b) => (b.allowed === true) - (a.allowed === true));
          return items.slice(0, 40);
        }

        const command = info.command;
        if (!command) return [];

        // Literal subcommands come from the usage lines, so "/scene " offers group / prop / change / ...
        const literals = new Set();
        for (const signature of command.signatures) {
          const expected = signature.tokens[info.index];
          if (!expected) continue;
          if (expected.startsWith("<") || expected.startsWith("[")) continue;
          // Only offer it when everything before it already matches what was typed.
          let matches = true;
          for (let i = 1; i < info.index; i++) {
            const previous = signature.tokens[i];
            if (!previous || previous.startsWith("<") || previous.startsWith("[")) continue;
            if (previous.toLowerCase() !== String(tokens[i] || "").toLowerCase()) { matches = false; break; }
          }
          if (matches) literals.add(expected);
        }
        if (info.index === 1) for (const sub of command.subcommands || []) literals.add(String(sub).split(" ")[0]);

        const lower = typing.toLowerCase();
        const subItems = [...literals]
          .filter(value => !lower || value.toLowerCase().startsWith(lower))
          .sort((a, b) => a.localeCompare(b))
          .map(value => ({ value, label: value, desc: "", kind: t("console.suggest_sub") }));
        if (subItems.length) return subItems.slice(0, 40);

        // Otherwise the caret is on a value slot — complete ids from the matching handbook section.
        const placeholder = info.signature?.tokens[info.index] || "";
        const section = sectionForArgument(command.name, placeholder);
        if (!section) return [];
        const bare = typing.replace(/^[xlr]/i, "");
        return handbook.search(section, bare, 30).map(entry => ({
          value: String(entry.id),
          label: String(entry.id),
          desc: entry.text,
          kind: t("console.suggest_id")
        }));
      }

      function updateSuggestions(input, force) {
        const caret = input.selectionStart ?? input.value.length;
        consoleState.suggestions = buildSuggestions(input.value, caret);
        consoleState.suggestIndex = consoleState.suggestions.length ? 0 : -1;
        consoleState.suggestOpen = consoleState.suggestions.length > 0 && (force || input.value.trim().length > 0);
        paintSuggestions();
      }

      function paintSuggestions() {
        const host = $("#cmd-suggest");
        if (!host) return;
        if (!consoleState.suggestOpen || !consoleState.suggestions.length) {
          host.classList.add("hidden");
          host.innerHTML = "";
          return;
        }
        host.classList.remove("hidden");
        host.innerHTML = String(html`${consoleState.suggestions.map((item, index) => html`
          <button type="button" class="suggest-item ${index === consoleState.suggestIndex ? "active" : ""}" data-suggest="${index}">
            <span class="label">${item.label}</span>
            <span class="desc">${(item.desc || "").split("\n")[0]}</span>
            ${item.allowed === false ? chip(t("common.locked"), "warn") : chip(item.kind, "subtle")}
          </button>`)}`);
        on(host, "[data-suggest]", "mousedown", event => {
          event.preventDefault();
          applySuggestion($("#cmd-input"), consoleState.suggestions[Number(event.currentTarget.dataset.suggest)]);
        });
        const active = $(".suggest-item.active", host);
        if (active) active.scrollIntoView({ block: "nearest" });
      }

      function closeSuggestions() {
        consoleState.suggestOpen = false;
        consoleState.suggestIndex = -1;
        paintSuggestions();
      }

      /// Replaces the token under the caret with the chosen value and leaves the caret after it.
      function applySuggestion(input, item) {
        if (!input || !item) return;
        const caret = input.selectionStart ?? input.value.length;
        const before = input.value.slice(0, caret);
        const after = input.value.slice(caret);
        const tokenStart = /\s$/.test(before) ? before.length : before.length - (/[^\s]*$/.exec(before)?.[0].length || 0);
        // Keep an option prefix the player already typed ("x10" -> "x" + id).
        const typed = before.slice(tokenStart);
        const prefix = item.kind === t("console.suggest_id") && /^[xlr]/i.test(typed) ? typed[0] : "";

        input.value = `${before.slice(0, tokenStart)}${prefix}${item.value} ${after.replace(/^\s+/, "")}`;
        const nextCaret = tokenStart + prefix.length + String(item.value).length + 1;
        consoleState.text = input.value;
        input.focus();
        input.setSelectionRange(nextCaret, nextCaret);
        closeSuggestions();
        renderSignature(input);
      }

      /// The line under the input: the matched usage with the argument the caret is on highlighted.
      function renderSignature(input) {
        const host = $("#cmd-hint");
        if (!host) return;
        const caret = input.selectionStart ?? input.value.length;
        const info = describeInput(input.value, caret);
        if (!info.command || !info.signature) {
          host.innerHTML = "";
          return;
        }
        const tokens = info.signature.tokens;
        host.innerHTML = String(html`
          <span>${t("console.signature")}</span>
          <span class="sig">${tokens.map((token, index) => index === info.argIndex && index > 0
            ? html`<span class="arg-active">${token}</span> `
            : html`${token} `)}</span>
          ${info.command.allowed ? "" : chip(t("common.locked"), "warn")}`);
      }

""";
    }
}
