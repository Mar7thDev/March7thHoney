using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;

public class PacketGetLevelRewardTakenListScRsp : BasePacket
{
    public PacketGetLevelRewardTakenListScRsp(PlayerInstance player) : base(CmdIds.GetLevelRewardTakenListScRsp)
    {
        var proto = new GetLevelRewardTakenListScRsp
        {
            LevelRewardTakenList = { player.Data.TakenLevelReward.Select(x => (uint)x) }
        };

        SetData(proto);
    }
}