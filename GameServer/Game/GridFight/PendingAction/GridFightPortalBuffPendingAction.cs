using March7thHoney.Data;
using March7thHoney.GameServer.Game.GridFight.Sync;
using March7thHoney.GameServer.Server.Packet.Send.GridFight;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight.PendingAction;

public class GridFightPortalBuffPendingAction(GridFightInstance inst) : BaseGridFightPendingAction(inst)
{
    public uint MaxRerollCount { get; set; } = 2;
    public uint CurRerollCount { get; set; }

    public async ValueTask RerollBuff()
    {
        if (MaxRerollCount <= CurRerollCount )
            return;

        CurRerollCount++;
        PortalBuffList = GameData.GridFightPortalBuffData.Keys.OrderBy(_ => Guid.NewGuid()).Take(3).ToList();

        // sync
        await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
            new GridFightPendingActionSyncData(GridFightSrc.KGridFightSrcNone, this)));
    }

    public List<uint> PortalBuffList { get; set; } = GameData.GridFightPortalBuffData.Keys.OrderBy(_ => Guid.NewGuid()).Take(3).ToList();

    public override GridFightPendingAction ToProto()
    {
        return new GridFightPendingAction
        {
            PortalBuffAction = new GridFightPortalBuffActionInfo
            {
                GridFightPortalBuffList = { PortalBuffList },
                MaxRerollCount = MaxRerollCount,
                CurRollCount = CurRerollCount
            },
            QueuePosition = QueuePosition
        };
    }
}