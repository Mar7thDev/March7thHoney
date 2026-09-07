using System.Net;

namespace March7thHoney.WebServer.Pages;

public static class RegistrationPage
{
    public static string Render(
        int minimumPasswordLength,
        string serverName,
        string registrationPageIcon,
        bool registrationEnabled)
    {
        var iconMarkup = LoginPlatformPage.RenderIconMarkup(registrationPageIcon, serverName);
        var disabledAttribute = registrationEnabled ? "" : " disabled";
        var registrationEnabledJson = registrationEnabled ? "true" : "false";

        var body = $$"""
{{LoginPlatformPage.Brand(iconMarkup, "lp.register", "Create account", serverName)}}
      <form id="register-form" autocomplete="on" novalidate>
        <label>
          <span data-i18n="lp.username">Username</span>
          <input id="username" name="username" autocomplete="username" required maxlength="64"{{disabledAttribute}}>
        </label>
        <label>
          <span data-i18n="lp.email">Email</span>
          <input id="email" name="email" type="email" autocomplete="email" required maxlength="254"{{disabledAttribute}}>
        </label>
        <label>
          <span data-i18n="lp.password">Password</span>
          <div class="input-row">
            <input id="password" name="password" type="password" autocomplete="new-password" required minlength="{{minimumPasswordLength}}"{{disabledAttribute}}>
            <button class="toggle" type="button" data-toggle="password" data-i18n="lp.show">Show</button>
          </div>
          <div id="strength" class="strength" data-score="0"><span></span><span></span><span></span><span></span></div>
          <p id="strength-label" class="hint"></p>
        </label>
        <label>
          <span data-i18n="lp.confirm_password">Confirm password</span>
          <input id="confirm-password" name="confirm_password" type="password" autocomplete="new-password" required minlength="{{minimumPasswordLength}}"{{disabledAttribute}}>
        </label>
        <p class="hint" id="password-hint"></p>
        <button id="submit-button" class="primary" type="submit" data-i18n="lp.register"{{disabledAttribute}}>Create account</button>
        <p id="message" class="message" role="status" aria-live="polite"></p>
      </form>

      <div id="result" class="result">
        <div class="uid-box">
          <span data-i18n="reg.your_uid">Your UID</span>
          <code id="uid"></code>
        </div>
        <span id="next-step"></span>
      </div>

      <div class="links">
        <a href="/login-platform/forgot-password.html" data-i18n="lp.forgot_link">Forgot password?</a>
      </div>
""";

        var strings = """
    LP.extend({
      "zh-CN": {
        "reg.title": "注册",
        "reg.your_uid": "你的 UID",
        "reg.disabled": "当前未开放注册。",
        "reg.username_required": "请输入用户名。",
        "reg.email_required": "请输入邮箱。",
        "reg.password_hint": "密码至少 {min} 个字符，建议混合大小写、数字和符号。",
        "reg.creating": "正在创建账号…",
        "reg.created": "账号已创建。",
        "reg.created_verify": "账号已创建，请先验证邮箱。",
        "reg.next_verify": "请查收邮件并完成验证后再登录游戏。",
        "reg.next_verify_failed": "需要验证邮箱，但验证邮件发送失败，请联系管理员。",
        "reg.next_login": "回到游戏登录界面，使用用户名或邮箱登录。"
      },
      "en": {
        "reg.title": "Register",
        "reg.your_uid": "Your UID",
        "reg.disabled": "Registration is currently closed.",
        "reg.username_required": "Username cannot be empty.",
        "reg.email_required": "Email cannot be empty.",
        "reg.password_hint": "At least {min} characters; mixing cases, digits and symbols is recommended.",
        "reg.creating": "Creating account…",
        "reg.created": "Account created.",
        "reg.created_verify": "Account created. Email verification required.",
        "reg.next_verify": "Check your email and verify the account before logging in.",
        "reg.next_verify_failed": "Email verification is required, but the verification email could not be sent. Contact an administrator.",
        "reg.next_login": "Return to the game login screen and sign in with your username or email."
      }
    });
""";

        var script = $$"""
    (() => {
      const minimumPasswordLength = {{minimumPasswordLength}};
      const registrationEnabled = {{registrationEnabledJson}};
      const form = document.getElementById("register-form");
      const button = document.getElementById("submit-button");
      const message = document.getElementById("message");
      const result = document.getElementById("result");
      const uid = document.getElementById("uid");
      const nextStep = document.getElementById("next-step");
      const passwordHint = document.getElementById("password-hint");
      let nextStepKey = "";

      LP.init();
      LP.bindStrength("password", "strength", "strength-label");
      const refreshText = () => {
        passwordHint.textContent = LP.t("reg.password_hint", { min: minimumPasswordLength });
        if (!registrationEnabled) LP.setMessage(message, LP.t("reg.disabled"), "error");
        if (nextStepKey) nextStep.textContent = LP.t(nextStepKey);
      };
      refreshText();
      document.addEventListener("lp:lang", refreshText);

      form.addEventListener("submit", async event => {
        event.preventDefault();
        result.classList.remove("show");

        if (!registrationEnabled) {
          LP.setMessage(message, LP.t("reg.disabled"), "error");
          return;
        }

        const username = document.getElementById("username").value.trim();
        const email = document.getElementById("email").value.trim();
        const password = document.getElementById("password").value;
        const confirmPassword = document.getElementById("confirm-password").value;

        if (!username) return LP.setMessage(message, LP.t("reg.username_required"), "error");
        if (!email) return LP.setMessage(message, LP.t("reg.email_required"), "error");
        if (password.length < minimumPasswordLength) return LP.setMessage(message, LP.t("lp.password_min", { min: minimumPasswordLength }), "error");
        if (password !== confirmPassword) return LP.setMessage(message, LP.t("lp.password_mismatch"), "error");

        button.disabled = true;
        LP.setMessage(message, LP.t("reg.creating"), "");
        try {
          const body = await LP.post("/account/register", {
            username,
            email,
            password,
            confirm_password: confirmPassword,
            device_id: LP.getOrCreateDeviceId(),
            device_fp: LP.getDeviceFingerprint()
          });
          const accountUid = body.data?.account?.uid || body.data?.user_info?.aid || "";
          const verificationRequired = Boolean(body.data?.email_verification_required);
          const emailSent = Boolean(body.data?.email_sent);
          uid.textContent = accountUid;
          nextStepKey = verificationRequired ? (emailSent ? "reg.next_verify" : "reg.next_verify_failed") : "reg.next_login";
          nextStep.textContent = LP.t(nextStepKey);
          result.classList.add("show");
          LP.setMessage(message, LP.t(verificationRequired ? "reg.created_verify" : "reg.created"), "success");
          form.reset();
          document.getElementById("strength").dataset.score = "0";
          document.getElementById("strength-label").textContent = "";
        } catch (error) {
          LP.setMessage(message, error instanceof Error ? error.message : LP.t("lp.request_failed"), "error");
        } finally {
          button.disabled = false;
        }
      });
    })();
""";

        return LoginPlatformPage.Wrap("Register", serverName, body, script, strings);
    }

    /// <summary>Kept for callers that resolved the icon through this page; the logic now lives in <see cref="LoginPlatformPage" />.</summary>
    public static string RenderIconMarkup(string configuredIcon, string serverName)
    {
        return LoginPlatformPage.RenderIconMarkup(configuredIcon, serverName);
    }
}
