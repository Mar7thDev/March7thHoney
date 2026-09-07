using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.Account;
using March7thHoney.Database.Inventory;
using March7thHoney.Database.Mail;
using March7thHoney.Enums.Item;
using March7thHoney.GameServer.Server;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.Mail;

/// <summary>A mail ready to be delivered; attachments are cloned per recipient.</summary>
public sealed record MailDraft(
    string Sender,
    string Title,
    string Content,
    int TemplateId,
    int ExpireDays,
    List<ItemData> Attachments,
    bool IsStar = false);

/// <summary>
///     Delivers mail to a player whether they are online (through the live <see cref="MailManager" />, so the
///     client gets a NewMail notify) or offline (appended straight to the persisted <see cref="MailData" />).
///     Shared by the /mail command and the admin console.
/// </summary>
public static class MailDeliveryHelper
{
    public static List<AccountData> GetAllAccounts()
    {
        var accounts = DatabaseHelper.GetAllInstanceFromMap<AccountData>();
        if (accounts == null || accounts.Count == 0)
            accounts = DatabaseHelper.GetAllInstance<AccountData>();

        return accounts?
            .Where(account => account.Uid > 0)
            .GroupBy(account => account.Uid)
            .Select(group => group.First())
            .ToList() ?? [];
    }

    public static MailDraft BuildWelcomeDraft(int uid, string username)
    {
        var config = ConfigManager.Config.ServerOption.WelcomeMail;
        var attachments = config.Items
            .Where(item => item.ItemId > 0 && item.Count > 0 && GameData.ItemConfigData.ContainsKey(item.ItemId))
            .Select(item => new ItemData
            {
                ItemId = item.ItemId,
                Count = item.Count,
                Rank = GetConfiguredAttachmentRank(item.ItemId, item.Rank),
                Level = item.Level,
                Promotion = item.Promotion,
                TalentLevel = item.TalentLevel
            })
            .ToList();

        return new MailDraft(
            FormatText(config.Sender, uid, username),
            FormatText(config.Title, uid, username),
            FormatText(config.Content, uid, username),
            config.TemplateId,
            config.ExpireDays,
            attachments,
            config.IsStar);
    }

    /// <summary>Deliver to one uid. Returns false only when the database is unavailable.</summary>
    public static async ValueTask<bool> SendToUidAsync(int uid, MailDraft draft)
    {
        var connection = Listener.GetActiveConnection(uid);
        if (connection?.Player?.MailManager != null)
        {
            await SendToConnectionAsync(connection, draft);
            return true;
        }

        var mailData = DatabaseHelper.Instance?.GetInstanceOrCreateNew<MailData>(uid);
        if (mailData == null) return false;

        mailData.MailList.Add(CreateMailInfo(mailData.NextMailId++, draft));
        DatabaseHelper.SaveDatabaseType(mailData);
        return true;
    }

    public static async ValueTask SendToConnectionAsync(Connection connection, MailDraft draft)
    {
        var manager = connection.Player!.MailManager!;
        if (draft.Attachments.Count > 0)
        {
            await manager.SendMail(draft.Sender, draft.Title, draft.Content, draft.TemplateId,
                CloneAttachments(draft.Attachments), draft.ExpireDays, draft.IsStar);
            return;
        }

        await manager.SendMail(draft.Sender, draft.Title, draft.Content, draft.TemplateId, draft.ExpireDays,
            draft.IsStar);
    }

    public static async ValueTask<int> SendToAllAccountsAsync(MailDraft draft)
    {
        var sentCount = 0;
        foreach (var account in GetAllAccounts())
        {
            if (await SendToUidAsync(account.Uid, draft)) sentCount++;
        }

        return sentCount;
    }

    public static async ValueTask<int> SendWelcomeToAllAsync()
    {
        var sentCount = 0;
        foreach (var account in GetAllAccounts())
        {
            if (await SendToUidAsync(account.Uid, BuildWelcomeDraft(account.Uid, GetRecipientName(account))))
                sentCount++;
        }

        return sentCount;
    }

    public static MailInfo CreateMailInfo(int mailId, MailDraft draft)
    {
        var now = DateTime.Now;
        return new MailInfo
        {
            MailID = mailId,
            SenderName = draft.Sender,
            Title = draft.Title,
            Content = draft.Content,
            TemplateID = draft.TemplateId,
            IsStar = draft.IsStar,
            SendTime = now.ToUnixSec(),
            ExpireTime = now.AddDays(Math.Max(draft.ExpireDays, 1)).ToUnixSec(),
            Attachment = new MailAttachmentInfo
            {
                Items = CloneAttachments(draft.Attachments)
            }
        };
    }

    public static List<ItemData> CloneAttachments(IEnumerable<ItemData> attachments)
    {
        return [.. attachments.Select(attachment => attachment.Clone())];
    }

    public static string GetRecipientName(AccountData account)
    {
        return account.Username ?? account.Uid.ToString();
    }

    public static int GetConfiguredAttachmentRank(int itemId, int rank)
    {
        return GameData.ItemConfigData.TryGetValue(itemId, out var itemConfig) &&
               itemConfig.ItemMainType == ItemMainTypeEnum.Equipment &&
               rank <= 0
            ? 1
            : rank;
    }

    public static string FormatText(string value, int uid, string username)
    {
        return value
            .Replace("{uid}", uid.ToString())
            .Replace("{username}", username)
            .Replace("{account}", username);
    }
}
