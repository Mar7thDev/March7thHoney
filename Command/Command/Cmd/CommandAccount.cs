using March7thHoney.Database.Account;
using March7thHoney.Internationalization;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("account", "Game.Command.Account.Desc", "Game.Command.Account.Usage", permission: "egglink.manage")]
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
}
