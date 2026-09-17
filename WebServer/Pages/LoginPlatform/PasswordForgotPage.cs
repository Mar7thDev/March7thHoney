namespace March7thHoney.WebServer.Pages;

public static class PasswordForgotPage
{
    public static string Render(string serverName, string registrationPageIcon)
    {
        var iconMarkup = LoginPlatformPage.RenderIconMarkup(registrationPageIcon, serverName);

        var body = $$"""
{{LoginPlatformPage.Brand(iconMarkup, "lp.forgot", "Forgot password", serverName)}}
      <p class="hint" data-i18n="forgot.intro" style="margin-bottom: 16px;">Enter the username and the verified email of the account; a reset link is sent to that address.</p>
      <form id="forgot-form" autocomplete="on" novalidate>
        <label>
          <span data-i18n="lp.username">Username</span>
          <input id="username" name="username" autocomplete="username" required maxlength="64">
        </label>
        <label>
          <span data-i18n="lp.email">Email</span>
          <input id="email" name="email" type="email" autocomplete="email" required maxlength="254">
        </label>
        <button id="submit-button" class="primary" type="submit" data-i18n="forgot.submit">Send reset email</button>
        <p id="message" class="message" role="status" aria-live="polite"></p>
      </form>
      <div class="links">
        <a href="/login-platform/register.html" data-i18n="lp.back_register">Create an account</a>
      </div>
""";

        var strings = """
    LP.extend({
      "zh-CN": {
        "forgot.intro": "输入账号的用户名和已验证的邮箱，我们会把重置链接发送到该邮箱。",
        "forgot.submit": "发送重置邮件",
        "forgot.required": "请填写用户名和邮箱。",
        "forgot.sending": "正在发送重置邮件…",
        "forgot.sent": "如果账号存在，重置邮件已发送，请查收。"
      },
      "en": {
        "forgot.intro": "Enter the username and the verified email of the account; a reset link is sent to that address.",
        "forgot.submit": "Send reset email",
        "forgot.required": "Username and email are required.",
        "forgot.sending": "Sending reset email…",
        "forgot.sent": "If the account exists, a reset email has been sent."
      }
    });
""";

        var script = """
    (() => {
      const form = document.getElementById("forgot-form");
      const button = document.getElementById("submit-button");
      const message = document.getElementById("message");
      LP.init();

      form.addEventListener("submit", async event => {
        event.preventDefault();
        const username = document.getElementById("username").value.trim();
        const email = document.getElementById("email").value.trim();
        if (!username || !email) {
          LP.setMessage(message, LP.t("forgot.required"), "error");
          return;
        }
        button.disabled = true;
        LP.setMessage(message, LP.t("forgot.sending"), "");
        try {
          await LP.post("/account/password/forgot", { username, email });
          LP.setMessage(message, LP.t("forgot.sent"), "success");
          form.reset();
        } catch (error) {
          LP.setMessage(message, error instanceof Error ? error.message : LP.t("lp.request_failed"), "error");
        } finally {
          button.disabled = false;
        }
      });
    })();
""";

        return LoginPlatformPage.Wrap("Forgot password", serverName, body, script, strings);
    }
}
