using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Player;

public class PacketStaminaInfoScNotify : BasePacket
{
    public PacketStaminaInfoScNotify(PlayerInstance player) : base(CmdIds.StaminaInfoScNotify)
    {
        var proto = new StaminaInfoScNotify
        {
            Stamina = (uint)player.Data.Stamina,
            ReserveStamina = (uint)player.Data.StaminaReserve,
            NextRecoverTime = player.Data.NextStaminaRecover
        };

        SetData(proto);
    }
}