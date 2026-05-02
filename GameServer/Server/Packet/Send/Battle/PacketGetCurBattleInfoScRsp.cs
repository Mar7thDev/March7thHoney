using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Battle;

public class PacketGetCurBattleInfoScRsp : BasePacket
{
    public PacketGetCurBattleInfoScRsp() : base(CmdIds.GetCurBattleInfoScRsp)
    {
        var proto = new GetCurBattleInfoScRsp
        {
            BattleInfo = new SceneBattleInfo()
        };

        SetData(proto);
    }
}