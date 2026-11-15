using March7thHoney.GameServer.Game.Activity.Activities;
using March7thHoney.GameServer.Server.Packet.Send.Activity;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.GetMaterialSubmitActivityDataCsReq)]
public class HandlerGetMaterialSubmitActivityDataCsReq : Handler<GetMaterialSubmitActivityDataCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player,
        GetMaterialSubmitActivityDataCsReq req)
    {
        await connection.SendPacket(
            new PacketGetMaterialSubmitActivityDataScRsp(new MaterialSubmitActivity(player).BuildList()));
    }
}
