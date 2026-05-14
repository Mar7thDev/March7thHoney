using March7thHoney.Database.Inventory;
using March7thHoney.Util;

namespace March7thHoney.Database.Mail;

public static class MailHelper
{
    public static void SendWelcomeMail(int uid, string username)
    {
        var config = ConfigManager.Config.ServerOption.WelcomeMail;
        if (!config.Enabled) return;

        var mailData = DatabaseHelper.Instance?.GetInstanceOrCreateNew<MailData>(uid);
        if (mailData == null) return;

        var now = DateTime.Now;
        var mail = new MailInfo
        {
            MailID = mailData.NextMailId++,
            SenderName = FormatText(config.Sender, uid, username),
            Title = FormatText(config.Title, uid, username),
            Content = FormatText(config.Content, uid, username),
            TemplateID = config.TemplateId,
            SendTime = now.ToUnixSec(),
            ExpireTime = now.AddDays(Math.Max(config.ExpireDays, 1)).ToUnixSec(),
            IsStar = config.IsStar,
            Attachment = new MailAttachmentInfo
            {
                Items = config.Items
                    .Where(item => item.ItemId > 0 && item.Count > 0)
                    .Select(item => new ItemData
                    {
                        ItemId = item.ItemId,
                        Count = item.Count,
                        Rank = item.Rank,
                        Level = item.Level,
                        Promotion = item.Promotion
                    })
                    .ToList()
            }
        };

        mailData.MailList.Add(mail);
        DatabaseHelper.SaveDatabaseType(mailData);
    }

    private static string FormatText(string value, int uid, string username)
    {
        return value
            .Replace("{uid}", uid.ToString())
            .Replace("{username}", username)
            .Replace("{account}", username);
    }
}
