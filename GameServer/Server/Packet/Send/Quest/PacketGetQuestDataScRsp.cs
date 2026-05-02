using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Quest;

public class PacketGetQuestDataScRsp : BasePacket
{
    public PacketGetQuestDataScRsp(PlayerInstance player) : base(CmdIds.GetQuestDataScRsp)
    {
        var proto = new GetQuestDataScRsp();
        foreach (var quest in GameData.QuestDataData.Values)
            proto.QuestList.Add(new Proto.Quest
            {
                Id = (uint)quest.QuestID,
                Status = player.QuestManager?.GetQuestStatus(quest.QuestID) ?? QuestStatus.QuestNone
            });
        SetData(proto);
    }
}