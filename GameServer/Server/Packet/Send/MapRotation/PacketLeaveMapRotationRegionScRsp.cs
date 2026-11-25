using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.MapRotation;

public class PacketLeaveMapRotationRegionScRsp : BasePacket
{
    public PacketLeaveMapRotationRegionScRsp(MotionInfo? motion, int clientPosVersion) : base(
        CmdIds.LeaveMapRotationRegionScRsp)
    {
        SetData(new LeaveMapRotationRegionScRsp
        {
            Motion = motion,
            ClientPosVersion = (uint)clientPosVersion
        });
    }
}
