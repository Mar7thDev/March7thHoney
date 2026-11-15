namespace March7thHoney.WebServer.Pages;

public static class PlayerConsoleStyles
{
    public static string Render()
    {
        return """
    :root {
      color-scheme: light;
      --bg: #f4f7fb;
      --bg-2: #eef3f8;
      --panel: #ffffff;
      --panel-2: #f7f9fc;
      --line: #dce3ec;
      --line-2: #c7d2e0;
      --ink: #182230;
      --muted: #5f6f82;
      --soft: #8492a6;
      --accent: #477dd8;
      --accent-2: #2f68c5;
      --accent-soft: rgba(71, 125, 216, 0.13);
      --green: #16835b;
      --green-soft: rgba(22, 131, 91, 0.11);
      --amber: #a96713;
      --amber-soft: rgba(169, 103, 19, 0.12);
      --red: #cc4545;
      --red-soft: rgba(204, 69, 69, 0.11);
      --shadow: 0 16px 40px rgba(33, 52, 79, 0.12);
      --radius: 14px;
      --radius-sm: 9px;
      --topbar: 60px;
      --font: "Inter", "Segoe UI", system-ui, -apple-system, "PingFang SC", "Microsoft YaHei", "Noto Sans SC", sans-serif;
      --mono: "JetBrains Mono", "Cascadia Code", "SFMono-Regular", Consolas, "Liberation Mono", Menlo, monospace;
    }

    * { box-sizing: border-box; }
    html { height: 100%; }
    body {
      margin: 0;
      min-height: 100vh;
      background: var(--bg);
      color: var(--ink);
      font-family: var(--font);
      font-size: 14px;
      line-height: 1.5;
      -webkit-font-smoothing: antialiased;
      -moz-osx-font-smoothing: grayscale;
    }
    a { color: var(--accent-2); }
    h1, h2, h3 { margin: 0; font-weight: 700; letter-spacing: -0.01em; }
    h1 { font-size: 21px; }
    h2 { font-size: 17px; }
    h3 { font-size: 15px; }
    p { margin: 0; }
    code, pre, .mono { font-family: var(--mono); font-size: 12.5px; }
    pre { margin: 0; white-space: pre-wrap; overflow-wrap: anywhere; }
    .hidden { display: none !important; }
    .muted { color: var(--muted); }
    .small { font-size: 12px; }
    .grow { flex: 1; min-width: 0; }
    .stack { display: flex; flex-direction: column; gap: 12px; }
    .row { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
    .space-between { justify-content: space-between; }
    .eyebrow {
      margin: 0 0 4px;
      color: var(--accent-2);
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0.12em;
      text-transform: uppercase;
    }
    ::selection { background: var(--accent-soft); }
    *:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }

    /* ---------------------------------------------------------- controls */
    button, input, select, textarea { font: inherit; color: inherit; }
    button { border: 0; background: none; cursor: pointer; }
    input, select, textarea {
      width: 100%;
      min-height: 38px;
      padding: 8px 12px;
      border: 1px solid var(--line);
      border-radius: var(--radius-sm);
      background: var(--bg-2);
      color: var(--ink);
      outline: none;
      transition: border-color .15s ease, box-shadow .15s ease;
    }
    input:focus, select:focus, textarea:focus { border-color: var(--accent); box-shadow: 0 0 0 3px var(--accent-soft); }
    input[type="checkbox"] { width: auto; min-height: 0; padding: 0; }
    select { appearance: none; padding-right: 30px; background-image: linear-gradient(45deg, transparent 50%, var(--soft) 50%), linear-gradient(135deg, var(--soft) 50%, transparent 50%); background-position: calc(100% - 16px) 17px, calc(100% - 11px) 17px; background-size: 5px 5px; background-repeat: no-repeat; }
    .field { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
    .field > span { color: var(--muted); font-size: 12px; font-weight: 600; }
    .input-row { display: flex; align-items: center; gap: 8px; }
    .input-row > input { flex: 1; min-width: 0; }
    .remember { display: inline-flex; align-items: center; gap: 8px; color: var(--muted); font-size: 13px; cursor: pointer; user-select: none; }
    .remember input { width: 16px; height: 16px; cursor: pointer; }

    .btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 6px;
      padding: 8px 15px;
      border: 1px solid transparent;
      border-radius: var(--radius-sm);
      font-size: 13px;
      font-weight: 600;
      white-space: nowrap;
      transition: background .15s ease, border-color .15s ease, color .15s ease;
    }
    .btn:disabled { opacity: .5; cursor: not-allowed; }
    .btn.primary { background: var(--accent); color: #fff; }
    .btn.primary:hover:not(:disabled) { background: var(--accent-2); }
    .btn.ghost { border-color: var(--line-2); background: var(--panel); color: var(--muted); }
    .btn.ghost:hover:not(:disabled) { border-color: var(--accent); color: var(--accent-2); }
    .btn.danger { background: var(--red); color: #fff; }
    .btn.small { padding: 5px 11px; font-size: 12px; }
    .btn.block { width: 100%; }

    .chip { display: inline-flex; align-items: center; gap: 5px; padding: 3px 9px; border-radius: 999px; background: var(--accent-soft); color: var(--accent-2); font-size: 11.5px; font-weight: 700; }
    .chip.subtle { background: var(--bg-2); color: var(--muted); }
    .chip.good { background: var(--green-soft); color: var(--green); }
    .chip.warn { background: var(--amber-soft); color: var(--amber); }
    .chip.bad { background: var(--red-soft); color: var(--red); }
    .chip.clickable { cursor: pointer; }
    .chip-list { display: flex; flex-wrap: wrap; gap: 6px; }

    .message { min-height: 18px; font-size: 12.5px; color: var(--muted); }
    .message.bad { color: var(--red); }
    .message.good { color: var(--green); }
    .notice { padding: 10px 13px; border: 1px solid var(--line-2); border-radius: var(--radius-sm); background: var(--panel-2); color: var(--muted); font-size: 12.5px; }
    .notice.warn { border-color: rgba(169, 103, 19, 0.35); background: var(--amber-soft); color: var(--amber); }
    .empty { padding: 26px 16px; color: var(--soft); text-align: center; font-size: 13px; }
    .empty.bad { color: var(--red); }

    /* -------------------------------------------------------------- login */
    .login-view { display: grid; place-items: center; min-height: 100vh; padding: 24px; }
    .login-card { width: min(100%, 420px); display: flex; flex-direction: column; gap: 16px; padding: 28px; border: 1px solid var(--line); border-radius: 18px; background: var(--panel); box-shadow: var(--shadow); }
    .login-links { display: flex; justify-content: center; gap: 16px; font-size: 12.5px; }
    .lang-switch { display: flex; gap: 4px; justify-content: center; padding: 3px; border-radius: 999px; background: var(--bg-2); }
    .lang-switch button { padding: 4px 12px; border-radius: 999px; color: var(--soft); font-size: 12px; font-weight: 600; }
    .lang-switch button.active { background: var(--panel); color: var(--accent-2); box-shadow: 0 1px 3px rgba(33, 52, 79, .12); }

    /* -------------------------------------------------------------- shell */
    .brand { display: flex; align-items: center; gap: 11px; min-width: 0; }
    .brand strong { display: block; font-size: 14px; }
    .brand > div > span { display: block; color: var(--muted); font-size: 11.5px; }
    .brand-mark { display: grid; place-items: center; flex: none; width: 38px; height: 38px; border-radius: 11px; overflow: hidden; background: linear-gradient(135deg, #5b7fe0, #a78bfa); color: #fff; font-weight: 800; font-size: 15px; }
    .brand-mark img { width: 100%; height: 100%; object-fit: cover; display: block; }
    .brand-mark span { display: grid; place-items: center; width: 100%; height: 100%; padding: 0 3px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

    .topbar { position: sticky; top: 0; z-index: 30; display: flex; align-items: center; justify-content: space-between; gap: 14px; height: var(--topbar); padding: 0 18px; border-bottom: 1px solid var(--line); background: rgba(255, 255, 255, .92); backdrop-filter: blur(8px); }
    .topbar-actions { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; justify-content: flex-end; }

    .tabs { position: sticky; top: var(--topbar); z-index: 25; display: flex; gap: 4px; padding: 8px 18px; border-bottom: 1px solid var(--line); background: rgba(255, 255, 255, .92); backdrop-filter: blur(8px); overflow-x: auto; }
    .tab { display: inline-flex; align-items: center; gap: 7px; padding: 7px 14px; border-radius: 999px; color: var(--muted); font-size: 13px; font-weight: 600; white-space: nowrap; }
    .tab:hover { background: var(--bg-2); color: var(--ink); }
    .tab.active { background: var(--accent-soft); color: var(--accent-2); }
    .tab svg { flex: none; }

    .content { padding: 20px 18px 64px; }
    .view-root { display: flex; flex-direction: column; gap: 16px; max-width: 1240px; margin: 0 auto; }

    .panel { border: 1px solid var(--line); border-radius: var(--radius); background: var(--panel); box-shadow: 0 1px 2px rgba(33, 52, 79, .04); overflow: hidden; }
    .panel-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 13px 16px; border-bottom: 1px solid var(--line); background: var(--panel-2); flex-wrap: wrap; }
    .panel-body { padding: 16px; }
    .split { display: grid; grid-template-columns: minmax(0, 1.35fr) minmax(0, 1fr); gap: 16px; align-items: start; }
    .grid.two { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
    .grid.three { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; }

    /* ------------------------------------------------------ quick actions */
    .quick-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 16px; }
    .quick-card { display: flex; flex-direction: column; gap: 12px; padding: 16px; border: 1px solid var(--line); border-radius: var(--radius); background: var(--panel); box-shadow: 0 1px 2px rgba(33, 52, 79, .04); }
    .quick-card.locked { opacity: .58; }
    .quick-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; }
    .quick-head h3 { display: flex; align-items: center; gap: 8px; }
    .quick-icon { display: grid; place-items: center; flex: none; width: 30px; height: 30px; border-radius: 9px; background: var(--accent-soft); color: var(--accent-2); }
    .quick-fields { display: grid; grid-template-columns: repeat(auto-fit, minmax(120px, 1fr)); gap: 10px; }
    .quick-fields .field.wide { grid-column: 1 / -1; }
    .quick-preview { display: flex; align-items: center; gap: 8px; padding: 9px 11px; border: 1px dashed var(--line-2); border-radius: var(--radius-sm); background: var(--bg-2); }
    .quick-preview code { flex: 1; min-width: 0; color: var(--accent-2); overflow-wrap: anywhere; }
    .quick-actions { display: flex; gap: 8px; flex-wrap: wrap; }
    .quick-card.danger-card { border-color: rgba(204, 69, 69, 0.35); background: #fffafa; }

    /* ------------------------------------------------------------- picker */
    .picker { position: relative; }
    .picker-list { position: absolute; z-index: 20; left: 0; right: 0; top: calc(100% + 4px); max-height: 260px; overflow: auto; border: 1px solid var(--line-2); border-radius: var(--radius-sm); background: var(--panel); box-shadow: var(--shadow); }
    .picker-item { display: grid; grid-template-columns: 84px 1fr; gap: 10px; width: 100%; padding: 7px 11px; border-bottom: 1px solid var(--line); text-align: left; color: var(--ink); font-size: 12.5px; }
    .picker-item:last-child { border-bottom: 0; }
    .picker-item:hover, .picker-item.active { background: var(--accent-soft); }
    .picker-item .id { color: var(--accent-2); font-family: var(--mono); }
    .picker-item .name { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

    /* ------------------------------------------------------------ console */
    .console-output { height: min(52vh, 460px); padding: 12px 14px; border: 1px solid var(--line); border-radius: var(--radius-sm); background: var(--bg); overflow: auto; font-family: var(--mono); font-size: 12.5px; }
    .console-entry { padding: 8px 0; border-bottom: 1px dashed var(--line); }
    .console-entry:last-child { border-bottom: 0; }
    .console-entry.bad .console-result { color: var(--red); }
    .console-line { display: flex; align-items: center; gap: 7px; flex-wrap: wrap; }
    .console-time { color: var(--soft); }
    .console-prompt { color: var(--green); font-weight: 700; font-family: var(--mono); }
    .console-command { color: var(--ink); font-weight: 600; overflow-wrap: anywhere; }
    .console-result { margin-top: 4px; color: var(--muted); }
    .console-form { position: relative; display: flex; align-items: center; gap: 8px; }
    .console-form input { font-family: var(--mono); }
    .console-hint { display: flex; gap: 8px; align-items: baseline; flex-wrap: wrap; min-height: 18px; padding: 0 2px; color: var(--soft); font-size: 12px; }
    .console-hint .sig { color: var(--accent-2); font-family: var(--mono); }
    .console-hint .arg-active { padding: 0 4px; border-radius: 4px; background: var(--accent-soft); color: var(--accent-2); font-weight: 700; }

    .suggest { position: absolute; z-index: 22; left: 22px; right: 88px; bottom: calc(100% + 6px); max-height: 250px; overflow: auto; border: 1px solid var(--line-2); border-radius: var(--radius-sm); background: var(--panel); box-shadow: var(--shadow); }
    .suggest-item { display: grid; grid-template-columns: minmax(110px, auto) 1fr auto; gap: 10px; align-items: center; width: 100%; padding: 7px 11px; border-bottom: 1px solid var(--line); text-align: left; color: var(--ink); font-size: 12.5px; }
    .suggest-item:last-child { border-bottom: 0; }
    .suggest-item.active { background: var(--accent-soft); }
    .suggest-item .label { color: var(--accent-2); font-family: var(--mono); font-weight: 700; }
    .suggest-item .desc { color: var(--muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

    /* ---------------------------------------------------------- reference */
    .catalog-list { max-height: 460px; overflow: auto; border: 1px solid var(--line); border-radius: var(--radius-sm); }
    .catalog-item { display: grid; grid-template-columns: 108px 1fr auto; align-items: center; gap: 10px; width: 100%; padding: 9px 12px; border-bottom: 1px solid var(--line); text-align: left; color: var(--ink); }
    .catalog-item:last-child { border-bottom: 0; }
    .catalog-item:hover { background: var(--panel-2); }
    .catalog-item.active { background: var(--accent-soft); }
    .catalog-item.locked { opacity: .55; }
    .catalog-item .muted { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .command-detail { display: flex; flex-direction: column; gap: 10px; }
    .usage { padding: 10px 12px; border-radius: 7px; background: var(--bg-2); color: var(--muted); font-size: 12px; }
    .usage-line { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 7px 10px; border: 1px solid var(--line); border-radius: 7px; background: var(--bg-2); }
    .usage-line code { flex: 1; min-width: 0; overflow-wrap: anywhere; }
    .kv-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 10px; }
    .kv { display: flex; flex-direction: column; gap: 2px; }
    .kv span { color: var(--soft); font-size: 11.5px; font-weight: 600; }

    /* ----------------------------------------------------------- handbook */
    .toolbar { display: flex; gap: 10px; align-items: center; padding: 13px 16px; border-bottom: 1px solid var(--line); background: var(--panel-2); flex-wrap: wrap; }
    .toolbar select { width: auto; min-width: 130px; }
    .toolbar input[type=search] { flex: 1; min-width: 180px; }
    .table-wrap { overflow: auto; max-height: 60vh; }
    table { width: 100%; border-collapse: collapse; font-size: 13px; }
    th, td { padding: 8px 12px; text-align: left; border-bottom: 1px solid var(--line); }
    th { position: sticky; top: 0; z-index: 1; background: var(--panel-2); color: var(--muted); font-size: 11.5px; font-weight: 700; letter-spacing: .04em; text-transform: uppercase; }
    tbody tr:hover { background: var(--panel-2); }
    .w-id { width: 130px; }
    .w-actions { width: 190px; }
    .row-actions { display: flex; gap: 6px; justify-content: flex-end; }
    .handbook-text { max-height: 60vh; overflow: auto; padding: 14px 18px; color: var(--muted); font-size: 12.5px; }

    /* ------------------------------------------------------------- toasts */
    .toast-host { position: fixed; right: 18px; bottom: 18px; z-index: 60; display: flex; flex-direction: column; gap: 8px; max-width: min(420px, calc(100vw - 36px)); }
    .toast { padding: 11px 14px; border: 1px solid var(--line-2); border-radius: var(--radius-sm); background: var(--panel); color: var(--ink); font-size: 13px; box-shadow: var(--shadow); opacity: 0; transform: translateY(8px); transition: opacity .2s ease, transform .2s ease; cursor: pointer; overflow-wrap: anywhere; }
    .toast.show { opacity: 1; transform: none; }
    .toast.good { border-color: rgba(22, 131, 91, .45); }
    .toast.bad { border-color: rgba(204, 69, 69, .45); background: #fff0f0; }

    /* -------------------------------------------------------------- modal */
    .modal-backdrop { position: fixed; inset: 0; z-index: 50; display: grid; place-items: center; padding: 20px; background: rgba(24, 34, 48, 0.28); backdrop-filter: blur(4px); }
    .modal { width: min(100%, 460px); padding: 22px; border: 1px solid var(--line-2); border-radius: 16px; background: var(--panel); box-shadow: var(--shadow); display: flex; flex-direction: column; gap: 14px; }
    .modal-message { max-height: 45vh; overflow-y: auto; color: var(--muted); font-size: 13.5px; white-space: pre-wrap; overflow-wrap: anywhere; }
    .modal-actions { display: flex; justify-content: flex-end; gap: 8px; }

    /* -------------------------------------------------------- responsive */
    @media (max-width: 1100px) {
      .split { grid-template-columns: minmax(0, 1fr); }
      .grid.three { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    }
    @media (max-width: 760px) {
      .topbar { height: auto; padding: 10px 14px; flex-wrap: wrap; }
      .tabs { top: 0; position: static; padding: 8px 14px; }
      .content { padding: 16px 14px 60px; }
      .grid.two, .grid.three { grid-template-columns: minmax(0, 1fr); }
      .quick-grid { grid-template-columns: minmax(0, 1fr); }
      .suggest { left: 0; right: 0; }
      .topbar .brand > div > span { display: none; }
    }
    @media (max-width: 820px) {
      input, select, textarea { font-size: 16px; }
    }
""";
    }
}
