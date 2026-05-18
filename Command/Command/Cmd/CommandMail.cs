using March7thHoney.Data;
using March7thHoney.Database.Inventory;
using March7thHoney.Internationalization;
using March7thHoney.Util;

namespace March7thHoney.Command.Command.Cmd;

[CommandInfo("mail", "Game.Command.Mail.Desc", "Game.Command.Mail.Usage", permission: CommandPermissions.Mail)]
public class CommandMail : ICommand
{
    [CommandMethod("welcome")]
    public async ValueTask WelcomeMail(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var player = arg.Target.Player!;
        var config = ConfigManager.Config.ServerOption.WelcomeMail;
        var attachments = config.Items
            .Where(item => item.ItemId > 0 && item.Count > 0 && GameData.ItemConfigData.ContainsKey(item.ItemId))
            .Select(item => new ItemData
            {
                ItemId = item.ItemId,
                Count = item.Count,
                Rank = item.Rank,
                Level = item.Level,
                Promotion = item.Promotion
            })
            .ToList();

        await player.MailManager!.SendMail(
            FormatText(config.Sender, player.Uid, player.Data.Name ?? ""),
            FormatText(config.Title, player.Uid, player.Data.Name ?? ""),
            FormatText(config.Content, player.Uid, player.Data.Name ?? ""),
            config.TemplateId,
            attachments,
            config.ExpireDays);

        await arg.SendMsg(I18NManager.Translate("Game.Command.Mail.MailSentWithAttachment"));
    }

    [CommandDefault]
    public async ValueTask Mail(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (arg.Args.Count < 7)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (!(arg.Args.Contains("_TITLE") && arg.Args.Contains("_CONTENT")))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (!int.TryParse(arg.Args[1], out var templateId) || !int.TryParse(arg.Args[2], out var expiredDay))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var sender = arg.Args[0];
        var title = "";
        var content = "";
        var attachmentArgs = new List<string>();

        var flagTitle = false;
        var flagContent = false;
        var flagAttachment = false;
        foreach (var text in arg.Args)
        {
            switch (text)
            {
                case "_TITLE":
                    flagTitle = true;
                    continue;
                case "_CONTENT":
                    flagContent = true;
                    continue;
                case "_ATTACH":
                case "_ATTACHMENT":
                case "_ATTACHMENTS":
                    flagAttachment = true;
                    continue;
            }

            if (flagAttachment)
            {
                attachmentArgs.Add(text);
                continue;
            }

            if (flagTitle && !flagContent) title += text + " ";


            if (flagTitle && flagContent) content += text + " ";
        }

        content = content[..^1];
        title = title[..^1];

        var attachments = ParseAttachments(attachmentArgs);
        if (attachments == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        if (attachments.Count > 0)
        {
            await arg.Target.Player!.MailManager!.SendMail(sender, title, content, templateId, attachments, expiredDay);
            await arg.SendMsg(I18NManager.Translate("Game.Command.Mail.MailSentWithAttachment"));
            return;
        }

        await arg.Target.Player!.MailManager!.SendMail(sender, title, content, templateId, expiredDay);

        await arg.SendMsg(I18NManager.Translate("Game.Command.Mail.MailSent"));
    }

    private static List<ItemData>? ParseAttachments(List<string> args)
    {
        var attachments = new List<ItemData>();
        if (args.Count == 0) return attachments;

        for (var i = 0; i < args.Count;)
        {
            if (!int.TryParse(args[i], out var itemId) || itemId <= 0)
                return null;

            if (!GameData.ItemConfigData.ContainsKey(itemId))
                return null;

            var attachment = new ItemData
            {
                ItemId = itemId,
                Count = 1,
                Rank = 1,
                Level = 1,
                Promotion = 0
            };

            i++;
            var hasBareCount = false;
            var allowBareCount = true;
            while (i < args.Count)
            {
                var token = args[i];
                if (TryParseAttachmentModifier(token, attachment))
                {
                    if (char.ToLowerInvariant(token[0]) == 'x') hasBareCount = true;
                    allowBareCount = false;
                    i++;
                    continue;
                }

                if (allowBareCount && !hasBareCount && int.TryParse(token, out var count))
                {
                    if (count <= 0) return null;
                    attachment.Count = count;
                    hasBareCount = true;
                    allowBareCount = false;
                    i++;
                    continue;
                }

                break;
            }

            attachments.Add(attachment);
        }

        return attachments;
    }

    private static bool TryParseAttachmentModifier(string token, ItemData attachment)
    {
        if (token.Length < 2) return false;

        var prefix = char.ToLowerInvariant(token[0]);
        if (prefix is not ('x' or 'r' or 'l')) return false;
        if (!int.TryParse(token[1..], out var value)) return false;

        switch (prefix)
        {
            case 'x':
                if (value <= 0) return false;
                attachment.Count = value;
                return true;
            case 'r':
                if (value < 0) return false;
                attachment.Rank = value;
                return true;
            case 'l':
                if (value <= 0) return false;
                attachment.Level = value;
                return true;
            default:
                return false;
        }
    }

    private static string FormatText(string value, int uid, string username)
    {
        return value
            .Replace("{uid}", uid.ToString())
            .Replace("{username}", username)
            .Replace("{account}", username);
    }
}
