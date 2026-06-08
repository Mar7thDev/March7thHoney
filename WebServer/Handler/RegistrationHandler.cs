using March7thHoney.Database.Account;
using March7thHoney.Util;
using March7thHoney.WebServer.Objects;
using Microsoft.AspNetCore.Http;

namespace March7thHoney.WebServer.Handler;

public class RegistrationHandler
{
    public async Task<IResult> Handle(RegisterReqJson req, HttpRequest request)
    {
        if (!ConfigManager.Config.ServerOption.Auth.EnableRegistration)
            return Error("Registration is disabled");

        var identityKeys = LoginIdentityContextFactory.Build(request, req.device, req.device_id, req.device_fp);
        var antiAbuse = ConfigManager.Config.ServerOption.Auth.AntiAbuse;
        var username = ResolveUsername(req);
        var email = ResolveEmail(req);
        var password = req.password ?? string.Empty;
        var confirmation = req.confirm_password ?? req.confirmPassword ?? req.confirm;

        if (AuthRateLimiter.TryReject(request.HttpContext, AuthRateLimitAction.Registration, identityKeys,
                string.IsNullOrWhiteSpace(username) ? email : username, out var limited))
        {
            return limited;
        }

        if (AccountBanHelper.TryFindActiveBanByIdentityKeys(identityKeys, null, out var banStatus))
            return Error(banStatus.FormatLoginMessage(), -203);

        if (antiAbuse.RequireRegistrationIdentity &&
            !identityKeys.Any(key => key.StartsWith("device:", StringComparison.Ordinal)))
        {
            return Error("Registration is not available for this browser or network");
        }

        if (string.IsNullOrWhiteSpace(username))
            return Error("Username cannot be empty");

        if (string.IsNullOrWhiteSpace(email))
            return Error("Email cannot be empty");

        if (!string.IsNullOrEmpty(confirmation) && !string.Equals(password, confirmation, StringComparison.Ordinal))
            return Error("Passwords do not match");

        if (antiAbuse.LimitRegistrationPerIdentity &&
            AccountBanHelper.TryFindAccountByIdentityKeys(identityKeys, antiAbuse.RegistrationIdentityMode, out _))
        {
            return Error("Registration is not available for this device or network");
        }

        if (AccountData.GetAccountByUserName(username) != null)
            return Error("Username already exists");

        if (AccountData.GetAccountByEmail(email) != null)
            return Error("Email already exists");

        try
        {
            AccountHelper.CreateAccount(username, email, password, 0);
        }
        catch (Exception ex)
        {
            return Error(ex.Message);
        }

        var emailSent = false;
        var account = AccountData.GetAccountByUserName(username);
        account?.AddKnownIdentityKeys(identityKeys);
        if (account != null && ConfigManager.Config.ServerOption.Auth.SendVerificationEmailOnRegister)
        {
            var verificationToken = account.GenerateEmailVerificationToken();
            emailSent = await AccountEmailSender.SendVerificationEmailAsync(account, verificationToken, request);
        }

        if (account != null && !account.CanLogin())
            return RegistrationSuccess(account, emailSent, requiresEmailVerification: true);

        LoginSessionFactory.LoginFailure failure;
        if (!LoginSessionFactory.TryCreateSession(username, password, identityKeys, out var session, out failure))
            return Error(failure.Message, failure.Retcode);

        return Results.Json(new
        {
            retcode = 0,
            message = "OK",
            data = new
            {
                account = new
                {
                    uid = session.Uid,
                    name = session.Username,
                    email = session.Email,
                    token = session.DispatchToken,
                    is_email_verify = session.IsEmailVerified ? "1" : "0"
                },
                user_info = new
                {
                    aid = session.Uid,
                    mid = session.Uid,
                    account_name = session.Username,
                    email = session.Email,
                    is_email_verify = session.IsEmailVerified ? "1" : "0"
                },
                token = new { token_type = 1, token = session.DispatchToken },
                email_sent = emailSent
            }
        });
    }

    private static IResult RegistrationSuccess(AccountData account, bool emailSent, bool requiresEmailVerification)
    {
        var uid = account.Uid.ToString();
        var username = account.Username ?? uid;
        var email = account.Email ?? string.Empty;
        return Results.Json(new
        {
            retcode = 0,
            message = requiresEmailVerification ? LoginSessionFactory.EmailVerificationRequiredMessage : "OK",
            data = new
            {
                account = new
                {
                    uid,
                    name = username,
                    email,
                    token = "",
                    is_email_verify = account.IsEmailVerified ? "1" : "0"
                },
                user_info = new
                {
                    aid = uid,
                    mid = uid,
                    account_name = username,
                    email,
                    is_email_verify = account.IsEmailVerified ? "1" : "0"
                },
                token = new { token_type = 1, token = "" },
                email_sent = emailSent,
                email_verification_required = requiresEmailVerification
            }
        });
    }

    private static IResult Error(string message, int retcode = -201)
    {
        return Results.Json(new { retcode, message, data = (object?)null });
    }

    private static string ResolveUsername(RegisterReqJson req)
    {
        var account = (req.account ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(req.username))
            return req.username.Trim();

        return LooksLikeEmail(account) ? string.Empty : account;
    }

    private static string ResolveEmail(RegisterReqJson req)
    {
        var account = (req.account ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(req.email))
            return req.email.Trim();

        return LooksLikeEmail(account) ? account : string.Empty;
    }

    private static bool LooksLikeEmail(string value)
    {
        return value.Contains('@', StringComparison.Ordinal);
    }
}
