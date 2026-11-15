using March7thHoney.Data;
using March7thHoney.GameServer.Server.Packet.Send.PamSkin;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.PamSkin;

[Opcode(CmdIds.SelectPamSkinCsReq)]
public class HandlerSelectPamSkinCsReq : Handler<SelectPamSkinCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, SelectPamSkinCsReq req)
    {
        // set_skin echoes the request and cur_skin is the skin actually worn; an unknown skin is refused.
        var known = GameData.PamSkinConfigData.ContainsKey((int)req.PamSkin);
        if (known) player.Data.CurrentPamSkin = (int)req.PamSkin;

        await connection.SendPacket(new PacketSelectPamSkinScRsp(player, (int)req.PamSkin,
            known ? Retcode.RetSucc : Retcode.RetFail));
    }
}
