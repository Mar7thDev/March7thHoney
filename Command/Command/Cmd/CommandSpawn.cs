using System.Globalization;
using System.Text.RegularExpressions;
using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.Internationalization;

namespace March7thHoney.Command.Command.Cmd;

/// <summary>
///     /spawn: inject monsters or props near a player and optionally define the battle a spawned monster starts.
///     Everything is runtime-only; nothing is written to the player's save.
/// </summary>
[CommandInfo("spawn", "Game.Command.Spawn.Desc", "Game.Command.Spawn.Usage", permission: CommandPermissions.Spawn)]
public partial class CommandSpawn : ICommand
{
    public const int MaxMonstersPerWave = 5;
    public const int MaxLevel = 100;
    public const int MinRadius = 5;
    public const int MaxRadius = 1000;

    private static readonly HashSet<string> WaveSeparators = ["/", "|", "\\"];

    [CommandDefault]
    public async ValueTask Spawn(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var scene = player.SceneInstance;
        var origin = player.Data.Pos;
        if (scene == null || origin == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.NotInScene", player.Uid.ToString()));
            return;
        }

        if (!scene.SupportsGmSpawn)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.UnsupportedScene"));
            return;
        }

        if (!TryParse(arg.Args, out var request) || request.SpawnId == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.Usage"));
            return;
        }

        if (request.StageId > 0 && !GameData.StageConfigData.ContainsKey(request.StageId))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.InvalidStageId",
                request.StageId.ToString()));
            return;
        }

        if (scene.Entities.Count + request.Amount >= SceneInstance.GmSpawnMaxEntities)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.MaxEntities",
                SceneInstance.GmSpawnMaxEntities.ToString()));
            return;
        }

        var target = Resolve(request.SpawnId);
        switch (target.Kind)
        {
            case SpawnTargetKind.NoMonsterBase:
                await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.NoMonsterBase"));
                return;
            case SpawnTargetKind.BattleMonsterNoScene:
                await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.BattleMonsterNoScene", request.SpawnId));
                return;
            case SpawnTargetKind.Invalid:
                await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.InvalidId", request.SpawnId));
                return;
        }

        if (request.InvalidBattleMonsters.Count > 0)
            await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.InvalidBattleMonster",
                string.Join(", ", request.InvalidBattleMonsters)));

        var radius = request.Radius * 1000;
        if (target.Kind == SpawnTargetKind.Prop)
        {
            var props = await scene.SpawnGmProps(target.Prop!, request.Amount, origin, radius);
            if (props == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.NoPropTemplate"));
                return;
            }

            await arg.SendMsg(props.Count == 0
                ? I18NManager.Translate("Game.Command.Spawn.SpawnFailed")
                : I18NManager.Translate("Game.Command.Spawn.SpawnedProps", props.Count.ToString(),
                    target.Prop!.ID.ToString()));
            return;
        }

        var monsters = await scene.SpawnGmMonsters(target.Monster!, request.Amount, origin, radius,
            request.StageId == 0, monster =>
            {
                monster.CustomStageId = request.StageId;
                monster.CustomLevel = request.Level;
                monster.CustomBattleHp = request.Hp;
                monster.CustomBattleWaves = request.Waves.Count > 0
                    ? request.Waves.Select(wave => wave.ToList()).ToList()
                    : null;
            });
        if (monsters == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.NoMonsterTemplate"));
            return;
        }

        if (monsters.Count == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.SpawnFailed"));
            return;
        }

        await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.SpawnedMonsters", monsters.Count.ToString(),
            target.Monster!.ID.ToString()));
        if (request.Waves.Count > 0)
            await arg.SendMsg(I18NManager.Translate("Game.Command.Spawn.CustomBattle", request.Waves.Count.ToString(),
                string.Join(" / ", request.Waves.Select(wave => string.Join(" ", wave)))));
    }

    /// <summary>
    ///     Parses <c>&lt;id&gt; [s&lt;stageId&gt;] [lv&lt;level&gt;] [hp&lt;hp&gt;] [x&lt;amount&gt;] [r&lt;radius&gt;]
    ///     [&lt;monsterId&gt;... [/|\] ...]</c>. Options may appear anywhere; the first other token is the spawn ID and
    ///     the rest define custom battle waves. CommandArg keys options by their first character only, so the
    ///     two-letter <c>lv</c>/<c>hp</c> prefixes are parsed here.
    /// </summary>
    /// <returns><c>false</c> when an option value is out of range.</returns>
    public static bool TryParse(IEnumerable<string> args, out SpawnRequest request)
    {
        request = new SpawnRequest();
        var positional = new List<string>();
        foreach (var token in args)
        {
            if (string.IsNullOrEmpty(token) || token.StartsWith('@')) continue;

            var match = OptionPattern().Match(token);
            if (!match.Success)
            {
                positional.Add(token);
                continue;
            }

            var key = match.Groups["key"].Value.ToLowerInvariant();
            var digits = match.Groups["value"].Value;
            if (key == "hp")
            {
                if (!uint.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var hp)) return false;
                request.Hp = hp;
                continue;
            }

            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var value)) return false;
            switch (key)
            {
                case "s":
                    request.StageId = value;
                    break;
                case "lv" or "l":
                    request.Level = Math.Min(value, MaxLevel);
                    break;
                case "x":
                    request.Amount = Math.Max(value, 1);
                    break;
                case "r":
                    request.Radius = Math.Clamp(value, MinRadius, MaxRadius);
                    break;
            }
        }

        if (positional.Count == 0) return true;

        request.SpawnId = positional[0];
        ParseWaves(positional.Skip(1), request);
        return true;
    }

    private static void ParseWaves(IEnumerable<string> tokens, SpawnRequest request)
    {
        var current = new List<int>();

        void Flush()
        {
            if (current.Count == 0) return;
            request.Waves.Add(current);
            current = [];
        }

        foreach (var token in tokens)
        {
            if (WaveSeparators.Contains(token))
            {
                Flush();
                continue;
            }

            if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var monsterId) ||
                monsterId <= 0 || !GameData.MonsterConfigData.ContainsKey(monsterId))
            {
                request.InvalidBattleMonsters.Add(token);
                continue;
            }

            if (current.Count == MaxMonstersPerWave) Flush();
            current.Add(monsterId);
        }

        Flush();
    }

    /// <summary>
    ///     <c>monster</c> is the smallest NPC monster ID. A numeric ID is tried as an NPC monster, then as a battle
    ///     MonsterConfig ID mapped through its template's NPCMonsterList, then as a maze prop.
    /// </summary>
    public static SpawnTarget Resolve(string spawnId)
    {
        if (spawnId.Equals("monster", StringComparison.OrdinalIgnoreCase))
            return GameData.NpcMonsterDataData.Count == 0
                ? new SpawnTarget(SpawnTargetKind.NoMonsterBase)
                : new SpawnTarget(SpawnTargetKind.Monster, GameData.NpcMonsterDataData[GameData.NpcMonsterDataData.Keys.Min()]);

        if (!int.TryParse(spawnId, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            return new SpawnTarget(SpawnTargetKind.Invalid);

        if (GameData.NpcMonsterDataData.TryGetValue(id, out var npcMonster))
            return new SpawnTarget(SpawnTargetKind.Monster, npcMonster);

        if (GameData.MonsterConfigData.TryGetValue(id, out var battleMonster) &&
            GameData.MonsterTemplateConfigData.TryGetValue(battleMonster.MonsterTemplateID, out var template))
            foreach (var npcId in template.NPCMonsterList)
                if (GameData.NpcMonsterDataData.TryGetValue(npcId, out npcMonster))
                    return new SpawnTarget(SpawnTargetKind.Monster, npcMonster);

        if (GameData.MazePropData.TryGetValue(id, out var prop))
            return new SpawnTarget(SpawnTargetKind.Prop, Prop: prop);

        return GameData.MonsterConfigData.ContainsKey(id)
            ? new SpawnTarget(SpawnTargetKind.BattleMonsterNoScene)
            : new SpawnTarget(SpawnTargetKind.Invalid);
    }

    [GeneratedRegex(@"^(?<key>lv|hp|l|s|x|r)(?<value>\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OptionPattern();

    public class SpawnRequest
    {
        public string? SpawnId { get; set; }
        public int StageId { get; set; }
        public int Level { get; set; }
        public uint Hp { get; set; }
        public int Amount { get; set; } = 1;
        public int Radius { get; set; } = MinRadius;
        public List<List<int>> Waves { get; } = [];
        public List<string> InvalidBattleMonsters { get; } = [];
    }

    public enum SpawnTargetKind
    {
        Invalid,
        NoMonsterBase,
        BattleMonsterNoScene,
        Monster,
        Prop
    }

    public record SpawnTarget(SpawnTargetKind Kind, NPCMonsterDataExcel? Monster = null, MazePropExcel? Prop = null);
}