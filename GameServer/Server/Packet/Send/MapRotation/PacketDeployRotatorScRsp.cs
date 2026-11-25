using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.MapRotation;

public class PacketDeployRotatorScRsp : BasePacket
{
    /// <summary>Echoes the deployed volume (official: rotater_data {group 4, instance 300001}) with the pool left.</summary>
    public PacketDeployRotatorScRsp(RotaterData? rotater, int curNum, int maxNum) : base(CmdIds.DeployRotatorScRsp)
    {
        var proto = new DeployRotatorScRsp
        {
            EnergyInfo = new RotaterEnergyInfo
            {
                CurNum = (uint)curNum,
                MaxNum = (uint)maxNum
            }
        };
        if (rotater != null)
            proto.RotaterData = new RotaterData
            {
                GroupId = rotater.GroupId,
                InstanceId = rotater.InstanceId
            };

        SetData(proto);
    }

    public PacketDeployRotatorScRsp(Retcode retcode) : base(CmdIds.DeployRotatorScRsp)
    {
        SetData(new DeployRotatorScRsp { Retcode = (uint)retcode });
    }
}
