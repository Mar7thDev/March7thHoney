using March7thHoney.GameServer.Game.Activity.Activities;
using March7thHoney.GameServer.Server.Packet.Send.Activity;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.SubmitMaterialSubmitActivityMaterialCsReq)]
public class HandlerSubmitMaterialSubmitActivityMaterialCsReq : Handler<SubmitMaterialSubmitActivityMaterialCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player,
        SubmitMaterialSubmitActivityMaterialCsReq req)
    {
        var (retcode, entry) = await new MaterialSubmitActivity(player).Submit((int)req.LNOCOOCMNCI);
        await connection.SendPacket(new PacketSubmitMaterialSubmitActivityMaterialScRsp(retcode, entry));
    }
}
