namespace March7thHoney.WebServer.Pages;

public static class PasswordResetPage
{
    public static string Render(int minimumPasswordLength, string serverName, string registrationPageIcon)
    {
        var iconMarkup = LoginPlatformPage.RenderIconMarkup(registrationPageIcon, serverName);

        var body = $$"""
{{LoginPlatformPage.Brand(iconMarkup, "lp.reset", "Reset password", serverName)}}
      <div id="invalid" class="status hidden">
        <div class="icon bad" aria-hidden="true">!</div>
        <p data-i18n="reset.invalid_link">This reset link is incomplete. Open the link from the email again or request a new one.</p>
      </div>
      <form id="reset-form" autocomplete="on" novalidate>
        <label>
          <span data-i18n="lp.new_password">New password</span>
          <div class="input-row">
            <input id="password" name="password" type="password" autocomplete="new-password" required minlength="{{minimumPasswordLength}}">
            <button class="toggle" type="button" data-toggle="password" data-i18n="lp.show">Show</button>
          </div>
          <div id="strength" class="strength" data-score="0"><span></span><span></span><span></span><span></span></div>
          <p id="strength-label" class="hint"></p>
        </label>
        <label>
          <span data-i18n="lp.confirm_password">Confirm password</span>
          <input id="confirm-password" name="confirm_password" type="password" autocomplete="new-password" required minlength="{{minimumPasswordLength}}">
        </label>
        <button id="submit-button" class="primary" type="submit" data-i18n="reset.submit">Set password</button>
        <p id="message" class="message" role="status" aria-live="polite"></p>
      </form>
      <div id="done" class="status hidden">
        <div class="icon good" aria-hidden="true">&#10003;</div>
        <p data-i18n="reset.done">Your password has been updated. Return to the game and sign in with the new password.</p>
      </div>
      <div class="links">
        <a href="/login-platform/forgot-password.html" data-i18n="reset.request_new">Request a new link</a>
      </div>
""";

        var strings = """
    LP.extend({
      "zh-CN": {
        "reset.submit": "设置新密码",
        "reset.invalid_link": "重置链接不完整，请重新打开邮件中的链接，或重新申请。",
        "reset.updating": "正在更新密码…",
        "reset.done": "密码已更新，请回到游戏使用新密码登录。",
        "reset.request_new": "重新申请重置链接"
      },
      "en": {
        "reset.submit": "Set password",
        "reset.invalid_link": "This reset link is incomplete. Open the link from the email again or request a new one.",
        "reset.updating": "Updating password…",
        "reset.done": "Your password has been updated. Return to the game and sign in with the new password.",
        "reset.request_new": "Request a new link"
      }
    });
""";

        var script = $$"""
    (() => {
      const minimumPasswordLength = {{minimumPasswordLength}};
      const params = new URLSearchParams(window.location.search);
      const uid = params.get("uid") || "";
      const token = params.get("token") || "";
      const form = document.getElementById("reset-form");
      const button = document.getElementById("submit-button");
      const message = document.getElementById("message");
      LP.init();
      LP.bindStrength("password", "strength", "strength-label");

      if (!uid || !token) {
        form.classList.add("hidden");
        document.getElementById("invalid").classList.remove("hidden");
      }

      form.addEventListener("submit", async event => {
        event.preventDefault();
        const password = document.getElementById("password").value;
        const confirmPassword = document.getElementById("confirm-password").value;
        if (password.length < minimumPasswordLength) return LP.setMessage(message, LP.t("lp.password_min", { min: minimumPasswordLength }), "error");
        if (password !== confirmPassword) return LP.setMessage(message, LP.t("lp.password_mismatch"), "error");

        button.disabled = true;
        LP.setMessage(message, LP.t("reset.updating"), "");
        try {
          await LP.post("/account/password/reset", { uid, token, password, confirm_password: confirmPassword });
          form.classList.add("hidden");
          document.getElementById("done").classList.remove("hidden");
        } catch (error) {
          LP.setMessage(message, error instanceof Error ? error.message : LP.t("lp.request_failed"), "error");
        } finally {
          button.disabled = false;
        }
      });
    })();
""";

        return LoginPlatformPage.Wrap("Reset password", serverName, body, script, strings);
    }
}
