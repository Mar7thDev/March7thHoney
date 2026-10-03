using March7thHoney.GameServer.Server.Packet.Send.MiscModule;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.TrainCakeCatch;

// The client hands ELDIONMAAHO straight to CakeCatchModule.SyncTrainRoomGateData; the empty-body reply left it null.
[Opcode(CmdIds.SocialPlayGetDataCsReq)]
public class HandlerSocialPlayGetDataCsReq : PlayerHandler
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player)
    {
        await connection.SendPacket(new PacketPresentEmptyScRsp(CmdIds.SocialPlayGetDataScRsp, new SocialPlayGetDataScRsp()));
    }
}
