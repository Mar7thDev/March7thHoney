using March7thHoney.Enums.Mission;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.Internationalization;

namespace March7thHoney.Command.Command.Cmd;

/// <summary>
///     Sets the state of a loaded prop to repair a scene a save left in a state the story cannot pass. Some prop
///     graphs only react on the client: a console pressed while Locked shows "Not authorized to use the control
///     panel" and sends nothing, so a save that froze it in Locked cannot be passed in game. The prop is found by
///     proximity, runtime entity ID or MazeProp ID; a MazeProp ID shared by several loaded props needs the group.
///     With --quest the mission and quest prop-state goals are checked afterwards, as an interaction does; without
///     it the story does not move, so a cosmetic fix cannot finish a mission by accident.
/// </summary>
[CommandInfo("changepropstats", "Game.Command.ChangePropStats.Desc", "Game.Command.ChangePropStats.Usage",
    ["cps", "propstate"], permission: CommandPermissions.Scene)]
public class CommandChangePropStats : ICommand
{
    [CommandMethod("0 help")]
    public async ValueTask ShowStates(CommandArg arg)
    {
        await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.StateList",
            string.Join(", ", Enum.GetValues<PropStateEnum>().Select(StateText))));
    }

    [CommandDefault]
    public async ValueTask ChangeState(CommandArg arg)
    {
        // --quest is the only option; whatever follows it is still an operand.
        var operands = arg.Args.Where(token => !token.StartsWith("--", StringComparison.Ordinal)).ToList();
        var quest = arg.CharacterArgs.ContainsKey("quest");
        if (operands.Count is 0 or > 3 || operands[0] == "?")
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.Usage"));
            return;
        }

        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var player = arg.Target.Player!;
        var scene = player.SceneInstance;
        if (scene == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.NotInScene"));
            return;
        }

        if (!TryParseState(operands[^1], out var state))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.InvalidState", operands[^1]));
            return;
        }

        var props = scene.Entities.Values.OfType<EntityProp>().ToList();
        EntityProp? prop;
        if (operands.Count == 1)
        {
            prop = props.MinBy(entity => entity.Position.GetFast2dDist(player.Data.Pos!));
            if (prop == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.NoPropNearby"));
                return;
            }
        }
        else
        {
            var groupId = 0;
            if (!int.TryParse(operands[0], out var id) ||
                (operands.Count == 3 && !int.TryParse(operands[1], out groupId)))
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
                return;
            }

            prop = await FindProp(arg, props, id, groupId);
            if (prop == null) return;
        }

        await Apply(arg, player, prop, state, quest);
    }

    /// <summary>
    ///     A runtime entity ID (the one /scene near prints) wins; otherwise the ID is a MazeProp ID, which several
    ///     loaded props may share - those are reported with their groups instead of guessing one.
    /// </summary>
    private static async ValueTask<EntityProp?> FindProp(CommandArg arg, List<EntityProp> props, int id, int groupId)
    {
        var byEntity = props.Find(entity => entity.EntityId == id);
        if (byEntity != null)
        {
            if (groupId == 0 || byEntity.GroupId == groupId) return byEntity;
            await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.GroupMismatch", id.ToString(),
                byEntity.GroupId.ToString(), groupId.ToString()));
            return null;
        }

        var matches = props.Where(entity => entity.Excel.ID == id && (groupId == 0 || entity.GroupId == groupId))
            .ToList();
        switch (matches.Count)
        {
            case 1:
                return matches[0];
            case 0:
                await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.PropNotFound", id.ToString()));
                return null;
            default:
                await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.AmbiguousId", id.ToString(),
                    matches.Count.ToString(),
                    string.Join(", ", matches.Select(entity => entity.GroupId).Distinct().Order())));
                return null;
        }
    }

    private static async ValueTask Apply(CommandArg arg, PlayerInstance player, EntityProp prop, PropStateEnum state,
        bool quest)
    {
        var previous = prop.State;
        if (previous == state)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.Unchanged",
                prop.EntityId.ToString(), prop.GroupId.ToString(), prop.InstId.ToString(), StateText(state)));
            return;
        }

        // SetState refreshes the client and saves the state unless the group resets on reload.
        await prop.SetState(state);
        await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.Changed", prop.EntityId.ToString(),
            prop.Excel.ID.ToString(), prop.GroupId.ToString(), prop.InstId.ToString(), StateText(previous),
            StateText(state)));

        if (quest)
        {
            await player.MissionManager!.HandleFinishType(MissionFinishTypeEnum.PropState);
            await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.QuestChecked"));
        }
        else
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.QuestSkipped"));
        }

        // The prop's own list decides which states its graph reacts to, so a wrong guess shows at once.
        if (prop.Excel.PropStateList.Count > 0)
            await arg.SendMsg(I18NManager.Translate("Game.Command.ChangePropStats.DeclaredStates",
                string.Join(", ", prop.Excel.PropStateList.Select(StateText))));
    }

    /// <summary>
    ///     A state name in any case or a defined numeric value. Anything else is refused: falling back to Closed
    ///     would turn a typo into a real state change.
    /// </summary>
    private static bool TryParseState(string token, out PropStateEnum state)
    {
        if (int.TryParse(token, out var value))
        {
            state = (PropStateEnum)value;
            return Enum.IsDefined(state);
        }

        return Enum.TryParse(token, true, out state) && Enum.IsDefined(state);
    }

    private static string StateText(PropStateEnum state)
    {
        return $"{state}({(int)state})";
    }
}
