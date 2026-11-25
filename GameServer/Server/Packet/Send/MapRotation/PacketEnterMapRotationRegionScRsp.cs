using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.MapRotation;

public class PacketEnterMapRotationRegionScRsp : BasePacket
{
    public PacketEnterMapRotationRegionScRsp(EnterMapRotationRegionCsReq req, int clientPosVersion, int curNum,
        int maxNum) : base(CmdIds.EnterMapRotationRegionScRsp)
    {
        var proto = new EnterMapRotationRegionScRsp
        {
            Motion = req.Motion,
            RegionId = req.RegionId,
            ClientPosVersion = (uint)clientPosVersion,
            EnergyInfo = new RotaterEnergyInfo
            {
                CurNum = (uint)curNum,
                MaxNum = (uint)maxNum
            }
        };

        SetData(proto);
    }
}
