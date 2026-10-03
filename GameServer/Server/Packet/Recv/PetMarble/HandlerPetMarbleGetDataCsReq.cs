using March7thHoney.GameServer.Server.Packet.Send.MiscModule;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.PetMarble;

// PetMarbleModule._OnPetMarbleGetDataScRsp dereferences BJBJLBIOPKA; the empty-body reply left it null.
[Opcode(CmdIds.PetMarbleGetDataCsReq)]
public class HandlerPetMarbleGetDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketPresentEmptyScRsp(CmdIds.PetMarbleGetDataScRsp, new PetMarbleGetDataScRsp()));
    }
}
