using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueCommonActionResultScNotify : BasePacket
{
    // The classic Simulated Universe sub mode every official result notify carries.
    private const uint SubModeSimulatedUniverse = 101;

    // One coin-acquisition row, source/display values as in the official start capture.
    public PacketSyncRogueCommonActionResultScNotify(int coin) : this(new MAOKMPOELCO
    {
        Source = NHHMEPILLHL.Npafmebnfob,
        QueuePosition = 1,
        Detail = new PKHKEEKIODC { DDCLCNPONOH = new NPCIGEBNOFG { Num = (uint)coin } }
    }, DICEONDKEFF.Icfoncmfnlg)
    {
    }

    public PacketSyncRogueCommonActionResultScNotify(MAOKMPOELCO row, DICEONDKEFF displayType) : base(
        CmdIds.SyncRogueCommonActionResultScNotify)
    {
        var proto = new SyncRogueCommonActionResultScNotify
        {
            DisplayType = displayType,
            DLOJFEEHLIJ = SubModeSimulatedUniverse
        };
        proto.CLGGKLNPLCK.Add(row);

        SetData(proto);
    }
}

/// <summary>The classic Simulated Universe result rows, in the shapes of the official captures.</summary>
public static class RogueActionResultRows
{
    /// <summary>
    ///     Fragments an action paid (a won battle, a smashed destructible): money_result with the gain, never the
    ///     total (official 4.5 World 1: num 12 while the coin rides 74 to 86), source 0.
    /// </summary>
    public static MAOKMPOELCO MoneyGain(int queuePosition, int gain) => new()
    {
        Source = NHHMEPILLHL.Nlcdgipgfdj,
        QueuePosition = (uint)queuePosition,
        Detail = new PKHKEEKIODC { DDCLCNPONOH = new NPCIGEBNOFG { Num = (uint)gain, DisplayType = 1 } }
    };
}

public class PacketSyncRogueVirtualItemInfoScNotify : BasePacket
{
    public PacketSyncRogueVirtualItemInfoScNotify(int coin) : base(CmdIds.SyncRogueVirtualItemInfoScNotify)
    {
        SetData(new SyncRogueVirtualItemInfoScNotify { NIKDPODBIEF = new GAOBHBLFPEC { LKHAIEIDAJL = (uint)coin } });
    }
}

public class PacketCommonRogueUpdateScNotify : BasePacket
{
    public PacketCommonRogueUpdateScNotify(int areaId) : base(CmdIds.CommonRogueUpdateScNotify)
    {
        SetData(new CommonRogueUpdateScNotify
        {
            BMGOLFNGHGH = new KPIHDPGDMKB
            {
                OCAGIAKNGPG = (uint)areaId,
                DGJPGMGPCCE = (uint)areaId + 1
            }
        });
    }
}
