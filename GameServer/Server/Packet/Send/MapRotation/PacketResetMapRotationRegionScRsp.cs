using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.MapRotation;

public class PacketResetMapRotationRegionScRsp : BasePacket
{
    public PacketResetMapRotationRegionScRsp(MotionInfo? motion, int clientPosVersion) : base(
        CmdIds.ResetMapRotationRegionScRsp)
    {
        SetData(new ResetMapRotationRegionScRsp
        {
            Motion = motion,
            ClientPosVersion = (uint)clientPosVersion
        });
    }
}
