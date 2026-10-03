using March7thHoney.Data;
using March7thHoney.Database.RogueTourn;
using March7thHoney.Enums.TournRogue;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;
using March7thHoney.Util;
using MemoryPack;

namespace March7thHoney.GameServer.Game.RogueTourn;

public enum RogueTournActionResultKind
{
    Style,
    Formula,
    Buff,
    BuffTypes,
    Gift,
    Miracle,
    Scene,
    StyleExp,
    StyleExpGain
}

/// <summary>One SyncRogueCommonActionResultScNotify row an answered action, a room entry or a scene change produced.</summary>
public sealed class RogueTournActionResult
{
    public RogueTournActionResultKind Kind { get; init; }
    public int Value { get; init; }

    // Buff level, or the new style level for StyleExp.
    public int Level { get; init; }

    // Queue position of the action the row resolves; 0 leaves it unbound (gift entry and scene rows never carry one).
    public int QueuePos { get; set; }

    // Gift: the run-unique entry id the pick created (1, 2, 3 in pick order).
    public int EntryId { get; init; }

    // StyleExp: exp and level before the grant.
    public int PrevExp { get; init; }
    public int PrevLevel { get; init; }

    // BuffTypes: RogueBuffType → blessings of that type held.
    public Dictionary<int, int>? Types { get; init; }

    // Scene: the {scene, lineup} pair of the room just entered.
    public LHKAOPAPJEK? Scene { get; init; }
}

/// <summary>
///     RogueTourn wire builders. Shapes follow the official 4.5/4.6 captures: arms the client dereferences
///     unconditionally are always emitted, even when empty.
/// </summary>
public static class RogueTournProtoBuilder
{
    // RogueSubModeEnum.TournRogue; the client routes the run's game object by it.
    public const uint RogueSubModeTourn = 301;

    // Main 3 / Sub 2 maps to the RogueTourn activity module the schedule ships.
    public const RogueTournModeEnum CurrentTournMode = RogueTournModeEnum.Tourn3;
    private const uint CurSubTournId = 2;
    public const uint CurMainTournId = 3;

    // The in-room Workbench of Creation id the capture reports.
    public const int DefaultWorkbenchId = 105;

    // RogueModifierSourceType_RogueModifierSourcePersonaStyle: every mid-run modifier of both 4.6 traces.
    private const int ModifierSourcePersonaStyle = 10;

    // NHHMEPILLHL sources the official uses per row family.
    private const int SourceStyle = 0;
    private const int SourceSelect = 1;
    private const int SourceBuffTypes = 12;
    private const int SourceRoomRegistration = 16;

    // DICEONDKEFF: blessing grants and the Path histogram are 2, everything else 1.
    private const int DisplayBuff = 2;
    private const int DisplayDefault = 1;

    // The entity every DU blessing is attached to: (6 << 20 | 1), a type the server never creates in a scene.
    private const uint BuffHolderEntityId = 6 << 20 | 1;

    // The live snapshot's deck zone carries only these three constants (tes1/tes2 idx 503).
    private const uint PersonaZoneA = 40;
    private const uint PersonaZoneB = 3;
    private const uint PersonaZoneC = 10;

    // Run-level flags the official ships on every DU snapshot, in this order; only FruitBuffCount carries a value.
    private static readonly (string Key, double Value)[] GlobalBlackboard =
    [
        ("BestWish", 0), ("RogueNpcExtraOption", 0), ("RemoveCardEnable", 0), ("FruitBuffCount", 1),
        ("LeverageBondConsumeCoin", 0)
    ];

    #region Meta

    public static HMGGOEHMIJO BuildSeason()
    {
        return new HMGGOEHMIJO { HHAEEEOADOJ = CurSubTournId, FABMHMFNMDB = CurMainTournId };
    }

    public static KIKEAJJLAMH BuildQueryInfo(RogueTournData data)
    {
        var info = new KIKEAJJLAMH
        {
            DPABCOHLPLH = BuildPermanentTalentInfo(data),
            HANICMMHJGI = BuildSeasonTalentInfo(data),
            DMFPCFCHHKE = BuildExpInfo(data),
            BFNHJIBDMOA = BuildWeekChallenge(data),
            // The client NREs on any absent singular arm of this message.
            HHFAAFOBKME = BuildSeason(),
            DLDCFHHFMID = new JGMDJIENFFH { JBKHIFHPGHM = 0, GNIFODGCPAA = 1 },
            HAMKPLCBEBG = BuildHandbook(data.Handbook)
        };

        foreach (var areaId in RogueTournResources.AreasForSeason(CurrentTournMode))
        {
            if (!GameData.RogueTournAreaData.TryGetValue(areaId, out var area)) continue;
            data.AreaProgress.TryGetValue(areaId, out var progress);
            info.DANNIDIPFAL.Add(new DLEKOLHCNIE
            {
                AreaId = (uint)areaId,
                // Every area is reported unlocked, as the official capture does.
                IsUnlock = true,
                IsTakenReward = progress?.IsTakenReward ?? false,
                // First-passed gates the season's other content; Guide areas are reported passed.
                FJKHIFDJMDB = (progress?.IsFirstPassed ?? false) ||
                              area.AreaGroupID == RogueTournAreaGroupIDEnum.Guide
            });
        }

        // The saved runs the settle wrote, built by the same row builder GetAllArchive and ConfirmSettle use.
        foreach (var entry in data.Archives) info.HLJOMIMOANL.Add(BuildArchive(entry));
        return info;
    }

    // Every arm is a list of DISCOVERED ids; JKKIABLFCPE's meaning is unidentified and stays 0.
    public static HMOECPBNAOK BuildHandbook(RogueTournHandbookData? handbook)
    {
        var info = new HMOECPBNAOK { JKKIABLFCPE = 0 };
        if (handbook == null) return info;
        info.DPEBCGBEJIG.AddRange(handbook.Buffs.Select(id => (uint)id));
        info.IFFMIDABNEE.AddRange(handbook.Formulas.Select(id => (uint)id));
        info.MMPPHJOONMI.AddRange(handbook.Miracles.Select(id => (uint)id));
        info.PINNDKIJHFG.AddRange(handbook.Events.Select(id => (uint)id));
        info.DMFAGJOOGJM.AddRange(handbook.Workbenches.Select(id => (uint)id));
        return info;
    }

    /// <summary>
    ///     The Inspiration Circuit is one tree rooted at 100: a node is Enable when owned, Unlock (buyable) when it
    ///     has no parent or an owned parent, Lock otherwise. Reporting every node Unlock re-fires the "updated" dialog
    ///     and the red dot on every entrance-page open.
    /// </summary>
    public static MDLPIFCHFPJ BuildPermanentTalentInfo(RogueTournData data)
    {
        Dictionary<int, List<int>> parents = [];
        foreach (var talentId in RogueTournResources.PermanentTalentsSorted())
        {
            if (!GameData.RogueTournPermanentTalentData.TryGetValue(talentId, out var node)) continue;
            foreach (var next in node.NextTalentIDList)
            {
                if (!parents.TryGetValue(next, out var list)) parents[next] = list = [];
                list.Add(talentId);
            }
        }

        var talents = new FDBDLBAPDBE();
        foreach (var talentId in RogueTournResources.PermanentTalentsSorted())
        {
            var status = RogueTalentStatus.Lock;
            if (data.PermanentTalents.Contains(talentId))
                status = RogueTalentStatus.Enable;
            else if (!parents.TryGetValue(talentId, out var owners) || owners.Count == 0 ||
                     owners.Any(data.PermanentTalents.Contains))
                status = RogueTalentStatus.Unlock;

            talents.BLHJIPBHHFO.Add(new OKPIPFKONMG { CKHCLKOALKE = (uint)talentId, Status = status });
        }

        return new MDLPIFCHFPJ { JCOBJAMPOAL = talents };
    }

    // Head of each chain is Unlock, dependents Lock, persisted statuses win.
    public static KGPIEEKIEME BuildSeasonTalentInfo(RogueTournData data)
    {
        var talents = new FDBDLBAPDBE();
        foreach (var row in RogueTournResources.PersonaTalentsSorted())
        {
            var status = row.PreTalentID == 0 ? RogueTalentStatus.Unlock : RogueTalentStatus.Lock;
            if (data.SeasonTalents.TryGetValue(row.ID, out var saved)) status = (RogueTalentStatus)saved;
            talents.BLHJIPBHHFO.Add(new OKPIPFKONMG { CKHCLKOALKE = (uint)row.ID, Status = status });
        }

        return new KGPIEEKIEME { JCOBJAMPOAL = talents };
    }

    public static JGJEICAELEB BuildExpInfo(RogueTournData data)
    {
        var info = new JGJEICAELEB { Exp = (uint)data.Exp };
        info.FKDNANDJLGK.AddRange(data.TakenLevelRewards.Select(level => (uint)level));
        return info;
    }

    /// <summary>
    ///     The weekly panel needs a challenge id and a future end time, or it shows 00m 00s and refuses to open. The
    ///     id is a weekly rotation (4.5 reported 105, 4.6 reports 108), so it comes from the server config.
    /// </summary>
    public static CJOCDGGCPCN BuildWeekChallenge(RogueTournData data)
    {
        var state = data.WeekChallenge;
        var endTime = state.EndTime;
        if (endTime <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) endTime = NextWeeklyReset();
        var configured = ConfigManager.Config.ServerOption.RogueTourn.WeekChallengeId;

        return new CJOCDGGCPCN
        {
            EndTime = endTime,
            KBNNOPHGGIN = state.HasGained,
            JDPFPDECPEH = (uint)state.ExtraExp,
            JNBIIJCAKKB = (uint)(state.Id != 0 ? state.Id : configured)
        };
    }

    // Monday 04:00 server time (UTC+8).
    private static long NextWeeklyReset()
    {
        var offset = TimeSpan.FromHours(8);
        var now = DateTimeOffset.UtcNow.ToOffset(offset);
        var daysAhead = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        var day = now.Date.AddDays(daysAhead);
        var reset = new DateTimeOffset(day.Year, day.Month, day.Day, 4, 0, 0, offset);
        if (reset <= now) reset = reset.AddDays(7);
        return reset.ToUnixTimeSeconds();
    }

    /// <summary>One archive row: slot, name, times, squad, the season pair and the stored run snapshot (full deck).</summary>
    public static MJMGOBMOPDL BuildArchive(RogueTournArchiveData entry)
    {
        var row = new MJMGOBMOPDL
        {
            NPHAJEKCBKF = (uint)entry.Slot,
            Name = entry.Name ?? "",
            Time = entry.Time,
            EndTime = entry.EndTime,
            HHFAAFOBKME = BuildSeason()
        };
        foreach (var avatarId in entry.Avatars) row.AvatarList.Add(new DKFJLHHDFKO { Id = (uint)avatarId });

        if (!string.IsNullOrEmpty(entry.Data))
            try
            {
                var run = MemoryPackSerializer.Deserialize<RogueTournRunData>(Convert.FromBase64String(entry.Data));
                if (run != null)
                {
                    run.EnsureCollections();
                    row.PHGJIJELKDB = BuildRunInfo(run, true);
                }
            }
            catch
            {
                // An archive written by an incompatible build keeps its header row only.
            }

        return row;
    }

    // The client dereferences all five arms. The settle variant flags the revive and threshold arms.
    public static void FillMiscRealTimeData(RogueTournGetMiscRealTimeDataScRsp rsp)
    {
        rsp.AOMHPLHONJA = new BJLNEBBMMOE();
        rsp.JJLFKNPEPFP = new GKHFFKMFMEN { DOFNEAPHMKH = false };
        rsp.AOGOHOCFFNE = new DCDMPLJNBJC { DOFNEAPHMKH = false };
        rsp.CPOLAHJPGFE = new JGMDJIENFFH { GNIFODGCPAA = 1 };
        rsp.BNLLEJPCIGD = new FBBOKKLGKGL { DIFIEHPCDGO = 18000 };
    }

    /// <summary>
    ///     FAICNBKIELL, the settle page: the live-counter arms (revive/threshold flagged as on both 4.6 settles), the
    ///     run score and season exp the settle grants, and the finished run's snapshot with its full deck.
    /// </summary>
    public static FAICNBKIELL BuildSettlePayload(RogueTournRunData run, int score, int gain, int total)
    {
        var payload = new FAICNBKIELL
        {
            AOMHPLHONJA = new BJLNEBBMMOE { DOFNEAPHMKH = true },
            JJLFKNPEPFP = new GKHFFKMFMEN(),
            AOGOHOCFFNE = new DCDMPLJNBJC(),
            CPOLAHJPGFE = new JGMDJIENFFH { GNIFODGCPAA = 1 },
            BNLLEJPCIGD = new FBBOKKLGKGL { DIFIEHPCDGO = 18000, DOFNEAPHMKH = true },
            PHGJIJELKDB = BuildRunInfo(run, true)
        };
        if (score != 0 || gain != 0)
        {
            payload.JJLFKNPEPFP.NIOFIOOGKLD = (uint)score;
            payload.AOGOHOCFFNE.GEPHKCPLAJF = (uint)total;
            payload.AOGOHOCFFNE.KABCGHCPEBG = (uint)gain;
        }

        return payload;
    }

    #endregion

    #region Run

    public static PFLOIHLLMEE BuildRunInfo(RogueTournRunData run, bool fullDeck = false)
    {
        var basic = new CBOLONJDHHL
        {
            TransNo = run.TransNo,
            LJOBBEFACPG = (uint)run.AreaId,
            DLOJFEEHLIJ = RogueSubModeTourn,
            // Present even when idle: the official settle capture ships {queuePosition:0, detail:null}.
            FMIGOKIDKJL = BuildPendingAction(run),
            // The game data Sync NREs without the blackboard; an empty flag set reads as "every feature off".
            Blackboard = new GMIMDEJMFCO { GlobalBlackboard = new MGGBPKANFOA() },
            Modifier = new NCHGLFJCGIJ()
        };
        foreach (var (key, value) in GlobalBlackboard)
            basic.Blackboard.GlobalBlackboard.GFKAFIFMHNF.Add(new AKMCODLAEKI { Key = key, Value = value });
        // The snapshot lists every modifier; only the two start rows are source-less and no row carries the tier
        // arm here (it rides the notifies).
        foreach (var id in run.Modifiers)
        {
            var row = new BMCNDLNONMO { FCHLCANJIGL = (ulong)id, OOPCPMANCBO = new KIJEFIIBBGG() };
            if (!run.ModifierIsStart(id)) row.SourceType = (RogueModifierSourceType)ModifierSourcePersonaStyle;
            basic.Modifier.HDLGLFGKONC.Add(row);
        }

        var keywords = new OHHKEOGOMCA();
        foreach (var keywordId in RogueTournResources.KeywordsForSeason((RogueTournModeEnum)run.TournMode))
            keywords.PACLGPIDJGL[(uint)keywordId] = false;

        var itemValue = new FKEDPCMLIFE();
        foreach (var (key, value) in run.ItemValue) itemValue.HJANIOKJALB[(uint)key] = (uint)value;

        var lineup = new JEIKFFFGONB
        {
            LNIEFKBMIPD = new ItemCostData
            {
                ItemList =
                {
                    new ItemCost
                    {
                        PileItem = new PileItem
                        {
                            ItemId = RogueTournStates.ItemValueCosmicFragment,
                            ItemNum = (uint)run.Coin()
                        }
                    }
                }
            }
        };
        foreach (var avatarId in run.BaseAvatarIds)
            lineup.BHBPKKKNABO.Add(new DINGDGLKHIP { AvatarId = (uint)avatarId, HOKEFMONOPG = BEAIKEEANDP.Nkdecdgecfl });

        var buffs = new APNCCNFJBKO();
        foreach (var (buffId, level) in run.Buffs.OrderBy(entry => entry.Key))
            buffs.BuffList.Add(new KJAMNOGFAMJ { BuffId = (uint)buffId, HAGDKKBAAKK = (uint)level });
        var miracles = new GMIJHCONFIH();
        foreach (var miracleId in run.Miracles) miracles.MAJGMMCOCIG.Add(new KOFMGCNIMIN { LJCEOGPOGBE = (uint)miracleId });
        var formulas = new DJODDECFHMC { ENFIKCCMHAI = new HMAAICNBKCI() };
        foreach (var formulaId in run.Formulas) formulas.KJFEFFGPNFF.Add(BuildFormula(formulaId));

        var game = new CGNGOLJKBMP
        {
            LNCOKKIPAEP = keywords,
            Level = BuildLevelInfo(run, fullDeck),
            ItemValue = itemValue,
            Buff = new EGLAPJGFKPN { JPKACKNNHCA = buffs },
            Miracle = new NHLBNBBDHMI { HAJJBOFCFOO = miracles },
            Formula = formulas,
            Lineup = lineup,
            // The run restores the player's food buffs.
            BCNMBONFKPK = new HIAHPMEAGFC { JOOJIJFCHMK = true },
            HHMLHKLKNDH = new HGGPKPPBGFP { OHPELHFNGCI = DefaultWorkbenchId },
            BKIPDOFONLD = new CGBKKJENEOF()
        };

        // The exit page's settle button indexes the score arm's style table directly; it carries the run's style
        // and its gifts in pick order (the official's 10 rows at level5 settle).
        var style = new AJCFOLGANFC();
        if (run.PersonaStyle != 0)
        {
            style.EDOGNFCCGPF = (uint)run.PersonaStyle;
            style.PGGDEHKKINP = (uint)run.PersonaStyle;
        }

        for (var i = 0; i < run.Gifts.Count; i++)
            style.GJBOLJEJIEE.Add(new JHECCCANFGP { GNCFHIKODKE = (uint)run.Gifts[i], UniqueId = (uint)(i + 1) });

        return new PFLOIHLLMEE
        {
            Basic = basic,
            AMEABNLMMGB = game,
            OPDEIFOCJEF = new CABKLLAKHKO { GMEPGCODKKE = style }
        };
    }

    public static INHBMHCJJCN BuildPendingAction(RogueTournRunData run)
    {
        var action = new INHBMHCJJCN();
        var pending = run.Pending;
        if (pending == null) return action;

        action.QueuePosition = (uint)pending.QueuePos;
        var detail = new DCFPEIMAMCN();
        switch (pending.Kind)
        {
            case RogueTournPendingKind.Style:
            {
                var style = new PGMIABHEJNB { EAGILFBCKJH = true };
                style.ICCMIEHKDAO.AddRange(pending.Options.Select(id => (uint)id));
                detail.HDAHOBBJDIF = style;
                break;
            }
            case RogueTournPendingKind.Formula:
            {
                var formula = new OFMCKLEMOMP { CanRoll = pending.CanRoll };
                formula.ONECEAIJLAM.AddRange(pending.Options.Select(id => (uint)id));
                detail.FNEOOMHHGGB = formula;
                break;
            }
            case RogueTournPendingKind.Gift:
            {
                // DAEMEBCKHIK is 3 on every 4.6 gift offer: the window size.
                var gift = new OHJJMCMJEHL { DAEMEBCKHIK = (uint)pending.Options.Count };
                gift.BLAHNLDHINC.AddRange(pending.Options.Select(id => (uint)id));
                detail.KBEBGLAAEGM = gift;
                break;
            }
            case RogueTournPendingKind.Miracle:
            {
                var miracle = new AMKALFJHFJP { LGIPILAKIKB = (uint)pending.DropGroup };
                miracle.BPDDBKGDMDD.AddRange(pending.Options.Select(id => (uint)id));
                detail.AMIBPAGBMFP = miracle;
                break;
            }
            case RogueTournPendingKind.Buff:
            {
                // Measured 1 in every draft; FIMMFJCKCNK is unidentified and ships its modal value.
                var buff = new GGGBAIAPDLO
                {
                    CanRoll = pending.CanRoll, NKGGAOJLCMD = 1, EIALMEADLGF = 1, FIMMFJCKCNK = 1
                };
                foreach (var offer in pending.BuffOffers)
                    buff.LEOFNINDJDB.Add(new KJAMNOGFAMJ { BuffId = (uint)offer.BuffId, HAGDKKBAAKK = (uint)offer.Level });
                detail.PCHGEGFBKLF = buff;
                break;
            }
            case RogueTournPendingKind.Exit:
            {
                // Two shapes: a card offer (FNIPHPIEBNB + per-card level) or a preset confirm (OIMCPHNONPF). Both carry
                // FAHAMJFPMMI = 2 and the run's deck zone.
                var exit = new JDFNPICGCGI { FAHAMJFPMMI = 2, JBEBEKECKJN = BuildDeckZone(run) };
                if (pending.ExitPreset != 0) exit.OIMCPHNONPF.Add((uint)pending.ExitPreset);
                foreach (var uid in pending.Options)
                {
                    exit.FNIPHPIEBNB.Add((uint)uid);
                    var level = pending.OptionLevels.GetValueOrDefault(uid);
                    exit.KEGHPOHGPFF[(uint)uid] = (uint)(level == 0 ? 1 : level);
                }

                detail.IFPBFMOAJOP = exit;
                break;
            }
        }

        action.Detail = detail;
        return action;
    }

    // The two complementary id lists of the run deck (remaining / consumed), the object both the chooser and 6079 carry.
    public static ODHJEDHKHOD BuildDeckZone(RogueTournRunData run)
    {
        var (remaining, consumed, _) = run.DeckView();
        var zone = new ODHJEDHKHOD();
        zone.BIMFEAHDMIK.AddRange(remaining.Select(id => (uint)id));
        zone.NMMCOBBKKHA.AddRange(consumed.Select(id => (uint)id));
        return zone;
    }

    public static RogueTournPersonaRoomCardZoneUpdateScNotify? BuildRoomCardZoneNotify(RogueTournRunData run)
    {
        var zone = BuildDeckZone(run);
        if (zone.BIMFEAHDMIK.Count + zone.NMMCOBBKKHA.Count == 0) return null;
        return new RogueTournPersonaRoomCardZoneUpdateScNotify { NHNBJLLDOHO = zone };
    }

    public static ONPOFJPEBMJ BuildLevelInfo(RogueTournRunData run, bool fullDeck = false)
    {
        var level = new ONPOFJPEBMJ
        {
            Status = (RogueTournLevelStatus)run.LevelStatus,
            Reason = (FKNGOGLNKIG)run.LevelReason,
            BJHMFOAGMBF = (uint)run.CurLayerIndex
        };

        // The Persona deck arm: RogueTournPersonaGameData.Sync reads it unconditionally. The live snapshot ships the
        // constants alone; archived runs and the settle page add both id lists and the card rows.
        var zone = new LPADDGPLIKK { MKJDMNAEAED = PersonaZoneA, AMOABLICEDP = PersonaZoneB, PJOAKLHHCNI = PersonaZoneC };
        if (fullDeck)
        {
            var (remaining, consumed, cards) = run.DeckView();
            zone.BIMFEAHDMIK.AddRange(remaining.Select(id => (uint)id));
            zone.NMMCOBBKKHA.AddRange(consumed.Select(id => (uint)id));
            foreach (var card in cards)
                zone.LONFKFCFDND.Add(new BDJLHJDPEEK
                {
                    UniqueId = (uint)card.UniqueId,
                    CompositionType = (uint)RogueTournResources.PersonaCompositionType(card.CompositionType),
                    Level = (uint)card.Level
                });
        }

        level.FIADMEPDAOG = new CFOIFHEBCJC { CCFGCCMEFCI = zone };
        foreach (var layer in run.Layers) level.NNFFPJOLDHK.Add(BuildLayerInfo(layer));
        return level;
    }

    // JOJCPKNIJOH is the layer's CURRENT room index, not its room count.
    public static IJNFOHJGPBC BuildLayerInfo(RogueTournLayerData layer, IEnumerable<RogueTournRoomData>? rooms = null)
    {
        var info = new IJNFOHJGPBC
        {
            LayerIndex = (uint)layer.LayerIndex,
            MDAIAMFJNHM = (uint)layer.LayerId,
            JOJCPKNIJOH = (uint)layer.CurRoomIndex,
            Status = (IDCBNMFDICO)layer.Status
        };
        foreach (var room in rooms ?? layer.Rooms) info.JEHEILKKDEF.Add(BuildRoomInfo(room));
        return info;
    }

    /// <summary>
    ///     Unreached rooms ship a blank identity (an identity without a resolvable composition NREs the client's Sync)
    ///     but the composition arm itself is always present: it is a oneof, and an unset one is a null the client walks.
    /// </summary>
    public static ODNBCBEINNC BuildRoomInfo(RogueTournRoomData room)
    {
        var info = new ODNBCBEINNC { EEPIDJJJMAH = (uint)room.RoomIndex, FIADMEPDAOG = new ELHLCFBPAMO() };
        if (room.Status == RogueTournStates.RoomNone) return info;

        info.RoomId = (uint)room.RoomId;
        info.LHLKJIDFLIN = (uint)room.RoomType;
        info.Status = (IIAOGEIADGA)room.Status;
        info.FIADMEPDAOG.Level = (uint)room.Level;
        info.FIADMEPDAOG.CompositionType = (uint)RogueTournResources.PersonaCompositionType(room.CompositionType);
        return info;
    }

    /// <summary>
    ///     The official level push is a per-room DELTA: the current layer carrying only the room whose status changed,
    ///     plus the run status. withRoom=false is the run-status-only shape of the finish and settle pushes.
    /// </summary>
    public static RogueTournLevelInfoUpdateScNotify BuildLevelNotify(RogueTournRunData run, bool withRoom = true)
    {
        var notify = new RogueTournLevelInfoUpdateScNotify
        {
            Status = (RogueTournLevelStatus)run.LevelStatus,
            Reason = (FKNGOGLNKIG)run.LevelReason,
            BJHMFOAGMBF = (uint)run.CurLayerIndex
        };
        var layer = run.CurLayer();
        var room = run.CurRoom();
        if (withRoom && layer != null)
            notify.NNFFPJOLDHK.Add(BuildLayerInfo(layer, room == null ? [] : [room]));
        return notify;
    }

    public static SyncRogueCommonPendingActionScNotify BuildPendingNotify(RogueTournRunData run)
    {
        return new SyncRogueCommonPendingActionScNotify
        {
            DLOJFEEHLIJ = RogueSubModeTourn,
            Action = BuildPendingAction(run)
        };
    }

    public static CommonRogueVirtualItemInfoScNotify BuildVirtualItemNotify(RogueTournRunData run)
    {
        var notify = new CommonRogueVirtualItemInfoScNotify();
        notify.DMCINCJLMND.Add(new LKCDLJCKCJI
        {
            ItemType = RogueTournStates.ItemValueCosmicFragment,
            KMNILIKJFHD = (uint)run.Coin()
        });
        return notify;
    }

    /// <summary>
    ///     The ONE result notify that registers every room of the run (unique id → card) with the 4.6 client, which
    ///     resolves the room chooser's ids against it and never sends RogueTournEnterRoomCsReq itself.
    /// </summary>
    public static SyncRogueCommonActionResultScNotify? BuildRoomRegistrationNotify(RogueTournRunData run)
    {
        var notify = new SyncRogueCommonActionResultScNotify
        {
            DLOJFEEHLIJ = RogueSubModeTourn,
            DisplayType = (DICEONDKEFF)DisplayDefault
        };
        foreach (var room in run.Layers.SelectMany(layer => layer.Rooms).Where(room => room.UniqueId != 0))
            notify.CLGGKLNPLCK.Add(new MAOKMPOELCO
            {
                Source = (NHHMEPILLHL)SourceRoomRegistration,
                Detail = new PKHKEEKIODC
                {
                    CIOLGFNLEBB = new NKOBPBBONDJ
                    {
                        KDFKDNLNDHN = new BDJLHJDPEEK
                        {
                            UniqueId = (uint)room.UniqueId,
                            CompositionType = (uint)RogueTournResources.PersonaCompositionType(room.CompositionType),
                            Level = (uint)room.Level
                        },
                        BJDEBHMKJBC = true
                    }
                }
            });
        return notify.CLGGKLNPLCK.Count == 0 ? null : notify;
    }

    // One notify per row, as the official server emits them.
    public static SyncRogueCommonActionResultScNotify? BuildActionResult(RogueTournActionResult result)
    {
        var row = new MAOKMPOELCO();
        var detail = new PKHKEEKIODC();
        var display = DisplayDefault;
        switch (result.Kind)
        {
            case RogueTournActionResultKind.Style:
                row.Source = (NHHMEPILLHL)SourceStyle;
                detail.JKLJBCBFNPL = new JGCMGAMHDHM
                {
                    PGGDEHKKINP = (uint)result.Value,
                    HGKBJMGGPGL = (uint)result.Value
                };
                break;
            case RogueTournActionResultKind.Formula:
                row.Source = (NHHMEPILLHL)SourceSelect;
                detail.MDGNDLLEFJE = new JJEMDNEIMOJ { DKEENIMPLKD = BuildFormula(result.Value) };
                break;
            case RogueTournActionResultKind.Buff:
                row.Source = (NHHMEPILLHL)SourceSelect;
                display = DisplayBuff;
                detail.IFOOEPKGGAG = new KJAMNOGFAMJ { BuffId = (uint)result.Value, HAGDKKBAAKK = (uint)result.Level };
                break;
            case RogueTournActionResultKind.BuffTypes:
            {
                if (result.Types == null || result.Types.Count == 0) return null;
                row.Source = (NHHMEPILLHL)SourceBuffTypes;
                display = DisplayBuff;
                var value = new HMAAICNBKCI();
                foreach (var (buffType, count) in result.Types) value.DCLBHOHPGMB[(uint)buffType] = count;
                detail.JEBLGELGBHM = new CJAFLDKHDAF { Value = value };
                break;
            }
            case RogueTournActionResultKind.Gift:
                // The official gift-entry rows carry no source.
                detail.ALNEINEOJOJ = new HIMJNABCJMM
                {
                    JKFNAKBGIBF =
                    {
                        new EGMPACONNFP
                        {
                            UniqueId = (uint)result.EntryId, GNCFHIKODKE = (uint)result.Value, NPBLEKPLPFA = 1
                        }
                    }
                };
                break;
            case RogueTournActionResultKind.Miracle:
                row.Source = (NHHMEPILLHL)SourceSelect;
                detail.GPJJMPCMAAA = new IOMEMIEAPJO { Miracle = new KOFMGCNIMIN { LJCEOGPOGBE = (uint)result.Value } };
                break;
            case RogueTournActionResultKind.Scene:
                if (result.Scene == null) return null;
                detail.FLPDILMMCFK = new OEPIHJOHLKM { IDCMACCPCBH = result.Scene };
                break;
            case RogueTournActionResultKind.StyleExp:
                row.Source = (NHHMEPILLHL)SourceStyle;
                detail.EGPLOEAAFGL = new PPHLEBFJIGM
                {
                    OIIJCLJHPPE = (uint)result.PrevLevel,
                    AIIFGDGONIF = (uint)result.PrevExp,
                    StyleExp = (uint)result.Value,
                    StyleLevel = (uint)result.Level
                };
                break;
            case RogueTournActionResultKind.StyleExpGain:
                detail.MIDFLEHKPBF = new OECNJOAPCKO { FAGJACOFHGF = (uint)result.Value };
                break;
        }

        row.Detail = detail;
        if (result.QueuePos != 0) row.QueuePosition = (uint)result.QueuePos;
        var notify = new SyncRogueCommonActionResultScNotify
        {
            DLOJFEEHLIJ = RogueSubModeTourn,
            DisplayType = (DICEONDKEFF)display
        };
        notify.CLGGKLNPLCK.Add(row);
        return notify;
    }

    // The requirement list is MainBuffType×MainBuffNum then SubBuffType×SubBuffNum.
    private static LEJFIEPMMGG BuildFormula(int formulaId)
    {
        var formula = new LEJFIEPMMGG { KAHMGPGPOII = (uint)formulaId };
        if (!GameData.RogueTournFormulaData.TryGetValue(formulaId, out var excel)) return formula;
        AddFormulaRequirement(formula, excel.MainBuffTypeID, excel.MainBuffNum);
        AddFormulaRequirement(formula, excel.SubBuffTypeID, excel.SubBuffNum);
        return formula;
    }

    private static void AddFormulaRequirement(LEJFIEPMMGG formula, int buffTypeId, int count)
    {
        if (buffTypeId == 0 || count == 0) return;
        formula.IFJIHNBDNGN.Add(new CBDMGEGELFM { Key = (uint)buffTypeId, DPICNGBHFAC = count });
    }

    /// <summary>
    ///     The official attaches every granted blessing to the run's buff-holder entity, one notify per buff, before
    ///     reporting the grant as a result row; without it the client's run buff list stays empty.
    /// </summary>
    public static IEnumerable<SyncEntityBuffChangeListScNotify> BuildBuffGrantNotifies(
        IEnumerable<RogueTournActionResult> results)
    {
        foreach (var result in results.Where(result => result.Kind == RogueTournActionResultKind.Buff && result.Value != 0))
            yield return new SyncEntityBuffChangeListScNotify
            {
                EntityBuffChangeList =
                {
                    new EntityBuffChangeInfo
                    {
                        EntityId = BuffHolderEntityId,
                        BuffChangeInfo = new BuffInfo
                        {
                            BuffId = (uint)result.Value,
                            Level = (uint)result.Level,
                            Count = uint.MaxValue,
                            LifeTime = -1,
                            AddTimeMs = (ulong)Extensions.GetUnixMs()
                        }
                    }
                }
            };
    }

    /// <summary>
    ///     One modifier row: the opaque id, the always-present empty payload, PersonaStyle as source for every row but
    ///     the two start rows, and on the style row the tier arm (cap always, tier only once it stepped).
    /// </summary>
    public static BMCNDLNONMO BuildModifierRow(int id, bool personaSource, bool styleArm, int tier)
    {
        var row = new BMCNDLNONMO { FCHLCANJIGL = (ulong)id, OOPCPMANCBO = new KIJEFIIBBGG() };
        if (personaSource) row.SourceType = (RogueModifierSourceType)ModifierSourcePersonaStyle;
        if (styleArm)
        {
            row.GGJOKFEOEBK = new HFJKOCDOAEH { EKNHHGMJCPM = RogueTournRunData.StyleTierCap };
            if (tier != 0) row.GGJOKFEOEBK.OLPPACCCNGD = (uint)tier;
        }

        return row;
    }

    // The run's drafted blessings plus the player's avatar-global maze buffs, as infinite scene buffs.
    public static IEnumerable<BuffInfo> BuildSceneBuffs(RogueTournRunData run, PlayerInstance player)
    {
        HashSet<int> held = [];
        foreach (var (buffId, level) in run.Buffs.OrderBy(entry => entry.Key))
        {
            held.Add(buffId);
            yield return SceneBuff(buffId, level);
        }

        foreach (var buff in GameData.AvatarGlobalBuffConfigData.Values.OrderBy(buff => buff.AvatarID))
            if (player.AvatarManager?.GetFormalAvatar(buff.AvatarID) != null && held.Add(buff.MazeBuffID))
                yield return SceneBuff(buff.MazeBuffID, 1);
    }

    private static BuffInfo SceneBuff(int buffId, int level)
    {
        return new BuffInfo { BuffId = (uint)buffId, Level = (uint)Math.Max(level, 1), Count = uint.MaxValue, LifeTime = -1 };
    }

    // The per-game wrapper {scene, lineup} every RogueTourn ScRsp carries as IDCMACCPCBH.
    public static JGAMMPGMCNI BuildEnterInfo(PlayerInstance player)
    {
        var (scene, lineup) = BuildScenePair(player);
        // JNKLNLKHGBB ships present-but-empty, as the official enter infos do.
        return new JGAMMPGMCNI { Scene = scene, Lineup = lineup, JNKLNLKHGBB = new FHEGNOPNICH() };
    }

    // The same pair delivered inside the result stream when the player walks into a room (FLPDILMMCFK).
    public static LHKAOPAPJEK BuildSceneResultPair(PlayerInstance player)
    {
        var (scene, lineup) = BuildScenePair(player);
        return new LHKAOPAPJEK { Scene = scene, Lineup = lineup, JNKLNLKHGBB = new FHEGNOPNICH() };
    }

    private static (SceneInfo? Scene, LineupInfo? Lineup) BuildScenePair(PlayerInstance player)
    {
        SceneInfo? sceneInfo = null;
        var scene = player.SceneInstance;
        if (scene != null)
        {
            sceneInfo = scene.ToProto();
            scene.MarkClientSceneSnapshotSynced();
        }

        var lineupManager = player.LineupManager!;
        var lineup = lineupManager.GetCurLineup();
        if (lineup == null) return (sceneInfo, null);

        lineupManager.RecalculateExtraMpCount();
        var lineupProto = lineup.ToProto();
        var maxMp = (uint)lineupManager.GetMaxMp();
        lineupProto.MaxMp = maxMp;
        if (lineupProto.Mp > maxMp) lineupProto.Mp = maxMp;
        return (sceneInfo, lineupProto);
    }

    #endregion

    #region Dialogue

    public static HFEOGNAMHCG BuildDialogueRow(RogueTournRoomDialogueData dialogue)
    {
        var row = new HFEOGNAMHCG
        {
            AIAFEKGBFIF = (uint)dialogue.Handle,
            // The NPC arm is the only owner a DU Event room has.
            Owner = new LEHBIBDGEHG { MFAEGBJBJBE = new HEOBLDJAOPE { OEANPIHCHHF = (uint)dialogue.NpcId } }
        };
        foreach (var optionId in dialogue.Options)
            row.KFNMIEMBJGD.Add(BuildDialogueOption(optionId, dialogue.Selected.Contains(optionId)));
        return row;
    }

    // Every official option row carries an empty param and is reported valid; arg_id is never set.
    public static KDODFHMDAJI BuildDialogueOption(int optionId, bool selected)
    {
        return new KDODFHMDAJI
        {
            Param = new KNMNEJCMHGE(),
            HAFOKMHCGFM = (uint)optionId,
            IsValid = true,
            IsSelected = selected
        };
    }

    #endregion
}
