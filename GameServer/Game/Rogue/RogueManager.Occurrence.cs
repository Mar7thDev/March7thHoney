using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.GameServer.Game.RogueTourn;
using March7thHoney.GameServer.Server.Packet.Send.Rogue;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Rogue;

/// <summary>
///     An Occurrence room (RogueRoom type 3) has no monsters: it is cleared by talking to its event NPC, whose Act graph
///     the CLIENT runs. Official 4.7 (World 1-3 run): the talk is announced just before the room's response and the NPC
///     carries its handle; each pick gets the option-finish notify, then the response with the dialogue events it
///     resolves; the room goes Finish and the next room Unlock once the talk is over, and an empty live set clears it.
/// </summary>
/// <remarks>
///     The option pool is the event's cached Opt table. Which picks end the talk and which dialogue events they report
///     come from the event's Act graph, which only a Resource.bin built with this change carries; without it every pick
///     ends the talk, which is exact for the single-choice events (World 1's 40398 among them). The client's own
///     FinishRogueCommonDialogueCsReq is honoured but never waited for: a 4.7 client was measured not to send it.
/// </remarks>
public partial class RogueManager
{
    // The talk whose closing pick was answered without a draft; it closes after the pick's response.
    private int _closingDialogueHandle;

    // Opens the talk of the Occurrence room being entered, once per room, before its scene builds the NPC. A finished
    // talk is never replayed.
    private void OpenRoomDialogue(RogueInstance inst, RogueRoomData room)
    {
        if (room.RoomType != RoomTypeOccurrence || inst.Dialogues.Values.Any(d => d.SiteId == room.SiteId)) return;

        var npcId = OccurrenceNpcId(room);
        var options = DialogueOptions(npcId);
        if (options.Count == 0) return;

        var handle = ++inst.DialogueSeq;
        inst.Dialogues[handle] = new RogueDialogueData
        {
            Handle = handle,
            NpcId = npcId,
            SiteId = room.SiteId,
            Options = options
        };
    }

    // The room's event: its first content value, by group, that names a RogueNPC row (room 305 maps group 21 to 40398).
    private static int OccurrenceNpcId(RogueRoomData room)
    {
        if (!GameData.RogueRoomData.TryGetValue(room.RoomId, out var excel)) return 0;
        return excel.GroupWithContent.OrderBy(entry => entry.Key).Select(entry => entry.Value)
            .FirstOrDefault(content => content != 0 && GameData.RogueNPCData.ContainsKey(content));
    }

    // Every option of the event's Opt table, ascending: the official announce of npc 10801 carries exactly its six.
    private static List<int> DialogueOptions(int npcId)
    {
        var options = GameData.RogueNPCData.GetValueOrDefault(npcId)?.RogueNpcConfig?.DialogueList.FirstOrDefault()
            ?.OptionInfo?.OptionList;
        return options?.Select(option => option.OptionID).Where(id => id != 0).Distinct().Order().ToList() ?? [];
    }

    private RogueDialogueData? CurrentRoomDialogue()
    {
        if (RogueInstance?.GetCurrentRoom() is not { } room) return null;
        return RogueInstance.Dialogues.Values.FirstOrDefault(d => d.SiteId == room.SiteId && !d.Closed);
    }

    /// <summary>Whether the current room's talk is still open; its exit stays shut until it closes.</summary>
    internal bool HasOpenRoomDialogue() => CurrentRoomDialogue() != null;

    /// <summary>The rogue_game_info of an NPC in the event group of the current room's open talk, or null.</summary>
    internal NpcRogueGameInfo? GetDialogueNpcInfo(int groupId)
    {
        if (CurrentRoomDialogue() is not { } dialogue || RogueInstance?.GetCurrentRoom() is not { } room ||
            !GameData.RogueRoomData.TryGetValue(room.RoomId, out var excel) ||
            excel.GroupWithContent.GetValueOrDefault(groupId) != dialogue.NpcId) return null;
        return new NpcRogueGameInfo { AIAFEKGBFIF = (uint)dialogue.Handle, OEANPIHCHHF = (uint)dialogue.NpcId };
    }

    /// <summary>Announces the current room's open talk; sent before the room's response (official order).</summary>
    public async ValueTask AnnounceRoomDialogue()
    {
        if (CurrentRoomDialogue() != null) await SendDialogueSet();
    }

    // The live set is a snapshot: rows announce, an empty list clears.
    private async ValueTask SendDialogueSet()
    {
        var notify = new SyncRogueCommonDialogueDataScNotify();
        notify.DialogueList.AddRange(LiveDialogues().Select(BuildDialogueRow));
        await Player.SendPacket(BasePacket.Create(CmdIds.SyncRogueCommonDialogueDataScNotify, notify));
    }

    private List<RogueDialogueData> LiveDialogues() =>
        RogueInstance?.Dialogues.Values.Where(d => !d.Closed).OrderBy(d => d.Handle).ToList() ?? [];

    /// <summary>The client's dialogue pull (also the reconnect path); null without a run.</summary>
    public GetRogueCommonDialogueDataScRsp? GetDialogueData()
    {
        if (RogueInstance == null) return null;
        var rsp = new GetRogueCommonDialogueDataScRsp();
        rsp.DialogueList.AddRange(LiveDialogues().Select(BuildDialogueRow));
        return rsp;
    }

    /// <summary>
    ///     Answers a pick on the run's talk; null when the handle is not the run's. The option pays its effect once (a
    ///     re-sent pick pays nothing): fragments are reported before the response, a blessing draft is announced and
    ///     holds back the option's finish notify until it is answered (official 4.5: pick, draft, answer, finish).
    /// </summary>
    public async ValueTask<SelectRogueCommonDialogueOptionScRsp?> SelectDialogueOption(int handle, int optionId)
    {
        if (RogueInstance is not { Status: RogueStatus.Doing } inst ||
            !inst.Dialogues.TryGetValue(handle, out var dialogue) || dialogue.Closed) return null;

        var rsp = new SelectRogueCommonDialogueOptionScRsp { AIAFEKGBFIF = (uint)handle, HAFOKMHCGFM = (uint)optionId };
        if (!dialogue.Options.Contains(optionId))
        {
            rsp.Retcode = (uint)Retcode.RetFail;
            return rsp;
        }

        var repeat = dialogue.Selected.Contains(optionId);
        if (!repeat) dialogue.Selected.Add(optionId);
        var (closes, events) = ResolvePick(dialogue.NpcId, optionId);

        var effect = repeat ? null : GameData.RogueDialogueEffectConfig.Find(dialogue.NpcId, optionId);
        var fragments = effect?.Fragments ?? 0;
        var draftGroup = effect?.BuffGroup ?? 0;
        var draft = draftGroup > 0 ? RogueBuffSelectMenu.Create(inst, GetBuffPool(draftGroup)) : null;
        if (draft is { HasSelection: false }) draft = null;

        inst.Coin += fragments;
        if (draft != null)
        {
            inst.ActionSeq++;
            inst.BuffSelect = draft;
            dialogue.PendingFinishOption = optionId;
            dialogue.PendingGroup = draftGroup;
            dialogue.PendingOffer = draft.SelectBuffs.Select(buff => buff.GetId()).ToList();
        }
        else if (closes && dialogue.PendingFinishOption == 0)
        {
            // A pick re-sent while its draft is unanswered must not close the talk under the draft: the option's
            // finish notify is still owed (TakeDeferredDialogueFinish).
            _closingDialogueHandle = handle;
        }

        SaveRun();

        if (dialogue.PendingFinishOption == 0) await SendOptionFinish(dialogue, optionId);
        if (fragments > 0)
        {
            await Player.SendPacket(new PacketSyncRogueCommonActionResultScNotify(
                RogueActionResultRows.MoneyCharge(NHHMEPILLHL.Nclbfaallfj, inst.ActionSeq, fragments, 1),
                DICEONDKEFF.Icfoncmfnlg));
            await Player.SendPacket(new PacketSyncRogueVirtualItemInfoScNotify(inst.Coin));
        }

        if (draft != null)
            await Player.SendPacket(new PacketSyncRogueCommonPendingActionScNotify(BuildPendingAction(inst)));

        rsp.ONOLNGOHEMF = BuildDialogueRow(dialogue);
        rsp.CGINGCNCPCO.AddRange(events.Select(id => (uint)id));
        return rsp;
    }

    // Whether a pick ends the talk, and the dialogue events to report, from the event's Act graph. Without a graph the
    // pick ends the talk with nothing to report.
    private static (bool Closes, List<int> Events) ResolvePick(int npcId, int optionId)
    {
        var graph = RogueTournEventDialogue.For(npcId);
        if (graph == null || !graph.OptionIds.Contains(optionId) || graph.IsFinishingOption(optionId)) return (true, []);

        var events = graph.EventsForOption(optionId);
        return (events.Count > 0 && graph.ClosesOnReport(optionId), events);
    }

    /// <summary>Closes the talk a closing pick ended, after that pick's response.</summary>
    public async ValueTask FlushDialoguePick()
    {
        var handle = _closingDialogueHandle;
        _closingDialogueHandle = 0;
        if (handle != 0) await CloseDialogue(handle);
    }

    /// <summary>
    ///     The client's "talk over" (FinishRogueCommonDialogueCsReq, no response). While the blessing draft an option
    ///     opened is unanswered the talk cannot close; the draft is announced again so it can be answered.
    /// </summary>
    public async ValueTask FinishDialogue(int handle)
    {
        if (RogueInstance is not { Status: RogueStatus.Doing } inst || handle == 0) return;
        if (inst.Dialogues.GetValueOrDefault(handle) is { Closed: false, PendingFinishOption: not 0 } &&
            inst.BuffSelect?.HasSelection == true)
        {
            await Player.SendPacket(new PacketSyncRogueCommonPendingActionScNotify(BuildPendingAction(inst)));
            return;
        }

        await CloseDialogue(handle);
    }

    /// <summary>
    ///     After a draft an option opened is answered: the option's held-back finish notify, then the talk closes when
    ///     that option ended it. The caller's exit opening then finishes the room.
    /// </summary>
    private async ValueTask TakeDeferredDialogueFinish(RogueInstance inst)
    {
        foreach (var dialogue in inst.Dialogues.Values.Where(d => !d.Closed && d.PendingFinishOption != 0).ToList())
        {
            var optionId = dialogue.PendingFinishOption;
            dialogue.PendingFinishOption = 0;
            dialogue.PendingGroup = 0;
            dialogue.PendingOffer = [];
            await SendOptionFinish(dialogue, optionId);
            if (!ResolvePick(dialogue.NpcId, optionId).Closes) continue;

            dialogue.Closed = true;
            await SendDialogueSet();
        }

        SaveRun();
    }

    // Closes a talk: the clear, then the room's exit, Finish and next-room Unlock. Idempotent for a closed talk, so the
    // client's own finish request after a server-side close changes nothing.
    private async ValueTask CloseDialogue(int handle)
    {
        if (RogueInstance is not { } inst || !inst.Dialogues.TryGetValue(handle, out var dialogue)) return;
        if (!dialogue.Closed)
        {
            dialogue.Closed = true;
            SaveRun();
            await SendDialogueSet();
        }

        if (inst.GetCurrentRoom()?.SiteId != dialogue.SiteId || inst.BuffSelect?.HasSelection == true) return;
        if (!await OpenRoomExit()) await FinishCurrentRoom(inst);
    }

    private async ValueTask SendOptionFinish(RogueDialogueData dialogue, int optionId)
    {
        await Player.SendPacket(BasePacket.Create(CmdIds.SyncRogueCommonDialogueOptionFinishScNotify,
            new SyncRogueCommonDialogueOptionFinishScNotify
            {
                AIAFEKGBFIF = (uint)dialogue.Handle,
                HAFOKMHCGFM = (uint)optionId,
                FJPGAOHGEBH = BuildDialogueOption(optionId, false)
            }));
    }

    // A relog drops an unanswered draft, but a draft an option opened is the player's earned blessing and holds its
    // talk open: deal the same cards again, or release the talk when they no longer resolve.
    private static void RestoreDialogueDraft(RogueInstance inst)
    {
        if (inst.BuffSelect != null) return;
        foreach (var dialogue in inst.Dialogues.Values.Where(d => !d.Closed && d.PendingFinishOption != 0))
        {
            var offer = dialogue.PendingOffer.Select(id => GameData.RogueBuffData.GetValueOrDefault(id))
                .OfType<BaseRogueBuffExcel>().ToList();
            if (offer.Count == 0)
            {
                dialogue.PendingFinishOption = 0;
                dialogue.PendingGroup = 0;
                dialogue.PendingOffer = [];
                continue;
            }

            inst.BuffSelect = RogueBuffSelectMenu.Restore(GetBuffPool(dialogue.PendingGroup), offer);
            return;
        }
    }

    private static HFEOGNAMHCG BuildDialogueRow(RogueDialogueData dialogue)
    {
        var row = new HFEOGNAMHCG
        {
            AIAFEKGBFIF = (uint)dialogue.Handle,
            // The NPC arm is the only owner a classic Occurrence has.
            Owner = new LEHBIBDGEHG { MFAEGBJBJBE = new HEOBLDJAOPE { OEANPIHCHHF = (uint)dialogue.NpcId } }
        };
        foreach (var optionId in dialogue.Options)
            row.KFNMIEMBJGD.Add(BuildDialogueOption(optionId, dialogue.Selected.Contains(optionId)));
        return row;
    }

    // Every official option row carries an empty param and is reported valid.
    private static KDODFHMDAJI BuildDialogueOption(int optionId, bool selected) => new()
    {
        Param = new KNMNEJCMHGE(),
        HAFOKMHCGFM = (uint)optionId,
        IsValid = true,
        IsSelected = selected
    };
}
