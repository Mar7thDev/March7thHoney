using March7thHoney.GameServer.Game.RogueTourn.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Server.Packet.Send.RogueCommon;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.RogueCommon;

[Opcode(CmdIds.RogueWorkbenchHandleFuncCsReq)]
public class HandlerRogueWorkbenchHandleFuncCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = RogueWorkbenchHandleFuncCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;

        BaseGameEntity? entity = null;
        player.SceneInstance?.Entities.TryGetValue((int)req.PropEntityId, out entity);
        if (entity is not RogueWorkbenchProp prop)
        {
            await connection.SendPacket(new PacketRogueWorkbenchHandleFuncScRsp(Retcode.RetSceneEntityNotExist,
                req.FuncId, null));
            return;
        }

        var func = prop.WorkbenchFuncs.Find(x => x.FuncId == req.FuncId);
        if (func == null)
        {
            await connection.SendPacket(
                new PacketRogueWorkbenchHandleFuncScRsp(Retcode.RetFail, req.FuncId, null));
            return;
        }

        var instance = player.RogueTournManager?.RogueTournInstance;
        if (instance == null)
        {
            await connection.SendPacket(new PacketRogueWorkbenchHandleFuncScRsp(Retcode.RetTournRogueStatusMismatch,
                req.FuncId, null));
            return;
        }

        await instance.HandleFunc(func, req.WorkbenchContent);

        await connection.SendPacket(
            new PacketRogueWorkbenchHandleFuncScRsp(Retcode.RetSucc, req.FuncId, func));
    }
}