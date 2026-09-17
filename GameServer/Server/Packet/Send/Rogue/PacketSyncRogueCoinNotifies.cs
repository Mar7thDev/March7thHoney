using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueCommonActionResultScNotify : BasePacket
{
    public PacketSyncRogueCommonActionResultScNotify(int coin) : base(CmdIds.SyncRogueCommonActionResultScNotify)
    {
        // One coin-acquisition row, source/display values as in the official start capture.
        var proto = new SyncRogueCommonActionResultScNotify
        {
            DisplayType = DICEONDKEFF.Icfoncmfnlg,
            DLOJFEEHLIJ = 101
        };
        proto.CLGGKLNPLCK.Add(new MAOKMPOELCO
        {
            Source = NHHMEPILLHL.Npafmebnfob,
            QueuePosition = 1,
            Detail = new PKHKEEKIODC { DDCLCNPONOH = new NPCIGEBNOFG { Num = (uint)coin } }
        });

        SetData(proto);
    }
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
