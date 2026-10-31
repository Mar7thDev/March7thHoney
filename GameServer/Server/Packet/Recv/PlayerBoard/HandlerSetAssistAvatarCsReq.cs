using March7thHoney.Database;
using March7thHoney.GameServer.Game.Friend;
using March7thHoney.GameServer.Server.Packet.Send.PlayerBoard;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.PlayerBoard;

[Opcode(CmdIds.SetAssistAvatarCsReq)]
public class HandlerSetAssistAvatarCsReq : Handler<SetAssistAvatarCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, SetAssistAvatarCsReq req)
    {
        var data = player.AvatarManager!.Data!;
        var requested = req.AvatarIdList.Count > 0 ? req.AvatarIdList.ToList() : [req.AvatarId];
        List<int> stored = [];
        List<uint> accepted = [];
        foreach (var id in requested)
        {
            if (id == 0) continue;
            // The client publishes the hero by path id (8008); storage keeps the base id.
            var avatar = data.FindByAnyId((int)id);
            if (avatar == null)
            {
                await connection.SendPacket(new PacketSetAssistAvatarScRsp([], Retcode.RetAvatarNotExist));
                return;
            }

            if (stored.Contains(avatar.BaseAvatarId) || stored.Count >= FriendManager.MaxPublishedAssists) continue;
            stored.Add(avatar.BaseAvatarId);
            accepted.Add(id);
        }

        data.AssistAvatars.Clear();
        data.AssistAvatars.AddRange(stored);
        DatabaseHelper.MarkDirty(player.Uid);
        await connection.SendPacket(new PacketSetAssistAvatarScRsp(accepted));
    }
}
