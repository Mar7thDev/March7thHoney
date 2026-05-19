using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.Database.Mail;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server;
using March7thHoney.GameServer.Server.Packet.Send.Player;
using March7thHoney.Internationalization;
using March7thHoney.Util;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("account", "Game.Command.Account.Desc", "Game.Command.Account.Usage", permission: CommandPermissions.Account)]
public class CommandAccount : ICommand
{
    [CommandMethod("create")]
    public async ValueTask CreateAccount(CommandArg arg)
    {
        if (arg.Args.Count < 2)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var account = arg.Args[1];
        string? password = null;
        var uid = 0;

        if (arg.Args.Count > 2)
            if (int.TryParse(arg.Args[2], out uid))
            {
            }
            else
            {
                password = arg.Args[2];
            }

        if (arg.Args.Count > 3)
            if (!int.TryParse(arg.Args[3], out uid))
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Account.InvalidUid"));
                return;
            }

        if (AccountData.GetAccountByUserName(account) != null)
        {
            await arg.SendMsg(string.Format(I18NManager.Translate("Game.Command.Account.DuplicateAccount"), account));
            return;
        }

        if (uid != 0 && AccountData.GetAccountByUid(uid) != null)
        {
            await arg.SendMsg(string.Format(I18NManager.Translate("Game.Command.Account.DuplicateUID"), uid));
            return;
        }

        try
        {
            AccountHelper.CreateAccount(account, password, uid);
            await arg.SendMsg(string.IsNullOrWhiteSpace(password)
                ? $"{I18NManager.Translate("Game.Command.Account.CreateSuccess", account)} Password is not set yet. Use /account setpassword {account} <password> before login."
                : I18NManager.Translate("Game.Command.Account.CreateSuccess", account));
        }
        catch (Exception ex)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Account.CreateError", ex.Message));
        }
    }

    [CommandMethod("setpassword")]
    public async ValueTask SetPassword(CommandArg arg)
    {
        if (arg.Args.Count < 3)
        {
            await arg.SendMsg("Usage: /account setpassword [Username] [Password]");
            return;
        }

        var accountName = arg.Args[1];
        var password = arg.Args[2];
        var account = AccountData.GetAccountByUserName(accountName);
        var canonicalAccount = account == null ? null : AccountData.GetAccountByUid(account.Uid) ?? account;
        if (canonicalAccount == null)
        {
            await arg.SendMsg($"Account {accountName} does not exist.");
            return;
        }

        try
        {
            AccountHelper.SetPassword(canonicalAccount, password);
            await arg.SendMsg($"Password updated for account {accountName}.");
        }
        catch (Exception ex)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Account.CreateError", ex.Message));
        }
    }

    [CommandMethod("setrole")]
    public async ValueTask SetRole(CommandArg arg)
    {
        await SetRoleInternal(arg);
    }

    [CommandMethod("role")]
    public async ValueTask Role(CommandArg arg)
    {
        await SetRoleInternal(arg);
    }

    [CommandMethod("roles")]
    public async ValueTask Roles(CommandArg arg)
    {
        await arg.SendMsg("Available roles: " + string.Join(", ", GetConfiguredRoles().Keys));
    }

    [CommandMethod("reset")]
    public async ValueTask Reset(CommandArg arg)
    {
        var accountKey = arg.Args.Count > 1 ? arg.Args[1] : arg.Sender.GetSender().ToString();
        if (string.IsNullOrWhiteSpace(accountKey) || accountKey == "0")
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Account.ResetUsage"));
            return;
        }

        var account = ResolveAccount(accountKey);
        if (account == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Account.AccountNotFound", accountKey));
            return;
        }

        if (DatabaseHelper.Instance == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Account.CreateError", "Database is not initialized"));
            return;
        }

        var displayName = account.Username ?? account.Uid.ToString();
        var isSelfReset = account.Uid == arg.Sender.GetSender();
        if (isSelfReset) await arg.SendMsg(I18NManager.Translate("Game.Command.Account.ResetStarted", displayName));

        var activeConnection = Listener.GetActiveConnection(account.Uid);
        if (activeConnection != null)
        {
            await activeConnection.SendPacket(new PacketPlayerSqueezedScNotify());
            activeConnection.Stop();
        }

        DatabaseHelper.Instance.DeleteUidGameplayData(account.Uid);

        var freshPlayer = new PlayerInstance(account.Uid);
        freshPlayer.Data.Level = ConfigManager.Config.ServerOption.StartTrailblazerLevel;
        freshPlayer.Data.Exp = 0;
        freshPlayer.OnLevelChange();

        MailHelper.SendWelcomeMail(account.Uid, displayName);

        DatabaseHelper.ToSaveUidList.SafeAdd(account.Uid);
        DatabaseHelper.Instance.SaveDatabase();

        if (!isSelfReset)
            await arg.SendMsg(I18NManager.Translate("Game.Command.Account.ResetSuccess", displayName));
    }

    private static async ValueTask SetRoleInternal(CommandArg arg)
    {
        if (arg.Args.Count < 3)
        {
            await arg.SendMsg("Usage: /account role [Username|UID] [Role]");
            return;
        }

        var accountKey = arg.Args[1];
        var account = ResolveAccount(accountKey);
        if (account == null)
        {
            await arg.SendMsg($"Account {accountKey} does not exist.");
            return;
        }

        if (!TryNormalizeRole(arg.Args[2], out var role))
        {
            await arg.SendMsg($"Unknown role {arg.Args[2]}. Available roles: {string.Join(", ", GetConfiguredRoles().Keys)}");
            return;
        }

        var permissions = account.SetRole(role);
        await arg.SendMsg(
            $"Role for {account.Username ?? account.Uid.ToString()} updated to {role}. Permissions synced: {permissions}.");
    }

    private static AccountData? ResolveAccount(string accountKey)
    {
        if (int.TryParse(accountKey, out var uid))
            return AccountData.GetAccountByUid(uid);

        var account = AccountData.GetAccountByUserName(accountKey);
        return account == null ? null : AccountData.GetAccountByUid(account.Uid) ?? account;
    }

    private static bool TryNormalizeRole(string role, out string normalizedRole)
    {
        foreach (var configuredRole in GetConfiguredRoles().Keys)
        {
            if (!string.Equals(configuredRole, role, StringComparison.OrdinalIgnoreCase)) continue;

            normalizedRole = configuredRole;
            return true;
        }

        normalizedRole = "";
        return false;
    }

    private static IReadOnlyDictionary<string, HashSet<string>> GetConfiguredRoles()
    {
        return ConfigManager.Config.ServerOption.PermissionRoles ?? CommandRoles.DefaultRoles();
    }
}
