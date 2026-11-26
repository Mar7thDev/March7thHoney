using March7thHoney.Data;
using March7thHoney.GameServer.Game.RogueCommon;
using March7thHoney.GameServer.Server.Packet.Send.Rogue;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Rogue;

/// <summary>
///     The run-start "Blessing of Trailblaze" picker. Every official 4.7 World 1-3 run offers one fragment row and one
///     blessing row of RogueBonus, in random order, at queue position 2 (the start coin is 1); its answer pays the
///     fragments or opens a blessing draft. Rows 4-6 are the enhanced tier of permanent talent 12 ("All Blessings of
///     Trailblaze effects are enhanced"); the Miracle rows (3, 6) are never offered, in line with every capture.
/// </summary>
public partial class RogueManager
{
    private const int TrailblazeTalentId = 12;

    // RogueBonus row => Cosmic Fragments it pays, from each row's own text ("Obtain 100/150 Cosmic Fragments").
    private static readonly Dictionary<int, int> BonusFragmentRows = new() { [1] = 100, [4] = 150 };

    // RogueBonus rows that open a blessing draft ("Obtain a 1-star Blessing" / "a Blessing of 1-2 stars"); the
    // official draft after row 2 deals 612052/612054/612656, all from the ordinary draft pool.
    private static readonly HashSet<int> BonusBlessingRows = [2, 5];

    /// <summary>
    ///     Offers the picker on a new run. The tutorial area (100) is a scripted layout whose captures show no offer.
    /// </summary>
    private void RollBonusOffer(RogueInstance inst)
    {
        if (inst.AreaId == 100) return;

        var enhanced = HasTalent(TrailblazeTalentId);
        List<int> offer = [enhanced ? 4 : 1, enhanced ? 5 : 2];
        if (offer.Any(id => !GameData.RogueBonusData.ContainsKey(id))) return;
        if (Random.Shared.Next(2) == 1) offer.Reverse();

        inst.BonusOffer = offer;
        inst.ActionSeq = 2;
    }

    /// <summary>
    ///     Answers the picker (official 4.7 order): a fragment row sends its money row and the coin total, a blessing row
    ///     its draft; then the response echoes the offer's own queue position. The offer is cleared in the same save
    ///     that records the payment, so a repeated answer cannot pay twice.
    /// </summary>
    public async ValueTask<HandleRogueCommonPendingActionScRsp?> AnswerBonus(int bonusId)
    {
        if (RogueInstance is not { Status: RogueStatus.Doing, BonusOffer.Count: > 0 } inst) return null;

        var rsp = new HandleRogueCommonPendingActionScRsp { Retcode = (uint)Retcode.RetRogueSelectBuffNotExist };
        if (!inst.BonusOffer.Contains(bonusId)) return rsp;

        var fragments = BonusFragmentRows.GetValueOrDefault(bonusId);
        var draft = BonusBlessingRows.Contains(bonusId)
            ? RogueBuffSelectMenu.Create(inst, GetBuffPool(RogueDraftGroups.Normal))
            : null;
        if (fragments == 0 && draft is not { HasSelection: true }) return rsp;
        rsp.Retcode = (uint)Retcode.RetSucc;

        var offerPosition = inst.ActionSeq;
        inst.BonusOffer = [];
        inst.ActionSeq = offerPosition + 1;
        inst.Coin += fragments;
        if (draft != null)
        {
            inst.ActionSeq++;
            inst.BuffSelect = draft;
        }

        SaveRun();

        if (fragments > 0)
        {
            await Player.SendPacket(new PacketSyncRogueCommonActionResultScNotify(
                RogueActionResultRows.MoneyGain(NHHMEPILLHL.Epcdlhffbli, inst.ActionSeq, fragments),
                DICEONDKEFF.Icfoncmfnlg));
            await Player.SendPacket(new PacketSyncRogueVirtualItemInfoScNotify(inst.Coin));
        }

        if (draft != null)
            await Player.SendPacket(new PacketSyncRogueCommonPendingActionScNotify(BuildPendingAction(inst)));

        rsp.QueuePosition = (uint)offerPosition;
        return rsp;
    }
}
