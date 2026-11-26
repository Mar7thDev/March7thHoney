using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.MapRotation;

public class PacketRotateMapScRsp : BasePacket
{
    /// <summary>The room rotation and the resulting transform are client-owned; the response echoes its motion.</summary>
    public PacketRotateMapScRsp(MotionInfo? motion, int clientPosVersion) : base(CmdIds.RotateMapScRsp)
    {
        SetData(new RotateMapScRsp
        {
            Motion = motion,
            ClientPosVersion = (uint)clientPosVersion
        });
    }
}
