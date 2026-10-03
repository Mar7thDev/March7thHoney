using March7thHoney.GameServer.Game.Pet;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Send.Pet;

public class PacketGetPetDataScRsp : BasePacket
{
    public PacketGetPetDataScRsp(PlayerInstance player) : base(CmdIds.GetPetDataScRsp)
    {
        SetData(player.GetComponent<PetManager>().ToGetPetDataProto());
    }
}
