using March7thHoney.Data;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Quest;

public class PacketGetQuestDataScRsp : BasePacket
{
    public PacketGetQuestDataScRsp(PlayerInstance player) : base(CmdIds.GetQuestDataScRsp)
    {
        var proto = new GetQuestDataScRsp();
        foreach (var quest in GameData.QuestDataData.Values)
        {
            if ((player.B51RacingManager?.GetQuestInfo(quest.QuestID) ?? player.MechCraftManager?.GetQuestInfo(quest.QuestID)) is { } activityQuest)
            {
                proto.QuestList.Add(activityQuest.ToProto());
                continue;
            }
            proto.QuestList.Add(new Proto.Quest
            {
                Id = (uint)quest.QuestID,
                Status = player.QuestManager?.GetQuestStatus(quest.QuestID) ?? QuestStatus.QuestNone
            });
        }
        SetData(proto);
    }
}
