namespace March7thHoney.WebServer.Pages;

public static partial class AdminPageScripts
{
    private static string MailModule()
    {
        return """
      // ================================================================ mail
      const mailState = {
        mode: "uids",
        uids: "",
        welcome: false,
        sender: "",
        title: "",
        content: "",
        templateId: 0,
        expireDays: 30,
        isStar: false,
        attachments: [],
        result: null,
        finderQuery: "",
        finderSection: "Item"
      };

      registerModule({
        id: "mail",
        icon: ICONS.mail,
        async load({ params }) {
          if (params.uid) {
            mailState.mode = "uids";
            mailState.uids = String(params.uid);
          }
          handbook.ensureDefault().then(() => {
            if (state.activeModule === "mail") {
              renderMailFinder();
              renderMailAttachments();
            }
          });
        },
        render(root) {
          renderMail(root);
        }
      });

      function renderMail(root) {
        root.innerHTML = String(html`
          <div class="split">
            <section class="panel">
              <div class="panel-head"><h3>${t("mail.compose")}</h3></div>
              <form id="mail-form" class="panel-body form-grid">
                <div class="field">
                  <span>${t("mail.recipients")}</span>
                  <div class="segmented">
                    <button type="button" class="${mailState.mode === "uids" ? "active" : ""}" data-mail-mode="uids">${t("mail.mode_uids")}</button>
                    <button type="button" class="${mailState.mode === "all" ? "active" : ""}" data-mail-mode="all">${t("mail.mode_all")}</button>
                  </div>
                </div>
                <div class="field"><span>${t("mail.welcome")}</span>${toggleSwitch("mail-welcome", mailState.welcome, t("mail.welcome_hint"))}</div>
                <label class="field wide ${mailState.mode === "all" ? "hidden" : ""}" id="mail-uids-field">
                  <span>${t("mail.uid_list")}</span>
                  <textarea id="mail-uids" spellcheck="false" placeholder="10001, 10002">${mailState.uids}</textarea>
                  <span class="muted small" id="mail-uid-count"></span>
                </label>
                <div class="wide notice warn ${mailState.mode === "all" ? "" : "hidden"}" id="mail-all-notice">${t("mail.all_notice")}</div>
                <div class="wide ${mailState.welcome ? "hidden" : ""}" id="mail-body-fields">
                  <div class="form-grid">
                    <label class="field"><span>${t("mail.sender")}</span><input id="mail-sender" maxlength="64" placeholder="${config.serverName || "March7thHoney"}" value="${mailState.sender}"></label>
                    <label class="field"><span>${t("mail.title")}</span><input id="mail-title" maxlength="128" required value="${mailState.title}"></label>
                    <label class="field wide"><span>${t("mail.content")}</span><textarea id="mail-content" rows="6" required>${mailState.content}</textarea></label>
                    <label class="field"><span>${t("mail.template")}</span><input id="mail-template" type="number" min="0" value="${mailState.templateId}"></label>
                    <label class="field"><span>${t("mail.expire_days")}</span><input id="mail-expire" type="number" min="1" max="3650" value="${mailState.expireDays}"></label>
                    <div class="field"><span>${t("mail.star")}</span>${toggleSwitch("mail-star", mailState.isStar, t("mail.star_hint"))}</div>
                  </div>
                </div>
                <div class="actions wide">
                  <button class="btn primary" type="submit">${t("mail.send")}</button>
                  <button class="btn ghost" type="button" id="mail-reset">${t("common.reset_form")}</button>
                </div>
              </form>
              <div id="mail-result"></div>
            </section>

            <div class="stack">
              <section class="panel ${mailState.welcome ? "hidden" : ""}" id="mail-attachments-panel">
                <div class="panel-head">
                  <h3>${t("mail.attachments")}</h3>
                  <button class="btn ghost small" type="button" id="mail-add-attachment">${t("mail.add_attachment")}</button>
                </div>
                <div id="mail-attachments"></div>
              </section>
              <section class="panel">
                <div class="panel-head">
                  <h3>${t("mail.item_finder")}</h3>
                  <span class="muted small">${t("mail.item_finder_hint")}</span>
                </div>
                <div class="panel-body stack">
                  <div class="input-row">
                    <select id="mail-finder-section" aria-label="${t("handbook.section")}">
                      ${["Item", "Avatar"].map(section => option(section, t(`handbook.section.${section}`), mailState.finderSection))}
                    </select>
                    <input id="mail-finder" type="search" placeholder="${t("mail.finder_placeholder")}" value="${mailState.finderQuery}">
                  </div>
                  <div id="mail-finder-results"></div>
                </div>
              </section>
            </div>
          </div>`);

        renderMailAttachments();
        renderMailFinder();
        renderMailResult();
        updateMailUidCount();

        on(root, "[data-mail-mode]", "click", event => {
          mailState.mode = event.currentTarget.dataset.mailMode;
          for (const button of $$("[data-mail-mode]", root)) button.classList.toggle("active", button === event.currentTarget);
          $("#mail-uids-field").classList.toggle("hidden", mailState.mode === "all");
          $("#mail-all-notice").classList.toggle("hidden", mailState.mode !== "all");
        });
        $("#mail-welcome").addEventListener("change", event => {
          mailState.welcome = event.currentTarget.checked;
          $("#mail-body-fields").classList.toggle("hidden", mailState.welcome);
          $("#mail-attachments-panel").classList.toggle("hidden", mailState.welcome);
        });
        $("#mail-uids").addEventListener("input", event => {
          mailState.uids = event.currentTarget.value;
          updateMailUidCount();
        });
        const bindText = (id, key, transform) => {
          const element = $(id);
          if (!element) return;
          element.addEventListener("input", event => {
            mailState[key] = transform ? transform(event.currentTarget.value) : event.currentTarget.value;
          });
        };
        bindText("#mail-sender", "sender");
        bindText("#mail-title", "title");
        bindText("#mail-content", "content");
        bindText("#mail-template", "templateId", value => Number(value) || 0);
        bindText("#mail-expire", "expireDays", value => Number(value) || 30);
        $("#mail-star").addEventListener("change", event => mailState.isStar = event.currentTarget.checked);
        $("#mail-add-attachment").addEventListener("click", () => {
          mailState.attachments.push({ item_id: "", count: 1, level: 1, rank: 0, promotion: 0, talent_level: 0 });
          renderMailAttachments();
        });
        $("#mail-finder-section").addEventListener("change", event => {
          mailState.finderSection = event.currentTarget.value;
          renderMailFinder();
        });
        $("#mail-finder").addEventListener("input", debounce(event => {
          mailState.finderQuery = event.target.value;
          renderMailFinder();
        }, 150));
        $("#mail-reset").addEventListener("click", () => {
          Object.assign(mailState, { uids: "", welcome: false, sender: "", title: "", content: "", templateId: 0, expireDays: 30, isStar: false, attachments: [], result: null });
          renderMail(root);
        });
        $("#mail-form").addEventListener("submit", event => {
          event.preventDefault();
          submitMail($("button[type=submit]", event.currentTarget));
        });
      }

      function updateMailUidCount() {
        const element = $("#mail-uid-count");
        if (!element) return;
        const count = parseUidList(mailState.uids).length;
        element.textContent = t("mail.uid_count", { count });
      }

      function renderMailAttachments() {
        const host = $("#mail-attachments");
        if (!host) return;
        if (!mailState.attachments.length) {
          host.innerHTML = String(emptyState(t("mail.no_attachments")));
          return;
        }
        host.innerHTML = String(html`
          <div class="table-wrap">
            <table class="attachments">
              <thead>
                <tr>
                  <th>${t("mail.item")}</th>
                  <th class="w-sm">${t("mail.count")}</th>
                  <th class="w-sm">${t("common.level")}</th>
                  <th class="w-sm">${t("mail.rank")}</th>
                  <th class="w-sm">${t("player.promotion")}</th>
                  <th class="w-sm">${t("mail.talent")}</th>
                  <th class="w-xs"></th>
                </tr>
              </thead>
              <tbody>
                ${mailState.attachments.map((item, index) => html`
                  <tr>
                    <td>
                      <input type="number" min="1" data-att-index="${index}" data-att-field="item_id" value="${item.item_id}" placeholder="ID">
                      <div class="muted small">${item.item_id ? (handbook.name("Item", item.item_id) || t("mail.unknown_item")) : ""}</div>
                    </td>
                    <td><input type="number" min="1" data-att-index="${index}" data-att-field="count" value="${item.count}"></td>
                    <td><input type="number" min="1" data-att-index="${index}" data-att-field="level" value="${item.level}"></td>
                    <td><input type="number" min="0" data-att-index="${index}" data-att-field="rank" value="${item.rank}"></td>
                    <td><input type="number" min="0" data-att-index="${index}" data-att-field="promotion" value="${item.promotion}"></td>
                    <td><input type="number" min="0" data-att-index="${index}" data-att-field="talent_level" value="${item.talent_level}"></td>
                    <td><button class="btn ghost small" type="button" data-att-remove="${index}" aria-label="${t("common.remove")}">&times;</button></td>
                  </tr>`)}
              </tbody>
            </table>
          </div>`);

        on(host, "[data-att-index]", "input", event => {
          const input = event.currentTarget;
          const item = mailState.attachments[Number(input.dataset.attIndex)];
          if (!item) return;
          item[input.dataset.attField] = input.value === "" ? "" : Number(input.value);
          if (input.dataset.attField === "item_id") {
            const hint = input.nextElementSibling;
            if (hint) hint.textContent = item.item_id ? (handbook.name("Item", item.item_id) || t("mail.unknown_item")) : "";
          }
        });
        on(host, "[data-att-remove]", "click", event => {
          mailState.attachments.splice(Number(event.currentTarget.dataset.attRemove), 1);
          renderMailAttachments();
        });
      }

      function renderMailFinder() {
        const host = $("#mail-finder-results");
        if (!host) return;
        const lang = handbook.defaultLang();
        const book = lang ? handbook.cache[lang] : null;
        if (!book) {
          host.innerHTML = String(loadingState(handbook.languages && !handbook.languages.length ? t("handbook.unavailable") : t("handbook.loading")));
          return;
        }
        const entries = book.sections[mailState.finderSection]?.entries || [];
        const query = mailState.finderQuery.trim().toLowerCase();
        if (!query) {
          host.innerHTML = String(emptyState(t("mail.finder_empty", { count: fmt.number(entries.length) })));
          return;
        }
        const results = entries.filter(entry => String(entry.id).startsWith(query) || entry.text.toLowerCase().includes(query)).slice(0, 40);
        if (!results.length) {
          host.innerHTML = String(emptyState(t("common.no_results")));
          return;
        }
        host.innerHTML = String(html`
          <div class="finder-list">
            ${results.map(entry => html`
              <button type="button" class="finder-item" data-finder-id="${entry.id}">
                <span class="mono">${entry.id}</span>
                <span>${entry.text}</span>
              </button>`)}
          </div>`);
        on(host, "[data-finder-id]", "click", event => {
          const id = Number(event.currentTarget.dataset.finderId);
          if (mailState.finderSection !== "Item") {
            copyText(id);
            return;
          }
          const empty = mailState.attachments.find(item => !item.item_id);
          if (empty) empty.item_id = id;
          else mailState.attachments.push({ item_id: id, count: 1, level: 1, rank: 0, promotion: 0, talent_level: 0 });
          renderMailAttachments();
          toast(t("mail.attachment_added", { name: handbook.label("Item", id) }), "good");
        });
      }

      function renderMailResult() {
        const host = $("#mail-result");
        if (!host) return;
        const result = mailState.result;
        if (!result) {
          host.innerHTML = "";
          return;
        }
        host.innerHTML = String(html`
          <div class="panel-body">
            <div class="notice ${result.failed ? "warn" : "good"}">
              <strong>${t("mail.result", { sent: result.sent, failed: result.failed })}</strong>
              ${result.errors?.length ? html`<ul class="error-list">${result.errors.map(error => html`<li>${error}</li>`)}</ul>` : ""}
            </div>
          </div>`);
      }

      async function submitMail(button) {
        const payload = {
          target: mailState.mode,
          uids: mailState.mode === "uids" ? parseUidList(mailState.uids) : [],
          welcome: mailState.welcome,
          sender: mailState.sender,
          title: mailState.title,
          content: mailState.content,
          template_id: mailState.templateId,
          expire_days: mailState.expireDays,
          is_star: mailState.isStar,
          attachments: mailState.attachments
            .filter(item => item.item_id)
            .map(item => ({
              item_id: Number(item.item_id),
              count: Number(item.count) || 1,
              level: Number(item.level) || 1,
              rank: Number(item.rank) || 0,
              promotion: Number(item.promotion) || 0,
              talent_level: Number(item.talent_level) || 0
            }))
        };
        if (mailState.mode === "uids" && !payload.uids.length) {
          toast(t("mail.no_recipients"), "bad");
          return;
        }
        if (!mailState.welcome && (!payload.title.trim() || !payload.content.trim())) {
          toast(t("mail.title_content_required"), "bad");
          return;
        }
        if (mailState.mode === "all") {
          if (!await confirmDialog({ title: t("mail.confirm_all_title"), message: t("mail.confirm_all_message"), danger: true, requireText: "ALL", confirmLabel: t("mail.send") })) return;
        }
        await run(button, async () => {
          mailState.result = await api("/admin/api/mail/send", { method: "POST", body: payload });
          renderMailResult();
          state.cache.overview = null;
          if (accountsState.mailbox.uid) accountsState.mailbox = { uid: null, data: null, loading: false, error: "" };
          return mailState.result;
        }, result => t("mail.sent_toast", { sent: result?.sent ?? 0 }));
      }

""";
    }
}
