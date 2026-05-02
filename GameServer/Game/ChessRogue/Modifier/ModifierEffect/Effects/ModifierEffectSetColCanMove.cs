using CastoriceHoney.Server.Enums.Rogue;
using CastoriceHoney.Server.GameServer.Game.Battle;

namespace CastoriceHoney.Server.GameServer.Game.ChessRogue.Modifier.ModifierEffect.Effects;

[ModifierEffect(ModifierEffectTypeEnum.SetColCanMove)]
public class ModifierEffectSetColCanMove : ModifierEffectHandler
{
    public override async ValueTask OnConfirmed(ChessRogueDiceModifierInstance modifierInstance,
        ChessRogueInstance chessRogueInstance)
    {
        var curCol = chessRogueInstance.CurCell?.PosX ?? 0;

        var col = modifierInstance.EffectConfig.Params.GetValueOrDefault("Col", "0").Split(";");
        foreach (var c in col)
            if (c.StartsWith("~")) // ~1 means cur + 1
            {
                var offset = int.Parse(c[1..]);
                foreach (var cell in chessRogueInstance.RogueCells.Values.Where(cell =>
                             cell.PosX == curCol + offset && !cell.IsCollapsed()))
                    chessRogueInstance.CanMoveCellIdList.Add(cell.CellId);
            }
            else
            {
                foreach (var cell in chessRogueInstance.RogueCells.Values.Where(cell =>
                             cell.PosX == int.Parse(c) && !cell.IsCollapsed()))
                    chessRogueInstance.CanMoveCellIdList.Add(cell.CellId);
            }

        await ValueTask.CompletedTask;
    }

    public override async ValueTask SelectModifierCell(ChessRogueDiceModifierInstance modifierInstance,
        ChessRogueInstance chessRogueInstance,
        int selectCellId)
    {
        await ValueTask.CompletedTask;
    }

    public override async ValueTask SelectCell(ChessRogueDiceModifierInstance modifierInstance,
        ChessRogueInstance chessRogueInstance,
        int selectCellId)
    {
        await ValueTask.CompletedTask;
    }

    public override void BeforeBattle(ChessRogueDiceModifierInstance modifierInstance, BattleInstance battle,
        ChessRogueInstance instance)
    {
    }

    public override async ValueTask AfterBattle(ChessRogueDiceModifierInstance modifierInstance, BattleInstance battle)
    {
        await ValueTask.CompletedTask;
    }
}