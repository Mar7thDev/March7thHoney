using March7thHoney.Data;
using March7thHoney.Database.RogueTourn;
using March7thHoney.Enums.TournRogue;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.RogueTourn;

public enum RogueTournActionResultKind
{
    Style,
    Formula
}

public readonly record struct RogueTournActionResult(RogueTournActionResultKind Kind, int Value);

/// <summary>
///     RogueTourn wire builders. Shapes follow the official 4.5 capture: arms the client dereferences
///     unconditionally are always emitted, even when empty.
/// </summary>
public static class RogueTournProtoBuilder
{
    // RogueSubModeEnum.TournRogue; the client routes the run's game object by it.
    public const uint RogueSubModeTourn = 301;

    // Main 3 / Sub 2 maps to the RogueTourn activity module the schedule ships.
    public const RogueTournModeEnum CurrentTournMode = RogueTournModeEnum.Tourn3;
    private const uint CurSubTournId = 2;
    private const uint CurMainTournId = 3;

    // A zero id / end time leaves the weekly panel unresolvable; 105 is the capture's row.
    private const uint DefaultWeekChallengeId = 105;

    // The in-room Workbench of Creation id the capture reports.
    private const uint DefaultWorkbenchId = 105;

    #region Meta

    public static KIKEAJJLAMH BuildQueryInfo(RogueTournData data)
    {
        var info = new KIKEAJJLAMH
        {
            DPABCOHLPLH = BuildPermanentTalentInfo(data),
            HANICMMHJGI = BuildSeasonTalentInfo(data),
            DMFPCFCHHKE = BuildExpInfo(data),
            BFNHJIBDMOA = BuildWeekChallenge(data),
            // The client NREs on any absent singular arm of this message.
            HHFAAFOBKME = new HMGGOEHMIJO { HHAEEEOADOJ = CurSubTournId, FABMHMFNMDB = CurMainTournId },
            DLDCFHHFMID = new JGMDJIENFFH { JBKHIFHPGHM = 0, GNIFODGCPAA = 1 },
            HAMKPLCBEBG = new HMOECPBNAOK { JKKIABLFCPE = 0 }
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
                // First-passed gates the season's other content; the settle flow that would set it is not
                // implemented, so Guide areas are reported passed (reported only, never persisted).
                FJKHIFDJMDB = (progress?.IsFirstPassed ?? false) ||
                              area.AreaGroupID == RogueTournAreaGroupIDEnum.Guide
            });
        }

        return info;
    }

    public static MDLPIFCHFPJ BuildPermanentTalentInfo(RogueTournData data)
    {
        var talents = new FDBDLBAPDBE();
        foreach (var talentId in RogueTournResources.PermanentTalentsSorted())
            talents.BLHJIPBHHFO.Add(new OKPIPFKONMG
            {
                CKHCLKOALKE = (uint)talentId,
                Status = data.PermanentTalents.Contains(talentId) ? RogueTalentStatus.Enable : RogueTalentStatus.Unlock
            });

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

    public static CJOCDGGCPCN BuildWeekChallenge(RogueTournData data)
    {
        var state = data.WeekChallenge;
        var endTime = state.EndTime;
        if (endTime <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) endTime = NextWeeklyReset();

        return new CJOCDGGCPCN
        {
            EndTime = endTime,
            KBNNOPHGGIN = state.HasGained,
            JDPFPDECPEH = (uint)state.ExtraExp,
            JNBIIJCAKKB = state.Id == 0 ? DefaultWeekChallengeId : (uint)state.Id
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

    public static MJMGOBMOPDL BuildArchive(RogueTournArchiveData entry)
    {
        var row = new MJMGOBMOPDL
        {
            NPHAJEKCBKF = (uint)entry.Slot,
            Name = entry.Name ?? "",
            Time = entry.Time,
            EndTime = entry.EndTime
        };
        foreach (var avatarId in entry.Avatars) row.AvatarList.Add(new DKFJLHHDFKO { Id = (uint)avatarId });
        return row;
    }

    // The client dereferences all five arms; the counters stay zero until those systems are modelled.
    public static void FillMiscRealTimeData(RogueTournGetMiscRealTimeDataScRsp rsp)
    {
        rsp.AOMHPLHONJA = new BJLNEBBMMOE();
        rsp.JJLFKNPEPFP = new GKHFFKMFMEN { DOFNEAPHMKH = false };
        rsp.AOGOHOCFFNE = new DCDMPLJNBJC { DOFNEAPHMKH = false };
        rsp.CPOLAHJPGFE = new JGMDJIENFFH { GNIFODGCPAA = 1 };
        rsp.BNLLEJPCIGD = new FBBOKKLGKGL { DIFIEHPCDGO = 18000 };
    }

    #endregion

    #region Run

    public static PFLOIHLLMEE BuildRunInfo(RogueTournRunData run)
    {
        var basic = new CBOLONJDHHL
        {
            TransNo = run.TransNo,
            LJOBBEFACPG = (uint)run.AreaId,
            DLOJFEEHLIJ = RogueSubModeTourn,
            // Present even when idle: the official settle capture ships {queuePosition:0, detail:null}.
            FMIGOKIDKJL = BuildPendingAction(run),
            // The game data Sync NREs without the blackboard.
            Blackboard = new GMIMDEJMFCO { GlobalBlackboard = new MGGBPKANFOA() }
        };

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

        var game = new CGNGOLJKBMP
        {
            LNCOKKIPAEP = keywords,
            Level = BuildLevelInfo(run),
            ItemValue = itemValue,
            Buff = new EGLAPJGFKPN { JPKACKNNHCA = new APNCCNFJBKO() },
            Miracle = new NHLBNBBDHMI { HAJJBOFCFOO = new GMIJHCONFIH() },
            Formula = new DJODDECFHMC { ENFIKCCMHAI = new HMAAICNBKCI() },
            Lineup = lineup,
            // The run restores the player's food buffs.
            BCNMBONFKPK = new HIAHPMEAGFC { JOOJIJFCHMK = true },
            HHMLHKLKNDH = new HGGPKPPBGFP { OHPELHFNGCI = DefaultWorkbenchId },
            BKIPDOFONLD = new CGBKKJENEOF()
        };

        return new PFLOIHLLMEE
        {
            Basic = basic,
            AMEABNLMMGB = game,
            // The exit page's settle button indexes this score arm directly.
            OPDEIFOCJEF = new CABKLLAKHKO { GMEPGCODKKE = new AJCFOLGANFC() }
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
        }

        action.Detail = detail;
        return action;
    }

    public static ONPOFJPEBMJ BuildLevelInfo(RogueTournRunData run)
    {
        var level = new ONPOFJPEBMJ
        {
            Status = (RogueTournLevelStatus)run.LevelStatus,
            Reason = (FKNGOGLNKIG)run.LevelReason,
            BJHMFOAGMBF = (uint)run.CurLayerIndex
        };
        foreach (var layer in run.Layers) level.NNFFPJOLDHK.Add(BuildLayerInfo(layer));
        return level;
    }

    // JOJCPKNIJOH is the layer's CURRENT room index, not its room count.
    public static IJNFOHJGPBC BuildLayerInfo(RogueTournLayerData layer)
    {
        var info = new IJNFOHJGPBC
        {
            LayerIndex = (uint)layer.LayerIndex,
            MDAIAMFJNHM = (uint)layer.LayerId,
            JOJCPKNIJOH = (uint)layer.CurRoomIndex,
            Status = (IDCBNMFDICO)layer.Status
        };
        foreach (var room in layer.Rooms) info.JEHEILKKDEF.Add(BuildRoomInfo(room));
        return info;
    }

    // Unreached rooms ship blank: an identity without a resolvable composition NREs the client's Sync.
    public static ODNBCBEINNC BuildRoomInfo(RogueTournRoomData room)
    {
        var info = new ODNBCBEINNC { EEPIDJJJMAH = (uint)room.RoomIndex };
        if (room.Status == RogueTournStates.RoomNone) return info;

        info.RoomId = (uint)room.RoomId;
        info.LHLKJIDFLIN = (uint)room.RoomType;
        info.Status = (IIAOGEIADGA)room.Status;
        info.FIADMEPDAOG = new ELHLCFBPAMO
        {
            Level = (uint)room.Level,
            CompositionType = (uint)RogueTournResources.PersonaCompositionType(room.CompositionType)
        };
        return info;
    }

    public static RogueTournLevelInfoUpdateScNotify BuildLevelNotify(RogueTournRunData run)
    {
        var notify = new RogueTournLevelInfoUpdateScNotify
        {
            Status = (RogueTournLevelStatus)run.LevelStatus,
            Reason = (FKNGOGLNKIG)run.LevelReason,
            BJHMFOAGMBF = (uint)run.CurLayerIndex
        };
        foreach (var layer in run.Layers) notify.NNFFPJOLDHK.Add(BuildLayerInfo(layer));
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

    // One notify per delta, as the official server emits them.
    public static SyncRogueCommonActionResultScNotify BuildActionResult(RogueTournActionResult result)
    {
        var row = new MAOKMPOELCO();
        var detail = new PKHKEEKIODC();
        switch (result.Kind)
        {
            case RogueTournActionResultKind.Style:
                row.Source = NHHMEPILLHL.Nlcdgipgfdj;
                detail.JKLJBCBFNPL = new JGCMGAMHDHM
                {
                    PGGDEHKKINP = (uint)result.Value,
                    HGKBJMGGPGL = (uint)result.Value
                };
                break;
            case RogueTournActionResultKind.Formula:
            {
                row.Source = NHHMEPILLHL.Midkknagofa;
                var formula = new LEJFIEPMMGG { KAHMGPGPOII = (uint)result.Value };
                // The requirement list is MainBuffType×MainBuffNum then SubBuffType×SubBuffNum.
                if (GameData.RogueTournFormulaData.TryGetValue(result.Value, out var excel))
                {
                    AddFormulaRequirement(formula, excel.MainBuffTypeID, excel.MainBuffNum);
                    AddFormulaRequirement(formula, excel.SubBuffTypeID, excel.SubBuffNum);
                }

                detail.MDGNDLLEFJE = new JJEMDNEIMOJ { DKEENIMPLKD = formula };
                break;
            }
        }

        row.Detail = detail;
        var notify = new SyncRogueCommonActionResultScNotify
        {
            DLOJFEEHLIJ = RogueSubModeTourn,
            DisplayType = DICEONDKEFF.Icfoncmfnlg
        };
        notify.CLGGKLNPLCK.Add(row);
        return notify;
    }

    private static void AddFormulaRequirement(LEJFIEPMMGG formula, int buffTypeId, int count)
    {
        if (buffTypeId == 0 || count == 0) return;
        formula.IFJIHNBDNGN.Add(new CBDMGEGELFM { Key = (uint)buffTypeId, DPICNGBHFAC = count });
    }

    // The per-game wrapper {scene, lineup} every RogueTourn ScRsp carries as IDCMACCPCBH.
    public static JGAMMPGMCNI BuildEnterInfo(PlayerInstance player)
    {
        var info = new JGAMMPGMCNI();
        var scene = player.SceneInstance;
        if (scene != null)
        {
            info.Scene = scene.ToProto();
            scene.MarkClientSceneSnapshotSynced();
        }

        var lineupManager = player.LineupManager!;
        var lineup = lineupManager.GetCurLineup();
        if (lineup == null) return info;

        lineupManager.RecalculateExtraMpCount();
        var lineupProto = lineup.ToProto();
        var maxMp = (uint)lineupManager.GetMaxMp();
        lineupProto.MaxMp = maxMp;
        if (lineupProto.Mp > maxMp) lineupProto.Mp = maxMp;
        info.Lineup = lineupProto;
        return info;
    }

    #endregion
}
