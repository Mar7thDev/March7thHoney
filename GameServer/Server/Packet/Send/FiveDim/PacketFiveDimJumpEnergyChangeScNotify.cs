using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Player.Components;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.FiveDim;

public class PacketFiveDimJumpEnergyChangeScNotify : BasePacket
{
    public PacketFiveDimJumpEnergyChangeScNotify(PlayerInstance player) : base(CmdIds.FiveDimJumpEnergyChangeScNotify)
    {
        var proto = new FiveDimJumpEnergyChangeScNotify
        {
            MaxJumpEnergy = player.GetComponent<FiveDimComponent>().MaxJumpEnergy
        };

        SetData(proto);
    }
}
