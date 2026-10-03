using March7thHoney.GameServer.Server.Packet.Send.MiscModule;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.PetMarble;

// The client passes ACCGIILHFKL to PetMarbleManager.SyncTimeLimitedTrialPetMarbles; the empty-body reply left it null.
[Opcode(CmdIds.PetMarbleGetCycleTrialPetsCsReq)]
public class HandlerPetMarbleGetCycleTrialPetsCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketPresentEmptyScRsp(CmdIds.PetMarbleGetCycleTrialPetsScRsp,
            new PetMarbleGetCycleTrialPetsScRsp()));
    }
}
