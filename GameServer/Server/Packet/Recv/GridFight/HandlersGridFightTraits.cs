using Google.Protobuf;
using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;
using static March7thHoney.GameServer.Server.Packet.Recv.GridFight.GridFightPacketFactory;

namespace March7thHoney.GameServer.Server.Packet.Recv.GridFight;

/// <summary>
///     GridFight request handler that, after the request itself, pushes whatever season-2 trait state the request
///     changed indirectly (Tenglong summon, template equipment, effect snapshots, trait prompts).
/// </summary>
public abstract class GridFightHandler<TReq> : Handler<TReq> where TReq : IMessage<TReq>, new()
{
    protected sealed override async Task OnHandle(Connection connection, PlayerInstance player, TReq req)
    {
        await OnGridFightHandle(connection, player, req);
        if (player.GridFightManager?.BuildSeasonTwoFollowUpSync() is { } followUp)
            await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(followUp));
    }

    protected abstract Task OnGridFightHandle(Connection connection, PlayerInstance player, TReq req);
}

/// <summary>
///     Season-2 trait interactions the client starts itself (GridFightTraitUpdateCsReq 8657). The client blocks its
///     input until GridFightTraitUpdateScRsp arrives, so every request is answered; state goes in the sync first.
/// </summary>
[Opcode(CmdIds.GridFightTraitUpdateCsReq)]
public sealed class HandlerGridFightTraitUpdateCsReq : GridFightHandler<GridFightTraitUpdateCsReq>
{
    protected override async Task OnGridFightHandle(
        Connection connection,
        PlayerInstance player,
        GridFightTraitUpdateCsReq request)
    {
        var manager = player.GridFightManager!;
        var session = manager.Session;
        var sync = session == null ? null : Handle(manager, session, request);
        if (sync != null) await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(sync));
        await connection.SendPacket(CreatePacket(CmdIds.GridFightTraitUpdateScRsp, new GridFightTraitUpdateScRsp
        {
            Retcode = sync != null ? 0 : (uint)Retcode.RetFail,
        }));
    }

    private static GridFightSyncUpdateResultScNotify? Handle(
        GridFightManager manager,
        GridFightSession session,
        GridFightTraitUpdateCsReq request)
    {
        var traitId = request.TraitId;
        var effectId = request.EffectId;
        switch (request.NEBNJDMFELOCase)
        {
            case GridFightTraitUpdateCsReq.NEBNJDMFELOOneofCase.GDAMHENIIDJ:
            {
                // 黑市主理人: buy a slot or refresh the rotating goods.
                if (request.GDAMHENIIDJ.AHAIBDAJAIB)
                {
                    if (!session.RefreshJadeGoods(out var goldChanged)) return null;
                    var refresh = new GridFightRewardApplication { Success = true, GoldChanged = goldChanged };
                    return manager.BuildSeasonTwoSelectionSync(traitId, effectId, 0, refresh, includeShop: true);
                }
                var purchase = session.BuyJadeGood(request.GDAMHENIIDJ.EAKAGIKFENM);
                return purchase.Success ? manager.BuildSeasonTwoSelectionSync(traitId, effectId, 0, purchase, includeShop: true) : null;
            }
            case GridFightTraitUpdateCsReq.NEBNJDMFELOOneofCase.KDBKIPPHMFI:
            {
                var purchase = session.BuyTrailblazeGood(request.KDBKIPPHMFI.Pos);
                return purchase.Success ? manager.BuildSeasonTwoSelectionSync(traitId, effectId, 0, purchase) : null;
            }
            case GridFightTraitUpdateCsReq.NEBNJDMFELOOneofCase.MCCMPFENGCD:
            {
                var change = session.ConsumeRoleForPhainon(request.MCCMPFENGCD.MDNAGANGNKK, out _);
                return change.Success ? manager.BuildSeasonTwoSelectionSync(traitId, effectId, 0, change: change) : null;
            }
            case GridFightTraitUpdateCsReq.NEBNJDMFELOOneofCase.GEPGMIEPBMB:
                session.SelectDanHengTrait(0, request.GEPGMIEPBMB.KJMFHCDACEK, out var chosen);
                return chosen ? manager.BuildSeasonTwoSelectionSync(traitId, effectId, 0) : null;
            case GridFightTraitUpdateCsReq.NEBNJDMFELOOneofCase.DPKFJEEJABC:
                session.AppointRanger(0, request.DPKFJEEJABC.RoleId, out var appointed);
                return appointed ? manager.BuildSeasonTwoSelectionSync(traitId, effectId, 0) : null;
            case GridFightTraitUpdateCsReq.NEBNJDMFELOOneofCase.FLDEMEJEODJ:
                return session.SetMemberLink(request.FLDEMEJEODJ.LKNGKJCFFBH.ToList())
                    ? manager.BuildSeasonTwoSelectionSync(traitId, effectId, 0)
                    : null;
            case GridFightTraitUpdateCsReq.NEBNJDMFELOOneofCase.IFDICIIOHHI
                when request.IFDICIIOHHI.HCDIBHEDDEC != null:
            {
                // 欢愉: drag equipment onto Aha. (Aha consumables, HJJPILGEFKK, are not implemented.)
                var dress = session.DressElationEquipment(request.IFDICIIOHHI.HCDIBHEDDEC.AMIFDFAHMDM, out var consumed);
                return dress.Success
                    ? manager.BuildSeasonTwoSelectionSync(traitId, effectId, 0, dress, removedEquipment: consumed)
                    : null;
            }
            default:
                return null;
        }
    }
}

/// <summary>
///     The client asks to enter a 幕后茶歇 node (8559, sent from Lua). The bonfire pending action is pushed on node
///     entry already; resend it here. Proto/4.7 has no matching response command, so none is sent.
/// </summary>
[Opcode(CmdIds.GridFightEnterBonfireCsReq)]
public sealed class HandlerGridFightEnterBonfireCsReq : GridFightHandler<GridFightEnterBonfireCsReq>
{
    protected override async Task OnGridFightHandle(
        Connection connection,
        PlayerInstance player,
        GridFightEnterBonfireCsReq request)
    {
        var manager = player.GridFightManager!;
        if (manager.Session?.SeasonTwoPending?.Kind == GridFightSeasonTwoPendingKind.Bonfire)
            await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(manager.BuildSeasonTwoPendingRefreshSync()));
    }
}
