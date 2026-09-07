using System.Net;
using System.Text.Json;
using March7thHoney.Command;
using March7thHoney.Util;

namespace March7thHoney.WebServer.Pages;

/// <summary>
///     Renders the single-page admin console. Everything (styles, translations, scripts) is inlined so the page
///     works with no static file directory and no external dependencies.
/// </summary>
public static class AdminPageRenderer
{
    public static string Render(string serverName)
    {
        var displayName = string.IsNullOrWhiteSpace(serverName) ? "March7thHoney" : serverName.Trim();
        var encodedDisplayName = WebUtility.HtmlEncode(displayName);
        var encodedTitle = WebUtility.HtmlEncode($"Admin console - {displayName}");
        var encodedInitials = WebUtility.HtmlEncode(BuildInitials(displayName));
        var serverNameJson = JsonSerializer.Serialize(displayName, WebJsonContext.Default.String);
        var languageJson = JsonSerializer.Serialize(ConfigManager.Config.ServerOption.Language ?? "EN",
            WebJsonContext.Default.String);
        var versionJson = JsonSerializer.Serialize(GameConstants.GAME_VERSION, WebJsonContext.Default.String);
        var knownPermissionsJson =
            JsonSerializer.Serialize(CommandPermissions.KnownPermissions().ToArray(), WebJsonContext.Default.StringArray);

        return $$"""
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <meta name="robots" content="noindex, nofollow">
  <meta name="color-scheme" content="dark">
  <title>{{encodedTitle}}</title>
  <style>
{{AdminPageStyles.Render()}}
  </style>
</head>
<body>
  <section id="login-view" class="login-view">
    <div class="login-card">
      <div class="login-brand">
        <span class="brand-mark" aria-hidden="true">{{encodedInitials}}</span>
        <div>
          <p class="eyebrow" data-i18n="login.eyebrow">Admin console</p>
          <h1>{{encodedDisplayName}}</h1>
        </div>
      </div>
      <form id="login-form" class="stack" autocomplete="on">
        <label class="field">
          <span data-i18n="login.key_label">Admin key</span>
          <div class="input-row">
            <input id="login-key" name="admin_key" type="password" autocomplete="current-password" required>
            <button id="login-toggle-key" class="btn ghost small" type="button">Show</button>
          </div>
        </label>
        <button class="btn primary block" type="submit" data-i18n="login.submit">Sign in</button>
        <p id="login-message" class="message" role="status" aria-live="polite"></p>
        <p class="muted small" data-i18n="login.hint"></p>
      </form>
      <div class="lang-switch" role="group">
        <button type="button" data-lang="zh-CN">中文</button>
        <button type="button" data-lang="en">English</button>
      </div>
    </div>
  </section>

  <div id="app" class="app hidden">
    <header class="topbar">
      <button id="nav-toggle" class="icon-btn" type="button" data-i18n-title="shell.menu" aria-label="Menu">
        <svg viewBox="0 0 20 20" width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M3 5h14M3 10h14M3 15h14"/></svg>
      </button>
      <div class="brand">
        <span class="brand-mark" aria-hidden="true">{{encodedInitials}}</span>
        <div>
          <strong>{{encodedDisplayName}}</strong>
          <span><span data-i18n="app.title">Admin console</span> · v{{GameConstants.GAME_VERSION}}</span>
        </div>
      </div>
      <div class="topbar-actions">
        <span id="clock" class="chip subtle mono"></span>
        <div class="lang-switch" role="group">
          <button type="button" data-lang="zh-CN">中文</button>
          <button type="button" data-lang="en">EN</button>
        </div>
        <button id="refresh-btn" class="btn ghost small" type="button" data-i18n="shell.refresh">Refresh</button>
        <button id="logout-btn" class="btn ghost small" type="button" data-i18n="shell.logout">Sign out</button>
      </div>
    </header>

    <div class="layout">
      <aside id="sidebar" class="sidebar">
        <nav id="nav" aria-label="Admin modules"></nav>
        <div id="session-info" class="sidebar-foot"></div>
      </aside>
      <main class="content">
        <div class="module-head">
          <div>
            <p id="module-kicker" class="eyebrow"></p>
            <h2 id="module-title"></h2>
          </div>
          <div id="module-actions" class="module-actions"></div>
        </div>
        <div id="module-root" class="module-root"></div>
      </main>
    </div>
  </div>

  <div id="toast-host" class="toast-host" aria-live="polite"></div>
  <div id="modal-host"></div>

  <script>
{{AdminPageI18n.Render()}}
    window.M7_ADMIN_CONFIG = {
      serverName: {{serverNameJson}},
      language: {{languageJson}},
      version: {{versionJson}},
      knownPermissions: {{knownPermissionsJson}}
    };
  </script>
  <script>
{{AdminPageScripts.Render()}}
  </script>
</body>
</html>
""";
    }

    private static string BuildInitials(string serverName)
    {
        var chars = serverName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part[0])
            .Where(char.IsLetterOrDigit)
            .Take(2)
            .ToArray();

        if (chars.Length == 0)
            chars = serverName.Where(char.IsLetterOrDigit).Take(2).ToArray();

        return chars.Length == 0 ? "M7" : new string(chars).ToUpperInvariant();
    }
}
