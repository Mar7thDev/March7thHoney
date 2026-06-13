using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Scene;

[Opcode(CmdIds.SceneCastSkillCostMpCsReq)]
public class HandlerSceneCastSkillCostMpCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SceneCastSkillCostMpCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        await player.LineupManager!.CostMp(1, req.CastEntityId);

        // The MP-cost packet is the canonical "technique released" signal. Flag the casting avatar so
        // its technique maze buffs are injected into the next battle (BattleManager.StartBattle) —
        // gated strictly on this actual scene cast, never on freesr. We only record the intent here;
        // the buffs are applied as battle data (not scene buffs/modifiers), so a caster-targeted
        // enemy modifier can't take effect on the caster while still in the scene. The caster avatar
        // entity is AttackedByEntityId (matches SceneSkillManager.OnCast).
        var caster = player.SceneInstance?.AvatarInfo.GetValueOrDefault((int)req.AttackedByEntityId);
        if (caster != null)
            player.SceneInstance!.CastTechniqueAvatarIds.Add(caster.AvatarInfo.BaseAvatarId);

        await connection.SendPacket(new PacketSceneCastSkillCostMpScRsp((int)req.CastEntityId));
    }
}
