using March7thHoney.Database.Account;
using March7thHoney.Util;
using March7thHoney.WebServer.Objects;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace March7thHoney.WebServer.Handler;

public class ComboTokenGranterHandler
{
    private static readonly Logger Logger = new("ComboLoginV2");

    public JsonResult Handle(int app_id, int channel_id, string data, string device, string sign,
        IReadOnlyCollection<string> identityKeys)
    {
        var tokenData = JsonConvert.DeserializeObject<LoginTokenData>(data);
        var res = new ComboTokenResJson();
        if (tokenData == null)
        {
            Logger.Warn($"Failed to deserialize combo login payload. raw_len={data?.Length ?? 0}");
            res.retcode = -202;
            res.message = "Invalid login data";
            return new JsonResult(res);
        }

        if (!int.TryParse(tokenData.uid, out var uid))
        {
            Logger.Warn($"Invalid combo login uid. uid={tokenData.uid ?? "<null>"} token_len={tokenData.token?.Length ?? 0}");
            res.retcode = -202;
            res.message = "Invalid login data";
            return new JsonResult(res);
        }

        var autoCreateUser = ConfigManager.Config.ServerOption.AutoCreateUser;
        var account = AccountData.GetAccountByUid(uid);

        // The OS client validates username/password against the real HoYoverse shield
        // endpoint (not proxied), so only the combo login reaches us, carrying the real
        // account uid. When auto-create is enabled, bind a local account to that uid
        // (mirroring the shield-login AutoCreateUser path) and trust the combo login
        // instead of validating a token this server never issued.
        if (account == null && autoCreateUser)
        {
            try
            {
                AccountHelper.CreateAccount($"uid{uid}", $"{uid}@autocreate.local", null, uid);
                account = AccountData.GetAccountByUid(uid);
                Logger.Info($"Combo login auto-created account for uid={uid}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Combo login auto-create failed. uid={uid}: {ex.Message}");
            }
        }

        if (account == null)
        {
            Logger.Warn(
                $"Combo login cache validation failed. uid={uid} token_len={tokenData.token?.Length ?? 0} account_exists=False");
            res.retcode = -201;
            res.message = "Game account cache information error";
            return new JsonResult(res);
        }

        var isDispatchToken = account.ValidateDispatchToken(tokenData.token);
        var isComboToken = account.ValidateComboToken(tokenData.token);
        if (!autoCreateUser && !isDispatchToken && !isComboToken)
        {
            Logger.Warn(
                $"Combo login cache validation failed. uid={uid} token_len={tokenData.token?.Length ?? 0} account_exists=True");
            res.retcode = -201;
            res.message = "Game account cache information error";
            return new JsonResult(res);
        }

        if (account.GetBanStatus().IsActive)
            account.AddKnownIdentityKeys(identityKeys);

        if (!LoginSessionFactory.CanAuthenticate(account, identityKeys, out var failure))
        {
            Logger.Warn($"Combo login blocked. uid={uid} retcode={failure.Retcode}");
            res.retcode = failure.Retcode;
            res.message = failure.Message;
            return new JsonResult(res);
        }

        account.AddKnownIdentityKeys(identityKeys);
        Logger.Info(
            $"Combo login validated. uid={uid} token_type={(isDispatchToken ? "dispatch" : isComboToken ? "combo" : "trusted")} token_len={tokenData.token?.Length ?? 0}");
        res.message = "OK";
        res.data = new ComboTokenResJson.LoginData(account.Uid.ToString(), account.GenerateComboToken());
        return new JsonResult(res);
    }
}

public class LoginTokenData
{
    public string? uid { get; set; }
    public string? token { get; set; }
    public bool guest { get; set; }
}
