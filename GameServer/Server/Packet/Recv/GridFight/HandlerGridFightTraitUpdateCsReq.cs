using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.GameServer.Game.GridFight.Sync;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightTraitUpdateCsReq)]
public class HandlerGridFightTraitUpdateCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightTraitUpdateCsReq.Parser.ParseFrom(data);

        var inst = connection.Player?.GridFightManager?.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightTraitUpdateScRsp);
            return;
        }

        var traitComp = inst.GetComponent<GridFightTraitComponent>();
        var effect = traitComp.Data.Traits.FirstOrDefault(x => x.TraitId == req.TraitId)?.Effects
            .FirstOrDefault(x => x.EffectId == req.EffectId);

        if (effect == null)
        {
            await connection.SendPacket(CmdIds.GridFightTraitUpdateScRsp);
            return;
        }

        effect.CoreRoleUniqueId = req.TraitCoreRoleInfo.UniqueId;
        // sync
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
            new GridFightTraitSyncData(GridFightSrc.KGridFightSrcTraitEffectUpdate, effect, 0, effect.TraitId,
                effect.EffectId)));

        await connection.SendPacket(CmdIds.GridFightTraitUpdateScRsp);
    }
}