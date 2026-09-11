using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Activity.FinalityBattle;
using March7thHoney.GameServer.Server.Packet.Send.Activity.FinalityBattle;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Activity.FinalityBattle;

[Opcode(CmdIds.StartFinalityBattleLevelCsReq)]
public class HandlerStartFinalityBattleLevelCsReq : Handler<StartFinalityBattleLevelCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, StartFinalityBattleLevelCsReq req)
    {
        var avatars = req.AvatarList.Select(avatar => new FinalityBattleAvatarSelection((int)avatar.Id, avatar.AvatarType)).ToList();
        var (retcode, battle) = player.FinalityBattleManager!.StartLevel((int)req.LevelId, (int)req.LeadingRoleId, avatars);
        await connection.SendPacket(new PacketStartFinalityBattleLevelScRsp(req.LevelId, retcode, battle));
    }
}
