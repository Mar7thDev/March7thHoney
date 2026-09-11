using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Player.Components;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.FiveDim;

public class PacketGetFiveDimGameDataScRsp : BasePacket
{
    public PacketGetFiveDimGameDataScRsp(PlayerInstance player) : base(CmdIds.GetFiveDimGameDataScRsp)
    {
        var proto = new GetFiveDimGameDataScRsp
        {
            MaxJumpEnergy = player.GetComponent<FiveDimComponent>().MaxJumpEnergy
        };

        SetData(proto);
    }
}
