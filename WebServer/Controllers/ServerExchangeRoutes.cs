using System.Security.Cryptography;
using System.Text;
using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace March7thHoney.WebServer.Controllers;

public static class ServerExchangeRoutes
{
    public static void MapServerExchangeRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/get_account_info", (HttpContext ctx, [FromQuery] string accountUid) =>
        {
            if (!ConfigManager.Config.ServerOption.ServerConfig.RunDispatch)
                return Results.StatusCode(404);

            if (!IsAuthorized(ctx))
                return Results.Text("Unauthorized", "text/plain", null, 401);

            if (string.IsNullOrEmpty(accountUid) || !int.TryParse(accountUid, out var uid))
                return Results.StatusCode(400);

            var account = DatabaseHelper.Instance?.GetInstance<AccountData>(uid);
            if (account == null)
                return Results.Text("Account not found", "text/plain", null, 404);

            return Results.Text(account.Uid.ToString(), "plain/text; charset=utf-8");
        });

        app.MapPost("/validate_account_token",
            (HttpContext ctx, [FromForm] string accountUid, [FromForm] string token, [FromForm] string? clientIp) =>
            {
                if (!ConfigManager.Config.ServerOption.ServerConfig.RunDispatch)
                    return Results.StatusCode(404);

                if (!IsAuthorized(ctx))
                    return Results.Text("Unauthorized", "text/plain", null, 401);

                if (string.IsNullOrWhiteSpace(accountUid) || string.IsNullOrWhiteSpace(token) ||
                    !int.TryParse(accountUid, out var uid))
                    return Results.Text("Invalid request", "text/plain", null, 400);

                var account = DatabaseHelper.Instance?.GetInstance<AccountData>(uid);
                if (account == null || !account.ValidateGameToken(token))
                    return Results.Text("Account token not found", "text/plain", null, 404);

                var identityKeys = AccountBanHelper.BuildIdentityKeys([],
                    clientIp ?? ctx.Connection.RemoteIpAddress?.ToString());
                if (clientIp != null && AccountBanHelper.NormalizeIpAddress(clientIp) == null)
                    return Results.Text("Invalid client IP", "text/plain", null, 400);
                if (AccountBanHelper.TryGetActiveBan(account, identityKeys, out var banStatus, "validate_account_token"))
                {
                    account.ClearLoginTokens();
                    return Results.Text(banStatus.FormatLoginMessage(), "text/plain", null, 403);
                }

                if (!account.CanLogin())
                {
                    account.ClearLoginTokens();
                    return Results.Text(ConfigManager.Config.ServerOption.Auth.EmailVerificationRequiredMessage,
                        "text/plain", null, 403);
                }

                account.AddKnownIdentityKeys(identityKeys);
                return Results.Text(account.Uid.ToString(), "plain/text; charset=utf-8");
            }).DisableAntiforgery();
    }

    private static bool IsAuthorized(HttpContext ctx)
    {
        var expectedSecret = ConfigManager.Config.ServerOption.ServerConfig.ServerExchangeSecret;
        if (string.IsNullOrWhiteSpace(expectedSecret))
            return false;

        if (!ctx.Request.Headers.TryGetValue("X-March7thHoney-Server-Secret", out var providedValues))
            return false;

        var providedSecret = providedValues.ToString();
        if (string.IsNullOrWhiteSpace(providedSecret))
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expectedSecret);
        var providedBytes = Encoding.UTF8.GetBytes(providedSecret);
        return expectedBytes.Length == providedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
