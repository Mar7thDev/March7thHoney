using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketSetClientPausedScRsp : BasePacket
{
    public PacketSetClientPausedScRsp(bool paused) : base(CmdIds.SetClientPausedScRsp)
    {
        var rsp = new SetClientPausedScRsp
        {
            Paused = paused
        };
        SetData(rsp);
    }
}