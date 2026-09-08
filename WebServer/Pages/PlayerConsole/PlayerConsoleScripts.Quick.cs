namespace March7thHoney.WebServer.Pages;

public static partial class PlayerConsoleScripts
{
    private static string QuickView()
    {
        return """
      // ======================================================= quick actions
      // Declarative cards over the commands players reach for most. Each card owns its fields and a build()
      // that turns them into the exact command text, so the preview under the card is always what will run.
      const QUICK_ICONS = {
        bag: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M4 7h12l-1 9H5z"/><path d="M7.5 7V5.5a2.5 2.5 0 0 1 5 0V7"/></svg>',
        stack: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="m10 3 7 3.5-7 3.5-7-3.5z"/><path d="m3 10.5 7 3.5 7-3.5"/></svg>',
        user: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><circle cx="10" cy="7" r="3"/><path d="M4 16.5c0-2.8 2.7-4.5 6-4.5s6 1.7 6 4.5"/></svg>',
        level: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M3 16h4V9H3zM8 16h4V4H8zM13 16h4v-5h-4z"/></svg>',
        unlock: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><rect x="4" y="9" width="12" height="8" rx="2"/><path d="M7 9V6.5a3 3 0 0 1 5.7-1.3"/></svg>',
        map: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M3 5.5 7.5 4l5 1.5L17 4v10.5L12.5 16l-5-1.5L3 16z"/><path d="M7.5 4v10.5M12.5 5.5V16"/></svg>',
        team: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><circle cx="7" cy="7" r="2.4"/><circle cx="14" cy="8" r="2"/><path d="M2.5 16c0-2.4 2-3.8 4.5-3.8s4.5 1.4 4.5 3.8M13 12.4c2.3.2 4 1.5 4 3.6"/></svg>',
        flag: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M5 3v14M5 4h9l-2 3 2 3H5"/></svg>',
        clock: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><circle cx="10" cy="10" r="7"/><path d="M10 6v4.3l2.6 1.6"/></svg>',
        gem: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="m10 3 6 4-6 10L4 7z"/><path d="M4 7h12"/></svg>',
        terminal: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="m4 6 3 3-3 3M10 12h6"/></svg>',
        danger: '<svg viewBox="0 0 20 20" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"><path d="M10 3.5 17.5 16.5h-15z"/><path d="M10 8v3.5M10 14h.01"/></svg>'
      };

      const quickValues = {};

      /// Optional "x10"/"l80"/"r5" style suffix — omitted entirely when the field is blank or at its default.
      function opt(prefix, value, skipWhen) {
        const text = String(value ?? "").trim();
        if (!text || text === skipWhen) return "";
        return ` ${prefix}${text}`;
      }

      const QUICK_CARDS = [
        {
          id: "give", command: "give", icon: QUICK_ICONS.bag, title: "quick.give.title", desc: "quick.give.desc",
          fields: [
            { key: "item", type: "picker", section: "Item", label: "quick.give.item", wide: true, required: true },
            { key: "count", type: "number", label: "quick.give.count", value: "1", min: "1" },
            { key: "level", type: "number", label: "quick.give.level", placeholder: "1-80" },
            { key: "rank", type: "number", label: "quick.give.rank", placeholder: "1-5" }
          ],
          build: v => `/give ${v.item}${opt("x", v.count, "1")}${opt("l", v.level)}${opt("r", v.rank)}`
        },
        {
          id: "giveall", command: "giveall", icon: QUICK_ICONS.stack, title: "quick.giveall.title", desc: "quick.giveall.desc",
          fields: [
            {
              key: "kind", type: "select", label: "quick.giveall.kind", wide: true, required: true,
              options: ["avatar", "equipment", "material", "relic", "unlock", "skin", "train", "pet", "path"]
            },
            { key: "count", type: "number", label: "quick.give.count", placeholder: "1", when: v => ["material", "pet", "equipment", "relic"].includes(v.kind) },
            { key: "level", type: "number", label: "quick.give.level", placeholder: "1-80", when: v => ["avatar", "equipment", "relic"].includes(v.kind) },
            { key: "rank", type: "number", label: "quick.give.rank", placeholder: "1-6", when: v => ["avatar", "equipment"].includes(v.kind) }
          ],
          build: v => `/giveall ${v.kind}${opt("r", v.rank)}${opt("l", v.level)}${opt("x", v.count)}`
        },
        {
          id: "avatar", command: "avatar", icon: QUICK_ICONS.user, title: "quick.avatar.title", desc: "quick.avatar.desc",
          hint: "quick.avatar.all_hint",
          fields: [
            {
              key: "action", type: "select", label: "quick.avatar.action", required: true, value: "get",
              options: [
                { value: "get", label: "quick.avatar.get" },
                { value: "level", label: "quick.avatar.level" },
                { value: "rank", label: "quick.avatar.rank" },
                { value: "talent", label: "quick.avatar.talent" }
              ]
            },
            { key: "avatar", type: "picker", section: "Avatar", label: "quick.avatar.id", required: true },
            { key: "value", type: "number", label: "quick.avatar.value", wide: true, when: v => v.action !== "get", required: v => v.action !== "get" }
          ],
          build: v => v.action === "get" ? `/avatar get ${v.avatar}` : `/avatar ${v.action} ${v.avatar} ${v.value}`
        },
        {
          id: "setlevel", command: "setlevel", icon: QUICK_ICONS.level, title: "quick.level.title", desc: "quick.level.desc",
          fields: [{ key: "level", type: "number", label: "quick.level.value", wide: true, required: true, min: "1", max: "70", placeholder: "70" }],
          build: v => `/setlevel ${v.level}`
        },
        {
          id: "unlockall", command: "unlockall", icon: QUICK_ICONS.unlock, title: "quick.unlock.title", desc: "quick.unlock.desc",
          fields: [{
            key: "kind", type: "select", label: "quick.unlock.kind", wide: true, required: true,
            options: ["mission", "quest", "tutorial", "rogue", "challenge", "grid"]
          }],
          build: v => `/unlockall ${v.kind}`
        },
        {
          id: "scene", command: "scene", icon: QUICK_ICONS.map, title: "quick.scene.title", desc: "quick.scene.desc",
          fields: [
            {
              key: "action", type: "select", label: "quick.scene.action", required: true, value: "change",
              options: ["change", "reload", "unlockall", "reset", "cur", "near"]
            },
            { key: "value", type: "number", label: "quick.scene.entry", when: v => v.action === "change" || v.action === "reset", required: v => v.action === "change" || v.action === "reset" }
          ],
          build: v => (v.action === "change" || v.action === "reset") ? `/scene ${v.action} ${v.value}` : `/scene ${v.action}`
        },
        {
          id: "lineup", command: "lineup", icon: QUICK_ICONS.team, title: "quick.lineup.title", desc: "quick.lineup.desc",
          fields: [
            { key: "action", type: "select", label: "quick.avatar.action", required: true, value: "heal", options: ["heal", "mp"] },
            { key: "amount", type: "number", label: "quick.avatar.value", placeholder: "2", when: v => v.action === "mp" }
          ],
          build: v => v.action === "mp" ? `/lineup mp${opt("", v.amount)}` : "/lineup heal"
        },
        {
          id: "mission", command: "mission", icon: QUICK_ICONS.flag, title: "quick.mission.title", desc: "quick.mission.desc",
          fields: [
            { key: "action", type: "select", label: "quick.avatar.action", required: true, value: "repair", options: ["repair", "running", "finish", "finishmain", "reaccept", "goto"] },
            { key: "id", type: "picker", section: "MainMission", label: "quick.avatar.value", wide: true, when: v => !["repair", "running"].includes(v.action), required: v => !["repair", "running"].includes(v.action) }
          ],
          build: v => ["repair", "running"].includes(v.action) ? `/mission ${v.action}` : `/mission ${v.action} ${v.id}`
        },
        {
          id: "faketime", command: "faketime", icon: QUICK_ICONS.clock, title: "quick.faketime.title", desc: "quick.faketime.desc",
          fields: [{ key: "date", type: "text", label: "quick.faketime.date", wide: true, required: true, placeholder: "2026-08-17 / off" }],
          build: v => `/faketime ${v.date}`
        },
        {
          id: "relic", command: "relic", icon: QUICK_ICONS.gem, title: "quick.relic.title", desc: "quick.relic.desc",
          fields: [{ key: "args", type: "text", label: "quick.relic.args", wide: true, required: true, mono: true, placeholder: "61011 31101 63103:6 63203:6 63303:6 63403:6 l15" }],
          build: v => `/relic ${v.args}`
        },
        {
          id: "raw", icon: QUICK_ICONS.terminal, title: "quick.raw.title", desc: "quick.raw.desc",
          fields: [{ key: "command", type: "text", label: "quick.raw.title", wide: true, required: true, mono: true, placeholder: "/help" }],
          build: v => v.command.startsWith("/") ? v.command : `/${v.command}`
        }
      ];

      registerView({
        id: "quick",
        async load() {
          // Ids are typed as names in the pickers, so the handbook has to be there before the first render.
          await handbook.ensureDefault();
        },
        render(root) {
          renderQuick(root);
        }
      });

      function quickCards() {
        return QUICK_CARDS.filter(card => !card.command || commandModel.resolve(card.command));
      }

      function renderQuick(root) {
        const cards = quickCards();
        root.innerHTML = String(html`
          ${state.player?.online ? "" : html`<div class="notice warn">${t("shell.offline_hint")}</div>`}
          <section class="panel">
            <div class="panel-head">
              <div>
                <h3>${t("quick.title")}</h3>
                <div class="muted small">${t("quick.subtitle")}</div>
              </div>
              ${chip(t("shell.level", { level: state.player?.level ?? 0 }), "subtle")}
            </div>
            <div class="panel-body">
              ${cards.length ? html`<div class="quick-grid">${cards.map(card => renderQuickCard(card))}</div>`
                : emptyState(t("quick.no_permission"))}
            </div>
          </section>
          ${renderResetCard()}`);

        bindQuickCards(root);
      }

      function renderQuickCard(card) {
        const command = card.command ? commandModel.resolve(card.command) : null;
        const allowed = !command || command.allowed;
        const values = quickCardValues(card);
        return html`
          <article class="quick-card ${allowed ? "" : "locked"}" data-quick-card="${card.id}">
            <div class="quick-head">
              <h3><span class="quick-icon">${raw(card.icon || "")}</span>${t(card.title)}</h3>
              ${allowed ? "" : chip(t("common.locked"), "warn")}
            </div>
            <p class="muted small">${t(card.desc)}</p>
            ${card.hint ? html`<p class="muted small">${t(card.hint)}</p>` : ""}
            <div class="quick-fields">${visibleFields(card, values).map(field => renderQuickField(card, field, values))}</div>
            <div class="quick-preview">
              <code data-quick-preview="${card.id}">${quickCommand(card, values) || "—"}</code>
              <button class="btn ghost small" type="button" data-quick-copy="${card.id}">${t("common.copy")}</button>
            </div>
            <div class="quick-actions">
              <button class="btn primary small" type="button" data-quick-run="${card.id}" ${allowed ? "" : "disabled"}>${t("common.run")}</button>
              <button class="btn ghost small" type="button" data-quick-send="${card.id}">${t("common.insert")}</button>
              ${allowed ? "" : html`<span class="muted small">${t("quick.locked_hint", { role: state.player?.role || "" })}</span>`}
            </div>
          </article>`;
      }

      function renderQuickField(card, field, values) {
        const value = values[field.key] ?? "";
        const label = hasTranslation(field.label) ? t(field.label) : field.label;
        const control = (() => {
          switch (field.type) {
            case "select":
              return html`<select data-quick-field="${field.key}">${(field.options || []).map(item => {
                const optionValue = typeof item === "string" ? item : item.value;
                const optionLabel = typeof item === "string" ? item : (hasTranslation(item.label) ? t(item.label) : item.label);
                return option(optionValue, optionLabel, value);
              })}</select>`;
            case "number":
              return html`<input type="number" data-quick-field="${field.key}" value="${value}"
                            ${field.min ? `min="${field.min}"` : ""} ${field.max ? `max="${field.max}"` : ""}
                            placeholder="${field.placeholder || ""}">`;
            case "picker":
              return html`
                <div class="picker">
                  <input type="text" class="${field.mono ? "mono" : ""}" data-quick-field="${field.key}"
                         data-picker-section="${field.section}" autocomplete="off" spellcheck="false"
                         value="${value}" placeholder="${field.placeholder || ""}">
                  <div class="picker-list hidden" data-picker-list="${field.key}"></div>
                </div>`;
            default:
              return html`<input type="text" class="${field.mono ? "mono" : ""}" data-quick-field="${field.key}"
                            value="${value}" placeholder="${field.placeholder || ""}">`;
          }
        })();

        const resolved = field.type === "picker" && value ? handbook.name(field.section, handbook.resolveId(field.section, value)) : "";
        return html`
          <label class="field ${field.wide ? "wide" : ""}">
            <span>${label}</span>
            ${control}
            ${resolved ? html`<span class="muted small">${resolved}</span>` : ""}
          </label>`;
      }

      function renderResetCard() {
        return html`
          <article class="quick-card danger-card">
            <div class="quick-head">
              <h3><span class="quick-icon">${raw(QUICK_ICONS.danger)}</span>${t("quick.reset.title")}</h3>
            </div>
            <p class="muted small">${t("quick.reset.desc")}</p>
            <div class="quick-actions">
              <button class="btn danger small" type="button" id="quick-reset">${t("quick.reset.run")}</button>
            </div>
          </article>`;
      }

      // -------------------------------------------------------------- values
      function quickCardValues(card) {
        const values = {};
        for (const field of card.fields || []) {
          const stored = quickValues[`${card.id}.${field.key}`];
          values[field.key] = stored !== undefined ? stored : (field.value ?? defaultFieldValue(field));
        }
        return values;
      }

      /// A <select> always shows its first option, so the state has to start there too or the preview
      /// would claim the card is incomplete while the player is looking at a valid choice.
      function defaultFieldValue(field) {
        if (field.type !== "select") return "";
        const first = (field.options || [])[0];
        return first === undefined ? "" : (typeof first === "string" ? first : first.value);
      }

      function visibleFields(card, values) {
        return (card.fields || []).filter(field => !field.when || field.when(values));
      }

      function isRequired(field, values) {
        return typeof field.required === "function" ? field.required(values) : Boolean(field.required);
      }

      /// Resolves picker fields (name -> id) and returns "" when a required field is still empty.
      function quickCommand(card, values) {
        const resolved = { ...values };
        for (const field of visibleFields(card, values)) {
          const value = String(resolved[field.key] ?? "").trim();
          if (field.type === "picker") resolved[field.key] = handbook.resolveId(field.section, value);
          else resolved[field.key] = value;
          if (isRequired(field, values) && !String(resolved[field.key] || "").length) return "";
        }
        try {
          return String(card.build(resolved) || "").replace(/\s+/g, " ").trim();
        } catch {
          return "";
        }
      }

      function refreshQuickCard(cardId, root) {
        const card = QUICK_CARDS.find(item => item.id === cardId);
        const host = $(`[data-quick-card="${cardId}"]`, root);
        if (!card || !host) return;
        host.outerHTML = String(renderQuickCard(card));
        bindQuickCards(root);
      }

      function updateQuickPreview(cardId, root) {
        const card = QUICK_CARDS.find(item => item.id === cardId);
        if (!card) return;
        const preview = $(`[data-quick-preview="${cardId}"]`, root);
        if (preview) preview.textContent = quickCommand(card, quickCardValues(card)) || "—";
      }

      // ------------------------------------------------------------- binding
      function bindQuickCards(root) {
        for (const host of $$("[data-quick-card]", root)) {
          const cardId = host.dataset.quickCard;
          const card = QUICK_CARDS.find(item => item.id === cardId);
          if (!card) continue;

          on(host, "[data-quick-field]", "input", event => {
            const key = event.currentTarget.dataset.quickField;
            quickValues[`${cardId}.${key}`] = event.currentTarget.value;
            updateQuickPreview(cardId, root);
            if (event.currentTarget.dataset.pickerSection) renderPicker(host, cardId, event.currentTarget);
          });
          // A select can switch which fields apply, so it rebuilds the card; text inputs never do,
          // because replacing a live input would drop focus and IME composition mid-word.
          on(host, "select[data-quick-field]", "change", event => {
            quickValues[`${cardId}.${event.currentTarget.dataset.quickField}`] = event.currentTarget.value;
            refreshQuickCard(cardId, root);
          });
          on(host, "[data-picker-section]", "focus", event => renderPicker(host, cardId, event.currentTarget));
          on(host, "[data-picker-section]", "blur", event => {
            const list = $(`[data-picker-list="${event.currentTarget.dataset.quickField}"]`, host);
            setTimeout(() => list?.classList.add("hidden"), 150);
          });

          on(host, "[data-quick-copy]", "click", () => {
            const command = quickCommand(card, quickCardValues(card));
            if (command) copyText(command);
          });
          on(host, "[data-quick-send]", "click", () => {
            const command = quickCommand(card, quickCardValues(card));
            if (!command) { toast(t("console.empty"), "bad"); return; }
            sendToConsole(command);
          });
          on(host, "[data-quick-run]", "click", async event => {
            const command = quickCommand(card, quickCardValues(card));
            if (!command) { toast(t("console.empty"), "bad"); return; }
            const result = await execute(command, event.currentTarget);
            if (result) toast(result.output || t("console.no_result"), "good");
          });
        }

        const reset = $("#quick-reset", root);
        if (reset) reset.addEventListener("click", () => selfReset(reset));
      }

      function renderPicker(host, cardId, input) {
        const section = input.dataset.pickerSection;
        const list = $(`[data-picker-list="${input.dataset.quickField}"]`, host);
        if (!list) return;
        const matches = handbook.search(section, input.value, 30);
        if (!matches.length) {
          list.classList.add("hidden");
          list.innerHTML = "";
          return;
        }
        list.classList.remove("hidden");
        list.innerHTML = String(html`${matches.map(entry => html`
          <button type="button" class="picker-item" data-pick="${entry.id}">
            <span class="id">${entry.id}</span><span class="name">${entry.text}</span>
          </button>`)}`);
        on(list, "[data-pick]", "mousedown", event => {
          event.preventDefault();
          input.value = event.currentTarget.dataset.pick;
          quickValues[`${cardId}.${input.dataset.quickField}`] = input.value;
          list.classList.add("hidden");
          refreshQuickCard(cardId, els.viewRoot);
        });
      }

      async function selfReset(button) {
        const uid = state.player?.uid ?? "";
        if (!await confirmDialog({
          title: t("quick.reset.title"),
          message: t("quick.reset.confirm", { uid }),
          confirmLabel: t("quick.reset.run"),
          danger: true
        })) return;

        await run(button, async () => {
          const payload = await api("/console/self-reset", { confirm: "true" });
          pushConsoleEntry("/console/self-reset", payload.output || t("quick.reset.done"), true);
          clearSession(false);
          return payload;
        }, t("quick.reset.done"));
      }

""";
    }
}
