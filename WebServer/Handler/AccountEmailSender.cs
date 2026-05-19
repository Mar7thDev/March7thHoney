using MailKit.Net.Smtp;
using MailKit.Security;
using March7thHoney.Database.Account;
using March7thHoney.Util;
using Microsoft.AspNetCore.Http;
using MimeKit;

namespace March7thHoney.WebServer.Handler;

internal static class AccountEmailSender
{
    private static readonly Logger Logger = new("AccountEmail");

    public static async Task<bool> SendVerificationEmailAsync(AccountData account, string token, HttpRequest request)
    {
        var auth = ConfigManager.Config.ServerOption.Auth;
        var link = BuildLink(request, "/account/email/verify",
            ("uid", account.Uid.ToString()),
            ("token", token));

        var minutes = Math.Max(auth.EmailVerificationTokenExpireMinutes, 1);
        var subject = FormatTemplate(auth.Email.VerificationSubject, account, link, minutes);
        var body = FormatTemplate(auth.Email.VerificationBody, account, link, minutes);
        return await SendAsync(account.Email, subject, body);
    }

    public static async Task<bool> SendPasswordResetEmailAsync(AccountData account, string token, HttpRequest request)
    {
        var auth = ConfigManager.Config.ServerOption.Auth;
        var link = BuildLink(request, "/login-platform/reset-password.html",
            ("uid", account.Uid.ToString()),
            ("token", token));

        var minutes = Math.Max(auth.PasswordResetTokenExpireMinutes, 1);
        var subject = FormatTemplate(auth.Email.PasswordResetSubject, account, link, minutes);
        var body = FormatTemplate(auth.Email.PasswordResetBody, account, link, minutes);
        return await SendAsync(account.Email, subject, body);
    }

    private static async Task<bool> SendAsync(string? to, string subject, string body)
    {
        var email = ConfigManager.Config.ServerOption.Auth.Email;
        var smtp = email.Smtp;

        if (!email.Enabled)
        {
            Logger.Warn("Email sending is disabled. Configure ServerOption.Auth.Email to send verification and reset emails.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(to) ||
            string.IsNullOrWhiteSpace(smtp.Host) ||
            string.IsNullOrWhiteSpace(smtp.FromAddress))
        {
            Logger.Warn("Email sending is not configured completely. Host, FromAddress, and recipient are required.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(smtp.Username) && string.IsNullOrWhiteSpace(smtp.Password))
        {
            Logger.Warn("Email sending is not configured completely. SMTP password is required when SMTP username is set.");
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            var secureSocketOptions = ResolveSecureSocketOptions(smtp.Port, smtp.EnableSsl);
            await client.ConnectAsync(smtp.Host, smtp.Port, secureSocketOptions);

            if (!string.IsNullOrWhiteSpace(smtp.Username))
                await client.AuthenticateAsync(smtp.Username, smtp.Password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to send email to {to}: {ex.Message}");
            return false;
        }
    }

    private static SecureSocketOptions ResolveSecureSocketOptions(int port, bool enableSsl)
    {
        if (!enableSsl)
            return SecureSocketOptions.None;

        return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }

    private static string BuildLink(HttpRequest request, string path, params (string Key, string Value)[] query)
    {
        var configuredBase = ConfigManager.Config.ServerOption.Auth.Email.PublicBaseUrl;
        var baseUrl = !string.IsNullOrWhiteSpace(configuredBase)
            ? configuredBase.TrimEnd('/')
            : $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');

        var queryString = string.Join("&",
            query.Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
        return $"{baseUrl}{path}?{queryString}";
    }

    private static string FormatTemplate(string template, AccountData account, string link, int minutes)
    {
        return template
            .Replace("{username}", account.Username ?? account.Uid.ToString(), StringComparison.Ordinal)
            .Replace("{email}", account.Email ?? string.Empty, StringComparison.Ordinal)
            .Replace("{uid}", account.Uid.ToString(), StringComparison.Ordinal)
            .Replace("{serverName}", ConfigManager.Config.GameServer.GameServerName, StringComparison.Ordinal)
            .Replace("{link}", link, StringComparison.Ordinal)
            .Replace("{minutes}", minutes.ToString(), StringComparison.Ordinal);
    }
}
