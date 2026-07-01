using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Mission;

public class PacketTeleportToMissionResetPointScRsp : BasePacket
{
    public PacketTeleportToMissionResetPointScRsp(PlayerInstance player) : base(CmdIds.TeleportToMissionResetPointScRsp)
    {
        SetData(new TeleportToMissionResetPointScRsp
        {
            Motion = new MotionInfo
            {
                Pos = player.Data.Pos?.ToProto() ?? new Vector(),
                Rot = player.Data.Rot?.ToProto() ?? new Vector()
            }
        });
    }
}
