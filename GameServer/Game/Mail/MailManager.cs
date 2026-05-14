using March7thHoney.Data;
using March7thHoney.Database;
using March7thHoney.Database.Inventory;
using March7thHoney.Database.Mail;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Mail;
using March7thHoney.GameServer.Server.Packet.Send.PlayerSync;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.Mail;

public class MailManager(PlayerInstance player) : BasePlayerManager(player)
{
    public MailData MailData { get; } = DatabaseHelper.Instance!.GetInstanceOrCreateNew<MailData>(player.Uid);

    public List<MailInfo> GetMailList()
    {
        return MailData.MailList;
    }

    public MailInfo? GetMail(int mailId)
    {
        return MailData.MailList.Find(x => x.MailID == mailId);
    }

    public List<uint> GetMailIdsWithAttachments()
    {
        return MailData.MailList
            .Where(mail => mail.Attachment.Items.Count > 0)
            .Select(mail => (uint)mail.MailID)
            .ToList();
    }

    public void DeleteMail(int mailId)
    {
        var index = MailData.MailList.FindIndex(x => x.MailID == mailId);
        if (index < 0) return;
        MailData.MailList.RemoveAt(index);
    }

    public async ValueTask SendMail(string sender, string title, string content, int templateId, int expiredDay = 30)
    {
        var mail = new MailInfo
        {
            MailID = MailData.NextMailId++,
            SenderName = sender,
            Content = content,
            Title = title,
            TemplateID = templateId,
            SendTime = DateTime.Now.ToUnixSec(),
            ExpireTime = DateTime.Now.AddDays(expiredDay).ToUnixSec()
        };

        MailData.MailList.Add(mail);

        await Player.SendPacket(new PacketNewMailScNotify(mail.MailID));
    }

    public async ValueTask SendMail(string sender, string title, string content, int templateId,
        List<ItemData> attachments, int expiredDay = 30)
    {
        var mail = new MailInfo
        {
            MailID = MailData.NextMailId++,
            SenderName = sender,
            Content = content,
            Title = title,
            TemplateID = templateId,
            SendTime = DateTime.Now.ToUnixSec(),
            ExpireTime = DateTime.Now.AddDays(expiredDay).ToUnixSec(),
            Attachment = new MailAttachmentInfo
            {
                Items = attachments
            }
        };

        MailData.MailList.Add(mail);

        await Player.SendPacket(new PacketNewMailScNotify(mail.MailID));
    }

    public List<ClientMail> ToMailProto()
    {
        var list = new List<ClientMail>();

        foreach (var mail in MailData.MailList) list.Add(mail.ToProto());

        return list;
    }

    public List<ClientMail> ToNoticeMailProto()
    {
        var list = new List<ClientMail>();

        foreach (var mail in MailData.NoticeMailList) list.Add(mail.ToProto());

        return list;
    }

    public async ValueTask<TakeMailAttachmentResult> TakeAttachments(IEnumerable<uint> mailIds)
    {
        var result = new TakeMailAttachmentResult();

        foreach (var mailId in mailIds.Distinct())
        {
            var mail = GetMail((int)mailId);
            if (mail == null)
            {
                result.FailedMails.Add(new TakeMailAttachmentFailInfo(mailId, Retcode.RetMailMailIdInvalid));
                continue;
            }

            if (mail.Attachment.Items.Count == 0)
            {
                result.FailedMails.Add(new TakeMailAttachmentFailInfo(mailId, Retcode.RetMailNoMailTakeAttachment));
                continue;
            }

            if (mail.Attachment.Items.Any(item =>
                    item.ItemId <= 0 || item.Count <= 0 || !GameData.ItemConfigData.ContainsKey(item.ItemId)))
            {
                result.FailedMails.Add(new TakeMailAttachmentFailInfo(mailId, Retcode.RetMailAttachementInvalid));
                continue;
            }

            var grantedItems = new List<ItemData>();
            foreach (var item in mail.Attachment.Items)
            {
                var grantedItem = await Player.InventoryManager!.AddItem(item.ItemId, item.Count, false, item.Rank,
                    item.Level, item.Promotion, sync: false, returnRaw: true);
                if (grantedItem != null) grantedItems.Add(grantedItem);
            }

            if (grantedItems.Count != mail.Attachment.Items.Count)
            {
                result.FailedMails.Add(new TakeMailAttachmentFailInfo(mailId, Retcode.RetMailAttachementInvalid));
                continue;
            }

            result.Attachment.Items.AddRange(grantedItems);
            result.SuccessMailIds.Add(mailId);
            mail.Attachment.Items.Clear();
            mail.IsRead = true;
        }

        if (result.SuccessMailIds.Count > 0)
        {
            DatabaseHelper.ToSaveUidList.SafeAdd(Player.Uid);
            await Player.SendPacket(new PacketPlayerSyncScNotify(result.Attachment.Items));
        }

        return result;
    }
}

public class TakeMailAttachmentResult
{
    public MailAttachmentInfo Attachment { get; } = new();
    public List<uint> SuccessMailIds { get; } = [];
    public List<TakeMailAttachmentFailInfo> FailedMails { get; } = [];
}

public record TakeMailAttachmentFailInfo(uint MailId, Retcode Retcode);
