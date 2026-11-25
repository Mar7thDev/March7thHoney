using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Enums.Rogue;
using March7thHoney.GameServer.Server.Packet.Send.Rogue;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Rogue;

public partial class RogueManager
{
    /// <summary>
    ///     What an enhance costs in Cosmic Fragments, by the blessing's category. No table prices it; the official 4.5
    ///     World 1 enhance lists price all nine rows this way. An unknown category takes the middle price, never a
    ///     free enhance.
    /// </summary>
    private static int EnhanceCost(BaseRogueBuffExcel row) => row.RogueBuffCategory switch
    {
        RogueBuffCategoryEnum.Common => 100,
        RogueBuffCategoryEnum.Legendary => 160,
        _ => 130
    };

    /// <summary>
    ///     The enhance machine's list: every owned blessing still at level 1 that has a level-2 row, with its price.
    ///     An enhanced blessing leaves the list (official 4.5: 612032 is gone from the list after its enhance). Path
    ///     resonance blessings level up through their own aeon drafts and are not offered.
    /// </summary>
    public List<(int BuffId, int Cost)> GetEnhanceableBuffs()
    {
        if (RogueInstance is not { Status: RogueStatus.Doing } inst) return [];
        return inst.Buffs.Values
            .Where(buff => buff.Level == 1)
            .Select(buff => (buff.BuffId, Excel: EnhanceRow(buff)))
            .Where(entry => entry.Excel != null)
            .OrderBy(entry => entry.BuffId)
            .Select(entry => (entry.BuffId, EnhanceCost(entry.Excel!)))
            .ToList();
    }

    // The level-1 row of an owned blessing that can be enhanced, or null.
    private static RogueBuffExcel? EnhanceRow(RogueBuffData buff)
    {
        return GameData.RogueBuffData.GetValueOrDefault(buff.BuffId * 100 + buff.Level) is RogueBuffExcel
               {
                   IsAeonBuff: false
               } row && GameData.RogueBuffData.ContainsKey(buff.BuffId * 100 + buff.Level + 1)
            ? row
            : null;
    }

    /// <summary>
    ///     Enhances an owned level-1 blessing and sends what the official sends between the request and the response
    ///     (4.7 World 1-3 run, identical for every enhance): the charge row, the coin total, the blessing on the holder
    ///     at its new level, then the grant row. The two rows take the next two queue positions. Returns the new level.
    /// </summary>
    public async ValueTask<(Retcode Ret, int Level)> EnhanceBuff(int buffId)
    {
        if (RogueInstance is not { Status: RogueStatus.Doing } inst) return (Retcode.RetRogueStatusNotMatch, 0);
        if (!inst.Buffs.TryGetValue(buffId, out var owned) || owned.Level != 1 || EnhanceRow(owned) is not { } row)
            return (Retcode.RetRogueSelectBuffNotExist, 0);

        var cost = EnhanceCost(row);
        if (inst.Coin < cost) return (Retcode.RetRogueCoinNotEnough, 0);

        inst.Coin -= cost;
        owned.Level++;
        var chargePosition = ++inst.ActionSeq;
        var grantPosition = ++inst.ActionSeq;
        SaveRun();

        await Player.SendPacket(new PacketSyncRogueCommonActionResultScNotify(
            RogueActionResultRows.MoneyCharge(NHHMEPILLHL.Fjmcihhigia, chargePosition, cost), DICEONDKEFF.Icfoncmfnlg));
        await Player.SendPacket(new PacketSyncRogueVirtualItemInfoScNotify(inst.Coin));
        await Player.SendPacket(new PacketSyncRogueBuffHolderChangeScNotify(buffId, owned.Level));
        await Player.SendPacket(new PacketSyncRogueCommonActionResultScNotify(
            RogueActionResultRows.BuffGrant(NHHMEPILLHL.Fjmcihhigia, grantPosition, buffId, owned.Level),
            DICEONDKEFF.Kddipfacjaj));
        return (Retcode.RetSucc, owned.Level);
    }

    /// <summary>
    ///     Records a drafted blessing on the client in the official order (4.7: holder change, handbook entry, then the
    ///     grant row, all before the pick's response). Without them the blessing book stayed empty and the enhance
    ///     machine showed "No Blessings Obtained".
    /// </summary>
    private async ValueTask SendBuffGranted(int buffId, int level)
    {
        await Player.SendPacket(new PacketSyncRogueBuffHolderChangeScNotify(buffId, level));
        await Player.SendPacket(new PacketSyncRogueHandbookBuffScNotify(buffId));
        await Player.SendPacket(new PacketSyncRogueCommonActionResultScNotify(
            RogueActionResultRows.BuffGrant(NHHMEPILLHL.Midkknagofa, 0, buffId, level), DICEONDKEFF.Nlcdgipgfdj));
    }
}
