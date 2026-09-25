using March7thHoney.GameServer.Game.DailyActive;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.DailyActive;

/// <summary>The whole daily training window; sent after login and after every point or tier change.</summary>
public class PacketDailyActiveInfoNotify : BasePacket
{
    public PacketDailyActiveInfoNotify(DailyActiveManager daily) : base(CmdIds.DailyActiveInfoNotify)
    {
        var proto = new DailyActiveInfoNotify
        {
            DailyActivePoint = (uint)Math.Max(daily.Point, 0)
        };
        proto.DailyActiveLevelList.AddRange(daily.BuildLevelList());
        proto.DailyActiveQuestIdList.AddRange(daily.QuestIds.Select(id => (uint)id));
        SetData(proto);
    }
}
