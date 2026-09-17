namespace March7thHoney.WebServer.Pages;

public static class LoginPlatformNotFoundPage
{
    public static string Render(string serverName, string registrationPageIcon)
    {
        var iconMarkup = LoginPlatformPage.RenderIconMarkup(registrationPageIcon, serverName);

        var body = $$"""
{{LoginPlatformPage.Brand(iconMarkup, "nf.title", "Page not found", serverName)}}
      <div class="status">
        <div class="icon bad" aria-hidden="true">404</div>
        <p data-i18n="nf.message">The page you requested does not exist. Use one of the links below.</p>
      </div>
      <div class="links">
        <a href="/login-platform/register.html" data-i18n="lp.register">Create account</a>
        <a href="/login-platform/forgot-password.html" data-i18n="lp.forgot">Forgot password</a>
      </div>
""";

        var strings = """
    LP.extend({
      "zh-CN": { "nf.title": "页面不存在", "nf.message": "你访问的页面不存在，请使用下面的链接。" },
      "en": { "nf.title": "Page not found", "nf.message": "The page you requested does not exist. Use one of the links below." }
    });
""";

        return LoginPlatformPage.Wrap("Page not found", serverName, body, "    LP.init();", strings);
    }
}
