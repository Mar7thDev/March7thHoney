using March7thHoney.Data;
using March7thHoney.Database.RogueMagic;
using March7thHoney.Enums.Rogue;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.RogueMagic;

/// <summary>
///     RogueMagic wire builders. The 4.5 protocol is obfuscated below the top-level messages; arms here are matched
///     by field shape against the legacy readable implementation and yuukisr_go's 4.5 capture-checked query.
/// </summary>
public static class RogueMagicProtoBuilder
{
    public const uint RogueSubModeMagic = (uint)RogueSubModeEnum.MagicRogue;

    // IJLEFNJPFKP's single field; the legacy server sent RogueMagicBattleConst = 5.
    private const uint BattleRogueMode = 5;

    private const uint ReviveCost = 80;
    private const uint MagicUnitSelectHintId = 260002;

    #region Meta

    // Shape follows yuukisr_go's capture-checked query (story and talent arms always present); values grant full
    // access as the legacy server did.
    public static CILKBJMGNGL BuildQueryInfo()
    {
        var talents = new FDBDLBAPDBE();
        foreach (var talentId in GameData.RogueMagicTalentData.Keys.OrderBy(id => id))
            talents.BLHJIPBHHFO.Add(new OKPIPFKONMG { CKHCLKOALKE = (uint)talentId, Status = RogueTalentStatus.Enable });

        var info = new CILKBJMGNGL
        {
            StoryInfo = new GNPFDBACMIH(),
            IHJKLHGMKBI = new DFGAIOOGPPA { JCOBJAMPOAL = talents }
        };
        info.StoryInfo.NOMDDGGIIBF.AddRange(GameData.RogueMagicStoryData.Keys.OrderBy(id => id).Select(id => (uint)id));

        foreach (var areaId in GameData.RogueMagicAreaData.Keys.OrderBy(id => id))
            info.DANNIDIPFAL.Add(new IFCNLKPPPHK
            {
                AreaId = (uint)areaId,
                IsUnlock = true,
                IsTakenReward = true,
                FJKHIFDJMDB = true
            });

        foreach (var compId in GameData.RogueMagicDifficultyCompData.Keys.OrderBy(id => id))
            info.JJIEDDMALPE.Add(new NMOMMMGHMOA { DFBDPEHAAMG = (uint)compId, IsUnlock = true });

        return info;
    }

    // The client dereferences both arms.
    public static void FillMiscRealTimeData(RogueMagicGetMiscRealTimeDataScRsp rsp)
    {
        rsp.KDDGKLNGNEA = new EJHDAJAGFIJ();
        rsp.BNLLEJPCIGD = new NOAKCDKAAFB();
    }

    #endregion

    #region Run

    public static GCBCKOKEAJH BuildCurInfo(RogueMagicRunData run)
    {
        var itemValue = new FKEDPCMLIFE();
        itemValue.HJANIOKJALB[RogueMagicStates.ItemValueCoin] = (uint)run.Coin;

        var lineup = new JEIKFFFGONB
        {
            LNIEFKBMIPD = new ItemCostData
            {
                ItemList =
                {
                    new ItemCost
                    {
                        PileItem = new PileItem { ItemId = RogueMagicStates.ItemValueCoin, ItemNum = ReviveCost }
                    }
                }
            }
        };
        foreach (var avatarId in run.BaseAvatarIds)
            lineup.BHBPKKKNABO.Add(new DINGDGLKHIP { AvatarId = (uint)avatarId, HOKEFMONOPG = BEAIKEEANDP.Nkdecdgecfl });

        var difficulty = new FPNDOBPMNED();
        difficulty.KEHKLHMEABI.AddRange(run.DifficultyCompIds.Select(id => (uint)id));

        var items = new PLBAMNCLBHB { NBMNPGKEIIH = (uint)run.StyleType };
        foreach (var unit in run.Units)
            items.HIILDALHIMD.Add(new MMBBHFNJILI
            {
                UniqueId = (uint)unit.UniqueId,
                OJEANDPABHC = new OKCCLEEPELM { CFKKKPNKHEL = (uint)unit.UnitId, Level = (uint)unit.Level }
            });
        foreach (var scepter in run.Scepters) items.MALDOMHPILM.Add(BuildScepter(scepter));

        return new GCBCKOKEAJH
        {
            BasicInfo = new CBOLONJDHHL
            {
                TransNo = run.TransNo,
                LJOBBEFACPG = (uint)run.AreaId,
                DLOJFEEHLIJ = RogueSubModeMagic,
                FMIGOKIDKJL = BuildPendingAction(run),
                // The game data Sync NREs without the blackboard.
                Blackboard = new GMIMDEJMFCO { GlobalBlackboard = new MGGBPKANFOA() }
            },
            Miracle = new NHLBNBBDHMI { HAJJBOFCFOO = new GMIJHCONFIH() },
            MEEJDFNHBDI = difficulty,
            ItemValue = itemValue,
            Level = BuildLevelInfo(run),
            Lineup = lineup,
            DDLADFJMEEC = items
        };
    }

    private static DNLKMHLAPHC BuildScepter(RogueMagicScepterData scepter)
    {
        var info = new DNLKMHLAPHC
        {
            OOPCPMANCBO = new OCBPJLLLJGO { GCAHKLNGFCK = (uint)scepter.ScepterId, Level = (uint)scepter.Level }
        };
        if (!GameData.RogueMagicScepterData.TryGetValue(scepter.ScepterId * 100 + scepter.Level, out var excel))
            return info;

        foreach (var (mountType, count) in excel.TrenchCount) info.HCPNPDLJKGF[(uint)mountType] = (uint)count;
        foreach (var locked in excel.LockMagicUnit)
            info.AJFLAHGJELP.Add(new OKCCLEEPELM
            {
                CFKKKPNKHEL = (uint)locked.MagicUnitId,
                Level = (uint)locked.MagicUnitLevel
            });
        return info;
    }

    public static KDHAIFIOKDN BuildLevelInfo(RogueMagicRunData run)
    {
        var level = new KDHAIFIOKDN
        {
            Status = (PFKHKAMBHBN)run.LevelStatus,
            BJHMFOAGMBF = (uint)run.CurLayerIndex
        };
        foreach (var layer in run.Layers) level.NNFFPJOLDHK.Add(BuildLayerInfo(layer, null));
        return level;
    }

    // JOJCPKNIJOH is the layer's CURRENT room index.
    private static GPEPGKCPBPB BuildLayerInfo(RogueMagicLayerData layer, List<int>? roomIndexes)
    {
        var info = new GPEPGKCPBPB
        {
            LayerIndex = (uint)layer.LayerIndex,
            MDAIAMFJNHM = (uint)layer.LayerId,
            JOJCPKNIJOH = (uint)layer.CurRoomIndex,
            Status = (JGNAEPIJDNC)layer.Status
        };
        foreach (var room in layer.Rooms.Where(room => roomIndexes == null || roomIndexes.Contains(room.RoomIndex)))
            info.JEHEILKKDEF.Add(new FODHGNLPBKH
            {
                EEPIDJJJMAH = (uint)room.RoomIndex,
                RoomId = (uint)room.RoomId,
                Status = (HOIFDJPBGLB)room.Status
            });
        return info;
    }

    public static RogueMagicLevelInfoUpdateScNotify BuildLevelNotify(RogueMagicRunData run, RogueMagicLayerData layer,
        List<int>? roomIndexes)
    {
        var notify = new RogueMagicLevelInfoUpdateScNotify
        {
            Status = (PFKHKAMBHBN)run.LevelStatus,
            BJHMFOAGMBF = (uint)run.CurLayerIndex
        };
        notify.NNFFPJOLDHK.Add(BuildLayerInfo(layer, roomIndexes));
        return notify;
    }

    public static INHBMHCJJCN BuildPendingAction(RogueMagicRunData run)
    {
        var action = new INHBMHCJJCN();
        var pending = run.PendingActions.FirstOrDefault();
        if (pending == null || pending.OptionIds.Count == 0) return action;

        action.QueuePosition = (uint)pending.QueuePos;
        var detail = new DCFPEIMAMCN();
        switch (pending.Kind)
        {
            case RogueMagicPendingKind.Scepter:
            {
                var select = new NFNMMFBJBLC();
                for (var i = 0; i < pending.OptionIds.Count; i++)
                    select.INLDHMAIPMH.Add(new OCBPJLLLJGO
                    {
                        GCAHKLNGFCK = (uint)pending.OptionIds[i],
                        Level = (uint)pending.OptionLevels[i]
                    });
                detail.FJCHODNDIKA = select;
                break;
            }
            case RogueMagicPendingKind.MagicUnit:
            {
                var select = new KBKLNECKDEN { LGIPILAKIKB = MagicUnitSelectHintId };
                for (var i = 0; i < pending.OptionIds.Count; i++)
                    select.AGKFAODICEA.Add(new OKCCLEEPELM
                    {
                        CFKKKPNKHEL = (uint)pending.OptionIds[i],
                        Level = (uint)pending.OptionLevels[i]
                    });
                detail.GOIFJFKIFFF = select;
                break;
            }
        }

        action.Detail = detail;
        return action;
    }

    public static SyncRogueCommonPendingActionScNotify BuildPendingNotify(RogueMagicRunData run)
    {
        return new SyncRogueCommonPendingActionScNotify
        {
            DLOJFEEHLIJ = RogueSubModeMagic,
            Action = BuildPendingAction(run)
        };
    }

    public static CommonRogueVirtualItemInfoScNotify BuildCoinNotify(RogueMagicRunData run)
    {
        var notify = new CommonRogueVirtualItemInfoScNotify();
        notify.DMCINCJLMND.Add(new LKCDLJCKCJI
        {
            ItemType = RogueMagicStates.ItemValueCoin,
            KMNILIKJFHD = (uint)run.Coin
        });
        return notify;
    }

    public static BattleRogueMagicInfo BuildBattleInfo(RogueMagicRunData run)
    {
        var item = new CJHPPLHFNAB
        {
            BJMJBLCMPAL = new EMLFDPFAPJP
            {
                NAEAGNLBKAM = (uint)run.BasicRoundCnt,
                MHPPIKLBNMD = (uint)run.ExtraRoundCnt
            }
        };

        foreach (var scepter in run.Scepters)
        {
            var battleScepter = new KCIEGDGJMBE
            {
                GCAHKLNGFCK = (uint)scepter.ScepterId,
                Level = (uint)scepter.Level
            };
            if (GameData.RogueMagicScepterData.TryGetValue(scepter.ScepterId * 100 + scepter.Level, out var excel))
            {
                foreach (var (mountType, count) in excel.TrenchCount)
                    battleScepter.HCPNPDLJKGF[(uint)mountType] = (uint)count;
                foreach (var locked in excel.LockMagicUnit)
                    battleScepter.HIILDALHIMD.Add(new OJMPKMNINGK
                    {
                        CFKKKPNKHEL = (uint)locked.MagicUnitId,
                        Level = (uint)locked.MagicUnitLevel,
                        IsLocked = true
                    });
            }

            item.AHBOCLNGFNM.Add(battleScepter);
        }

        return new BattleRogueMagicInfo
        {
            OOPCPMANCBO = new IJLEFNJPFKP { RogueMode = BattleRogueMode },
            DetailInfo = new BLJEJFMKEII { FODLGHIFJGC = item }
        };
    }

    // The per-game wrapper {scene, lineup, rotate} every RogueMagic ScRsp carries as IDCMACCPCBH.
    public static EEILAINOLPO BuildEnterInfo(PlayerInstance player)
    {
        var info = new EEILAINOLPO { JNKLNLKHGBB = new FHEGNOPNICH() };
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
