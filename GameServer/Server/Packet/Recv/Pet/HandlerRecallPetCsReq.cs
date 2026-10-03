using March7thHoney.GameServer.Game.Pet;
using March7thHoney.GameServer.Server.Packet.Send.Pet;
using March7thHoney.Kcp;

namespace March7thHoney.GameServer.Server.Packet.Recv.Pet;

[Opcode(CmdIds.RecallPetCsReq)]
public class HandlerRecallPetCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        player.GetComponent<PetManager>().Recall();

        await connection.SendPacket(new PacketRecallPetScRsp());
        await player.TrainCakeCatchManager!.BroadcastPlayerStateAsync();
    }
}
