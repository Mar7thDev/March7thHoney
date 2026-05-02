using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUseForgeCsReq)]
public class HandlerGridFightUseForgeCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUseForgeCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightUseForgeScRsp);
            return;
        }

        var component = inst.GetComponent<GridFightRoleComponent>();
        await component.UseForgeItem(req.UniqueId, req.ForgeTargetIndex);

        await connection.SendPacket(CmdIds.GridFightUseForgeScRsp);
    }
}