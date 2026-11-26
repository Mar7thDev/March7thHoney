using System.Text;
using March7thHoney.Data;
using March7thHoney.Database.Message;
using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Server;
using March7thHoney.Internationalization;

namespace March7thHoney.Command.Command.Cmd;

/// <summary>
///     Inspects and resets phone-message (chat) state. A section saved by an older build can hold a shape no client
///     renders, such as a mission card in its history and nothing offered; the offer and the history are derived from
///     the config and the client's picks, so the only repair is to drop the group and let its mission start it again.
/// </summary>
[CommandInfo("message", "Game.Command.Message.Desc", "Game.Command.Message.Usage", ["msg"],
    permission: CommandPermissions.Mission)]
public class CommandMessage : ICommand
{
    [CommandMethod("0 list")]
    public async ValueTask ListGroups(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var groups = arg.Target.Player!.MessageManager!.Data.Groups.Values.OrderBy(group => group.GroupId).ToList();
        if (groups.Count == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Message.NoGroups"));
            return;
        }

        var text = new StringBuilder();
        foreach (var group in groups)
        {
            text.AppendLine(I18NManager.Translate("Game.Command.Message.GroupLine", group.GroupId.ToString(),
                StatusName(group.Status.ToString(), "MessageGroup"), group.CurrentSectionId.ToString()));
            foreach (var section in group.Sections)
                text.AppendLine(I18NManager.Translate("Game.Command.Message.SectionLine",
                    section.SectionId.ToString(), StatusName(section.Status.ToString(), "MessageSection"),
                    string.Join(", ", section.ToChooseItemId), string.Join(", ", History(section))));
        }

        await arg.SendMsg(text.ToString().TrimEnd());
    }

    [CommandMethod("0 reset")]
    public async ValueTask ResetGroup(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (!arg.TryGetInt(0, out var id))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        // A group ID or the ID of one of its sections.
        var groupId = GameData.MessageGroupConfigData.ContainsKey(id) ? id
            : GameData.MessageSectionConfigData.TryGetValue(id, out var sectionConfig) ? sectionConfig.GroupID
            : 0;
        if (groupId == 0 || !GameData.MessageGroupConfigData.TryGetValue(groupId, out var groupConfig))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Message.NotFound", id.ToString()));
            return;
        }

        var player = arg.Target.Player!;
        var sectionIds = player.MessageManager!.ResetGroup(groupId);
        await arg.SendMsg(I18NManager.Translate(sectionIds.Count > 0
            ? "Game.Command.Message.GroupDropped"
            : "Game.Command.Message.NothingStored", groupId.ToString()));
        if (sectionIds.Count == 0) sectionIds = [.. groupConfig.MessageSectionIDList];

        // A chat is opened by the sub-mission that waits for it. While that main mission stays finished nothing
        // opens the chat again and the reset would only make it disappear, so the finished main is accepted again;
        // its accept starts the sections of its message subs. A running main reopens the chat at the next login.
        var rewound = 0;
        if (player.MissionManager!.Enabled)
            foreach (var main in GameData.MainMissionData.Values.OrderBy(main => main.MainMissionID))
            {
                if (player.MissionManager.Data.GetMainMissionStatus(main.MainMissionID) != MissionPhaseEnum.Finish)
                    continue;
                if (!main.MissionInfo.SubMissionList.Any(sub =>
                        (sub.FinishType is MissionFinishTypeEnum.MessageSectionFinish
                            or MissionFinishTypeEnum.MessagePerformSectionFinish) &&
                        sectionIds.Contains(sub.ParamInt1)))
                    continue;

                await player.MissionManager.ReAcceptMainMission(main.MainMissionID);
                rewound++;
                await arg.SendMsg(I18NManager.Translate("Game.Command.Message.MainRewound",
                    main.MainMissionID.ToString()));
            }

        if (rewound == 0) await arg.SendMsg(I18NManager.Translate("Game.Command.Message.NoMainRewound"));

        // The client keeps the chat it already loaded; a new login fetches the rebuilt phone.
        ConnectionDisconnectHelper.KickByGm(arg.Target);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Message.Kicked"));
    }

    private static IEnumerable<int> History(MessageSectionData section)
    {
        if (section.ConfirmedItemIds.Count > 0) return section.ConfirmedItemIds;
        return section.Items.Count > 0 ? [section.Items[^1].ItemId] : [];
    }

    private static string StatusName(string name, string prefix)
    {
        return name.StartsWith(prefix, StringComparison.Ordinal) ? name[prefix.Length..] : name;
    }
}
