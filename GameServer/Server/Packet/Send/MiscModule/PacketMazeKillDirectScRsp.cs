using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.MiscModule;

public class PacketMazeKillDirectScRsp : BasePacket
{
    public PacketMazeKillDirectScRsp(List<uint> entityIds) : base(CmdIds.MazeKillDirectScRsp)
    {
        var proto = new MazeKillDirectScRsp
        {
            EntityList = { entityIds }
        };

        SetData(proto);
    }
}