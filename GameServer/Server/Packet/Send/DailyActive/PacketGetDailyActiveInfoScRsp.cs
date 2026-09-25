using March7thHoney.GameServer.Game.DailyActive;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.DailyActive;

public class PacketGetDailyActiveInfoScRsp : BasePacket
{
    public PacketGetDailyActiveInfoScRsp(DailyActiveManager daily) : base(CmdIds.GetDailyActiveInfoScRsp)
    {
        var proto = new GetDailyActiveInfoScRsp
        {
            DailyActivePoint = (uint)Math.Max(daily.Point, 0)
        };
        proto.DailyActiveLevelList.AddRange(daily.BuildLevelList());
        proto.DailyActiveQuestIdList.AddRange(daily.QuestIds.Select(id => (uint)id));
        SetData(proto);
    }
}
