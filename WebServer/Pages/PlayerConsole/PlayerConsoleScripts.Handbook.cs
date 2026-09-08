namespace March7thHoney.WebServer.Pages;

public static partial class PlayerConsoleScripts
{
    private static string HandbookView()
    {
        return """
      // ============================================================= handbook
      const handbookState = { lang: "", section: "", query: "", error: "", limit: 300 };

      registerView({
        id: "handbook",
        async load() {
          await handbook.loadLanguages();
          if (!handbookState.lang || !handbook.languages.includes(handbookState.lang)) {
            handbookState.lang = handbook.defaultLang() || "";
          }
          handbookState.error = "";
          if (!handbookState.lang) return;
          try {
            const book = await handbook.load(handbookState.lang);
            if (!handbookState.section || !book.sections[handbookState.section]) {
              handbookState.section = book.sections.Item ? "Item" : (book.order[0] || "");
            }
          } catch (error) {
            handbookState.error = errorMessage(error);
          }
        },
        render(root) {
          renderHandbookView(root);
        }
      });

      function renderHandbookView(root) {
        const languages = handbook.languages || [];
        const book = handbookState.lang ? handbook.cache[handbookState.lang] : null;

        root.innerHTML = String(html`
          <section class="panel">
            <div class="toolbar">
              <select id="hb-lang" aria-label="${t("handbook.language")}" ${languages.length ? "" : "disabled"}>
                ${languages.length ? languages.map(lang => option(lang, lang, handbookState.lang)) : option("", t("handbook.unavailable"), "")}
              </select>
              <select id="hb-section" aria-label="${t("handbook.section")}" ${book ? "" : "disabled"}>
                ${(book?.order || []).map(section => option(section,
                  hasTranslation(`handbook.section.${section}`) ? t(`handbook.section.${section}`) : (book.sections[section]?.title || section),
                  handbookState.section))}
              </select>
              <input id="hb-search" type="search" placeholder="${t("handbook.search_placeholder")}" value="${handbookState.query}" ${book ? "" : "disabled"}>
              <span class="muted small" id="hb-meta"></span>
            </div>
            <div id="hb-results"></div>
          </section>`);

        renderHandbookResults();

        $("#hb-lang", root).addEventListener("change", async event => {
          handbookState.lang = event.currentTarget.value;
          handbookState.error = "";
          $("#hb-results").innerHTML = String(loadingState(t("handbook.loading")));
          try {
            const loaded = await handbook.load(handbookState.lang);
            if (!loaded.sections[handbookState.section]) handbookState.section = loaded.order[0] || "";
          } catch (error) {
            handbookState.error = errorMessage(error);
          }
          renderHandbookView(root);
        });
        $("#hb-section", root).addEventListener("change", event => {
          handbookState.section = event.currentTarget.value;
          renderHandbookResults();
        });
        // Repainting only the result table keeps the search box (and its IME buffer) alive while typing.
        $("#hb-search", root).addEventListener("input", debounce(event => {
          handbookState.query = event.target.value;
          renderHandbookResults();
        }, 150));
      }

      function renderHandbookResults() {
        const host = $("#hb-results");
        const meta = $("#hb-meta");
        if (!host) return;

        if (handbookState.error) {
          host.innerHTML = String(html`<div class="empty bad">${handbookState.error}</div>`);
          return;
        }
        const book = handbookState.lang ? handbook.cache[handbookState.lang] : null;
        if (!book) {
          host.innerHTML = String(emptyState(t("handbook.unavailable")));
          if (meta) meta.textContent = "";
          return;
        }
        const section = book.sections[handbookState.section];
        if (!section) {
          host.innerHTML = String(emptyState(t("common.no_results")));
          return;
        }

        const query = handbookState.query.trim().toLowerCase();
        // Sections without id:name pairs (the command cheatsheet) are plain text — show them as such.
        if (!section.entries.length) {
          const text = section.lines.filter(line => !query || line.toLowerCase().includes(query)).join("\n");
          host.innerHTML = String(html`<pre class="handbook-text">${text || t("common.no_results")}</pre>`);
          if (meta) meta.textContent = t("handbook.lines", { count: section.lines.length });
          return;
        }

        const matches = query
          ? section.entries.filter(entry => String(entry.id).startsWith(query) || entry.text.toLowerCase().includes(query))
          : section.entries;
        const shown = matches.slice(0, handbookState.limit);
        if (meta) meta.textContent = t("handbook.meta", { shown: shown.length, matches: matches.length, total: section.entries.length });
        if (!shown.length) {
          host.innerHTML = String(emptyState(t("common.no_results")));
          return;
        }

        const isItem = handbookState.section === "Item";
        const isAvatar = handbookState.section === "Avatar";
        host.innerHTML = String(html`
          <div class="table-wrap">
            <table>
              <thead><tr><th class="w-id">ID</th><th>${t("common.name")}</th><th class="w-actions"></th></tr></thead>
              <tbody>
                ${shown.map(entry => html`
                  <tr>
                    <td class="mono">${entry.id}</td>
                    <td>${entry.text}</td>
                    <td class="row-actions">
                      ${isItem && commandModel.allowed("give") ? html`<button class="btn ghost small" type="button" data-hb-give="${entry.id}">${t("handbook.to_give")}</button>` : ""}
                      ${isAvatar && commandModel.allowed("avatar") ? html`<button class="btn ghost small" type="button" data-hb-avatar="${entry.id}">${t("handbook.to_avatar")}</button>` : ""}
                      <button class="btn ghost small" type="button" data-hb-copy="${entry.id}">${t("common.copy")}</button>
                    </td>
                  </tr>`)}
              </tbody>
            </table>
          </div>`);

        on(host, "[data-hb-copy]", "click", event => copyText(event.currentTarget.dataset.hbCopy));
        on(host, "[data-hb-give]", "click", event => sendToConsole(`/give ${event.currentTarget.dataset.hbGive} x1`));
        on(host, "[data-hb-avatar]", "click", event => sendToConsole(`/avatar get ${event.currentTarget.dataset.hbAvatar}`));
      }

""";
    }
}
