using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.Database.RogueTourn;
using March7thHoney.Enums.Scene;
using March7thHoney.Enums.TournRogue;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.RogueTourn;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.Proto;
using March7thHoney.Util;
using MemoryPack;

/// <summary>
///     Divergent Universe (RogueTourn) run flow against the cached 4.6 tables: layer/room generation, the Persona
///     answer chain (style → equation → gift → room chooser), the in-layer room swap, settle and the archive.
///     Battles are not fought: a room is cleared the way an in-scene kill clears it.
/// </summary>
internal static class RogueTournRegression
{
    public static async Task Run(Action<string, bool> check, string repositoryConfigPath)
    {
        check("DU cache carries the Persona layer, preset, gift and exp-reward tables",
            GameData.RoguePersonaLayerRoomData.TryGetValue(3001, out var layer3001) && layer3001.Count == 4 &&
            GameData.RoguePersonaRoomPresetData.ContainsKey(1002) && GameData.RoguePersonaRoomCompTypeData.Count > 0 &&
            GameData.RoguePersonaStyleGiftData.Count > 0 && GameData.RogueTournExpRewardData.Count > 0);
        check("DU blessing pools carry their TournMode and resolve to unique Tourn3 blessings",
            RogueTournResources.BuffPool(RogueTournModeEnum.Tourn3) is { Count: > 3 } pool &&
            pool.Select(buff => buff.MazeBuffID).Distinct().Count() == pool.Count);

        var dialogue = RogueTournEventDialogue.For(722001);
        check("DU event Act graph resolves its option pool and finishing options",
            dialogue is { OptionIds.Count: 9, FinishingOptionIds.Count: > 0 } &&
            dialogue.OptionIds.All(option => dialogue.IsFinishingOption(option) || dialogue.EventsForOption(option).Count > 0));

        var player = new PlayerInstance(60301);
        await player.InitializeAsync();
        var manager = player.RogueTournManager!;
        var avatars = player.AvatarManager!.Data.FormalAvatars.Select(avatar => avatar.BaseAvatarId).Distinct().Take(4)
            .ToList();

        var talents = RogueTournProtoBuilder.BuildPermanentTalentInfo(manager.Data).JCOBJAMPOAL.BLHJIPBHHFO;
        check("DU permanent talents unlock only the circuit's roots on a fresh account",
            talents.Count > 1 && talents.Count(t => t.Status == RogueTalentStatus.Unlock) is > 0 and var roots &&
            roots < talents.Count && talents.All(t => t.Status != RogueTalentStatus.Enable));
        check("DU weekly challenge reports the configured rotation id",
            RogueTournProtoBuilder.BuildWeekChallenge(manager.Data).JNBIIJCAKKB ==
            (uint)ConfigManager.Config.ServerOption.RogueTourn.WeekChallengeId);

        var configPath = ConfigManager.Config.Path.ConfigPath;
        ConfigManager.Config.Path.ConfigPath = repositoryConfigPath;
        var (ret, run) = await manager.StartRun(20405, avatars);
        ConfigManager.Config.Path.ConfigPath = configPath;
        check("DU area 20405 starts", ret == Retcode.RetSucc && run != null);
        if (run == null) return;

        var rooms = run.Layers.SelectMany(layer => layer.Rooms).ToList();
        check("DU layers follow the Persona room counts and number rooms 1..N across the run",
            run.Layers.Select(layer => layer.Rooms.Count).SequenceEqual([4, 5, 4]) &&
            rooms.Select(room => room.UniqueId).SequenceEqual(Enumerable.Range(1, rooms.Count)));
        check("DU layers draw their planes from the authored pool, one difficulty per layer",
            run.Layers.All(layer => layer.PlaneId is 80702 or 80703 or 80704) &&
            run.Layers.Select(layer => layer.DifficultyId).SequenceEqual([3051, 3052, 3053]) &&
            run.Layers[0].Rooms.Select(room => room.MonsterLevel).SequenceEqual([86, 87, 88, 88]));
        check("DU preset slots replay their preset card (Battle lv3 opener, Boss lv1 closers, Respite)",
            run.Layers[0].Rooms[0] is { PresetId: 1001, RoomType: 3, Level: 3 } &&
            run.Layers.All(layer => layer.Rooms[^1] is { PresetId: 1002, RoomType: 1, Level: 1 }) &&
            run.Layers[2].Rooms[2] is { PresetId: 1003, RoomType: 10 });
        check("DU run opens with the two source-less start modifiers and the style select",
            run.Modifiers.Count == 2 && run.Pending?.Kind == RogueTournPendingKind.Style);

        var entered = await manager.EnterRoomScene(run.CurRoom());
        var scene = player.SceneInstance;
        var monsters = scene?.Entities.Values.OfType<EntityMonster>().ToList() ?? [];
        check("DU first room loads its block as a TournRogue scene with three level-86 room mobs",
            entered && scene is { GameModeType: GameModeTypeEnum.TournRogue } && monsters.Count == 3 &&
            monsters.All(monster => monster.CustomLevel == 86 && monster.RogueMonsterKind == 1));
        check("DU combat room door ships shut",
            scene!.Entities.Values.OfType<EntityProp>().Where(prop => prop.Excel.PropType == PropTypeEnum.PROP_ROGUE_DOOR)
                .All(door => door.State == RogueTournEntityLoader.DoorShut));

        var snapshot = RogueTournProtoBuilder.BuildRunInfo(run);
        check("DU snapshot ships the blackboard flags, start modifiers, deck constants and blank-but-present rooms",
            snapshot.Basic.Blackboard.GlobalBlackboard.GFKAFIFMHNF.Count == 5 &&
            snapshot.Basic.Modifier.HDLGLFGKONC.Count == 2 &&
            snapshot.Basic.Modifier.HDLGLFGKONC.All(row => row.SourceType == 0) &&
            snapshot.AMEABNLMMGB.Level.FIADMEPDAOG?.CCFGCCMEFCI?.MKJDMNAEAED == 40 &&
            snapshot.AMEABNLMMGB.Level.NNFFPJOLDHK.SelectMany(layer => layer.JEHEILKKDEF)
                .All(room => room.FIADMEPDAOG != null));
        var registration = RogueTournProtoBuilder.BuildRoomRegistrationNotify(run);
        check("DU room registration lists every room once", registration?.CLGGKLNPLCK.Count == rooms.Count);

        var style = run.Pending!.Options[0];
        var styleRsp = await manager.HandlePendingAction(new OFEDNNJIJFL
        {
            MPLGEEGOAMC = (uint)run.Pending.QueuePos,
            DPAMEGCFNAJ = new CLMPCCGCADK { EOOLCIACHFJ = (uint)style }
        });
        check("DU style confirmation mints the armed style modifier pair and opens the Equation draft",
            styleRsp?.Retcode == 0 && run.StyleModifier != 0 && run.Modifiers.Count == 4 &&
            !run.ModifierIsStart(run.StyleModifier) && run.Pending?.Kind == RogueTournPendingKind.Formula &&
            run.StyleExp > 0 && run.StyleRows.Count == 0);

        var formula = run.Pending!.Options[0];
        var formulaRsp = await manager.HandlePendingAction(new OFEDNNJIJFL
        {
            MPLGEEGOAMC = (uint)run.Pending.QueuePos,
            FMDNOHNKHDP = new HELGMBBOGMJ { JMNMFMHPCKD = (uint)formula }
        });
        check("DU Equation pick joins the run and the handbook",
            formulaRsp?.Retcode == 0 && run.Formulas.Contains(formula) && manager.Handbook.Formulas.Contains(formula) &&
            run.Pending == null);

        // Clear the room the way an in-scene kill does: no monster left.
        foreach (var monster in monsters) await scene.RemoveEntity(monster, false);
        var cleared = await manager.ClearRoomIfMonstersGone();
        var gifts = run.Pending?.Options ?? [];
        check("DU emptied room finishes, opens its doors and deals a style-consistent gift window",
            cleared && run.CurRoom()?.Status == RogueTournStates.RoomFinish &&
            scene.Entities.Values.OfType<EntityProp>().Where(prop => prop.Excel.PropType == PropTypeEnum.PROP_ROGUE_DOOR)
                .All(door => door.State == RogueTournEntityLoader.DoorOpen) &&
            run.Pending?.Kind == RogueTournPendingKind.Gift && gifts.Count == 3 &&
            gifts.All(id => GameData.RoguePersonaStyleGiftData[id] is { IsCore: false } gift &&
                            (gift.StyleIDList.Count == 0 || gift.StyleIDList.Contains(run.PersonaStyle))));

        await manager.HandlePendingAction(new OFEDNNJIJFL
        {
            MPLGEEGOAMC = (uint)run.Pending!.QueuePos,
            JGOLAJCHKAG = new JKLLLNDHJHI { PGDEHMAGGLF = (uint)gifts[0] }
        });
        var exit = run.Pending;
        check("DU gift pick records the gift, mints its modifier and opens the three-card room chooser",
            run.Gifts.SequenceEqual([gifts[0]]) && run.Modifiers.Count == 5 &&
            exit is { Kind: RogueTournPendingKind.Exit, Options.Count: 3, ExitPreset: 0 } &&
            exit.Options.All(uid => run.RoomByUniqueId(uid).Room is { Status: RogueTournStates.RoomNone, PresetId: 0 }));

        var versionBefore = player.ClientPosVersion;
        var pick = exit!.Options[^1];
        var (_, pickedCard) = run.RoomByUniqueId(pick);
        var pickedType = pickedCard!.RoomType;
        var exitRsp = await manager.HandlePendingAction(new OFEDNNJIJFL
        {
            MPLGEEGOAMC = (uint)exit.QueuePos,
            EKNBICFHDJP = new EKIFAOJKOCL { UniqueId = (uint)pick }
        });
        var slot = run.Layers[0].Rooms[1];
        check("DU room pick walks to the next slot with the picked card and spends the whole triple",
            exitRsp?.Retcode == 0 && run.Layers[0].CurRoomIndex == 2 && slot.UniqueId == pick &&
            slot.RoomType == pickedType && slot.Status == RogueTournStates.RoomProcessing &&
            exit.Options.All(run.DeckConsumed.Contains));
        check("DU in-layer room change re-targets the same scene and advances the position version",
            ReferenceEquals(player.SceneInstance, scene) &&
            scene.EntityLoader is RogueTournEntityLoader { RoomUniqueId: var uid } && uid == pick &&
            player.ClientPosVersion == versionBefore + 1 && scene.ClientPosVersion == player.ClientPosVersion);
        var delta = RogueTournProtoBuilder.BuildLevelNotify(run);
        check("DU level push is a per-room delta",
            delta.NNFFPJOLDHK.Count == 1 && delta.NNFFPJOLDHK[0].JEHEILKKDEF.Count == 1 &&
            delta.NNFFPJOLDHK[0].JEHEILKKDEF[0].EEPIDJJJMAH == 2);

        var granted = await manager.GrantAllBlessings();
        check("DU blessall grants the Tourn3 pool and climbs the style tier to its cap",
            granted > 0 && run.Buffs.Count == granted && run.StyleTier() == RogueTournRunData.StyleTierCap &&
            player.SceneInstance!.ToProto().SceneBuffInfoList.Count >= granted);

        // Walk to the last room of the last layer with every room cleared, then finish and settle.
        foreach (var room in rooms) room.Status = RogueTournStates.RoomFinish;
        run.CurLayerIndex = 3;
        run.Layers[2].CurRoomIndex = run.Layers[2].Rooms.Count;
        run.Pending = null;
        var notFinal = run.AtFinalRoom();
        var finished = manager.FinishRun();
        var (score, gain, total) = manager.SettleScore();
        var settle = RogueTournProtoBuilder.BuildSettlePayload(run, score, gain, total);
        check("DU last cleared room finishes the run and the settle page charges the authored score rule",
            notFinal && finished && run.Status == RogueTournStates.RunFinished && score > 0 && gain == score * 10 &&
            settle.JJLFKNPEPFP.NIOFIOOGKLD == score && settle.PHGJIJELKDB.AMEABNLMMGB.Level.FIADMEPDAOG
                .CCFGCCMEFCI.LONFKFCFDND.Count == rooms.Count);

        var expBefore = manager.Data.Exp;
        var archiveSlot = manager.SettleRun("regression", 0);
        var archive = manager.ArchiveOf(archiveSlot);
        check("DU settle writes the archive, grants the season exp and refuses a second settle",
            archiveSlot == 1 && manager.Data.Exp == expBefore + gain && archive != null &&
            RogueTournProtoBuilder.BuildArchive(archive).PHGJIJELKDB?.Basic.LJOBBEFACPG == 20405 &&
            manager.SettleRun("again", 0) == 0);

        var (paid, _) = await manager.TakeExpRewards((int)RogueTournProtoBuilder.CurMainTournId, [999]);
        check("DU exp reward refuses a level the ladder does not have", paid.Count == 0);

        // A run blob from before the room unique ids heals on load.
        var legacy = MemoryPackSerializer.Deserialize<RogueTournRunData>(MemoryPackSerializer.Serialize(run))!;
        foreach (var room in legacy.Layers.SelectMany(layer => layer.Rooms)) room.UniqueId = 0;
        legacy.EnsureCollections();
        check("DU legacy run blob gets its rooms numbered on load",
            legacy.Layers.SelectMany(layer => layer.Rooms).Select(room => room.UniqueId)
                .SequenceEqual(Enumerable.Range(1, rooms.Count)));

        check("DU reset drops the live run", await manager.ResetRun() && manager.RunInstance == null);
    }
}
