using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Match;

public class PacketGetCrossInfoScRsp : BasePacket
{
    public PacketGetCrossInfoScRsp() : base(CmdIds.GetCrossInfoScRsp)
    {
        var proto = new GetCrossInfoScRsp
        {
            FightGameMode = (FightGameMode)0
        };

        SetData(proto);
    }
}

