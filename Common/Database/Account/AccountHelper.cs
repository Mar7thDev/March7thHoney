using March7thHoney.Database.Mail;
using March7thHoney.Util;

namespace March7thHoney.Database.Account;

public static class AccountHelper
{
    public static void CreateAccount(string username, int uid)
    {
        if (AccountData.GetAccountByUserName(username) != null) throw new Exception("Account already exists");
        if (uid != 0 && AccountData.GetAccountByUid(uid) != null) throw new Exception("UID already exists");

        var newUid = uid;
        if (uid == 0)
        {
            newUid = 10001; // start from 10001
            while (AccountData.GetAccountByUid(newUid) != null) newUid++;
        }

        var per = ConfigManager.Config.ServerOption.DefaultPermissions;
        var perStr = string.Join(",", per);
        var account = new AccountData
        {
            Uid = newUid,
            Username = username,
            Permissions = perStr
        };
        DatabaseHelper.SaveInstance(account);
        MailHelper.SendWelcomeMail(newUid, username);
    }
}
