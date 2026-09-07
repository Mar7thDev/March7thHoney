namespace March7thHoney.WebServer.Pages;

public static partial class AdminPageScripts
{
    private static string HandbookModule()
    {
        return """
      // ============================================================ handbook
      const handbookState = {
        lang: "",
        section: "",
        query: "",
        error: "",
        limit: 300
      };

      registerModule({
        id: "handbook",
        icon: ICONS.handbook,
        async load({ params }) {
          await handbook.loadLanguages();
          if (!handbookState.lang || !handbook.languages.includes(handbookState.lang)) handbookState.lang = handbook.defaultLang() || "";
          if (params.section) handbookState.section = params.section;
          if (params.q) handbookState.query = params.q;
          handbookState.error = "";
          if (handbookState.lang) {
            try {
              const book = await handbook.load(handbookState.lang);
              if (!handbookState.section || !book.sections[handbookState.section]) {
                handbookState.section = book.sections.Item ? "Item" : (book.order[0] || "");
              }
            } catch (error) {
              handbookState.error = errorMessage(error);
            }
          }
        },
        render(root) {
          renderHandbook(root);
        }
      });

      function renderHandbook(root) {
        const languages = handbook.languages || [];
        const book = handbookState.lang ? handbook.cache[handbookState.lang] : null;

        root.innerHTML = String(html`
          <section class="panel">
            <div class="toolbar wrap">
              <select id="hb-lang" aria-label="${t("handbook.language")}" ${languages.length ? "" : "disabled"}>
                ${languages.length ? languages.map(lang => option(lang, lang, handbookState.lang)) : option("", t("handbook.unavailable"), "")}
              </select>
              <select id="hb-section" aria-label="${t("handbook.section")}" ${book ? "" : "disabled"}>
                ${(book?.order || []).map(section => option(section, hasTranslation(`handbook.section.${section}`) ? t(`handbook.section.${section}`) : (book.sections[section]?.title || section), handbookState.section))}
              </select>
              <input id="hb-search" type="search" placeholder="${t("handbook.search_placeholder")}" value="${handbookState.query}" ${book ? "" : "disabled"}>
              <span class="muted small" id="hb-meta"></span>
            </div>
            <div id="hb-results"></div>
          </section>`);

        renderHandbookResults();

        $("#hb-lang").addEventListener("change", async event => {
          handbookState.lang = event.currentTarget.value;
          handbookState.error = "";
          $("#hb-results").innerHTML = String(loadingState(t("handbook.loading")));
          try {
            const loaded = await handbook.load(handbookState.lang);
            if (!loaded.sections[handbookState.section]) handbookState.section = loaded.order[0] || "";
          } catch (error) {
            handbookState.error = errorMessage(error);
          }
          renderHandbook(root);
        });
        $("#hb-section").addEventListener("change", event => {
          handbookState.section = event.currentTarget.value;
          renderHandbookResults();
        });
        $("#hb-search").addEventListener("input", debounce(event => {
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
        if (!section.entries.length) {
          const text = section.lines.filter(line => !query || line.toLowerCase().includes(query)).join("\n");
          host.innerHTML = String(html`<pre class="handbook-text">${text || t("common.no_results")}</pre>`);
          if (meta) meta.textContent = t("handbook.lines", { count: fmt.number(section.lines.length) });
          return;
        }
        const matches = query
          ? section.entries.filter(entry => String(entry.id).startsWith(query) || entry.text.toLowerCase().includes(query))
          : section.entries;
        const shown = matches.slice(0, handbookState.limit);
        if (meta) meta.textContent = t("handbook.meta", { shown: fmt.number(shown.length), matches: fmt.number(matches.length), total: fmt.number(section.entries.length) });
        if (!shown.length) {
          host.innerHTML = String(emptyState(t("common.no_results")));
          return;
        }
        host.innerHTML = String(html`
          <div class="table-wrap">
            <table>
              <thead><tr><th class="w-lg">ID</th><th>${t("common.name")}</th><th class="w-actions"></th></tr></thead>
              <tbody>
                ${shown.map(entry => html`
                  <tr>
                    <td class="mono">${entry.id}</td>
                    <td>${entry.text}</td>
                    <td class="row-actions">
                      <button class="btn ghost small" type="button" data-copy="${entry.id}">${t("common.copy")}</button>
                      ${handbookState.section === "Item" ? html`<button class="btn ghost small" type="button" data-mail-item="${entry.id}">${t("handbook.to_mail")}</button>` : ""}
                    </td>
                  </tr>`)}
              </tbody>
            </table>
          </div>`);
        on(host, "[data-copy]", "click", event => copyText(event.currentTarget.dataset.copy));
        on(host, "[data-mail-item]", "click", event => {
          const id = Number(event.currentTarget.dataset.mailItem);
          mailState.attachments.push({ item_id: id, count: 1, level: 1, rank: 0, promotion: 0, talent_level: 0 });
          toast(t("mail.attachment_added", { name: handbook.label("Item", id) }), "good");
        });
      }

""";
    }
}
