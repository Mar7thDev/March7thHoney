using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Lineup;

/// <summary>
///     标记常用队伍。4.6 的回包是 cmd 773，4.7 是 cmd 780 <see cref="SetTeamFavourite" />（字段 index / _is_favourite / retcode
///     与请求一一对应，yuukisr_go 6bb4fd1 曾把它命名为 MarkPresetLineupScRsp）。按请求名推不出这个回包名，
///     没有处理器时客户端收不到任何应答。
/// </summary>
[Opcode(CmdIds.MarkPresetLineupCsReq)]
public class HandlerMarkPresetLineupCsReq : Handler<MarkPresetLineupCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, MarkPresetLineupCsReq req)
    {
        var ok = player.LineupManager!.SetLineupFavourite((int)req.Index, req.IsFavourite);
        await connection.SendPacket(new PacketSetTeamFavourite(req.Index, req.IsFavourite,
            ok ? Retcode.RetSucc : Retcode.RetLineupNotExist));
    }
}
