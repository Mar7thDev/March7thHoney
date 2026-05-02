using CastoriceHoney.Server.GameServer.Game.Scene.Entity;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.MiscModule;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Recv.MiscModule;

[Opcode(CmdIds.MazeKillDirectCsReq)]
public class HandlerMazeKillDirectCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = MazeKillDirectCsReq.Parser.ParseFrom(data);

        foreach (var entityId in req.EntityList.ToList())
        {
            if (!connection.Player!.SceneInstance!.Entities.TryGetValue((int)entityId, out var entity)) continue;
            if (entity is EntityMonster monster)
                await monster.Kill();
            else
                // remove entity if it's not a monster
                connection.Player.SceneInstance.Entities.Remove((int)entityId);
        }

        await connection.SendPacket(new PacketMazeKillDirectScRsp(req.EntityList.ToList()));
    }
}