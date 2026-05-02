using March7thHoney.Enums.Rogue;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Server.Packet.Send.ChessRogue;
using March7thHoney.GameServer.Server.Packet.Send.RogueModifier;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.ChessRogue.Modifier.ModifierEffect.Effects;

[ModifierEffect(ModifierEffectTypeEnum.ReplicateCurCellToSelectCell)]
public class ModifierEffectReplicateCurCellToSelectCell : ModifierEffectHandler
{
    public override async ValueTask OnConfirmed(ChessRogueDiceModifierInstance modifierInstance,
        ChessRogueInstance chessRogueInstance)
    {
        var types = modifierInstance.EffectConfig.Params.GetValueOrDefault("SourceType", "3").Split(";");

        foreach (var type in types)
        {
            var cells = chessRogueInstance.RogueCells.Where(x =>
                x.Value.BlockType == (RogueDLCBlockTypeEnum)int.Parse(type) && !x.Value.IsCollapsed());
            modifierInstance.SelectableCells.AddRange(cells.Select(x => x.Key));
        }

        await ValueTask.CompletedTask;
    }

    public override async ValueTask SelectModifierCell(ChessRogueDiceModifierInstance modifierInstance,
        ChessRogueInstance chessRogueInstance,
        int selectCellId)
    {
        await chessRogueInstance.Player.SendPacket(
            new PacketRogueModifierStageStartNotify(modifierInstance.SourceType));

        modifierInstance.SelectedCell = selectCellId;
        modifierInstance.IsConfirmed = true;
        var targetCell = chessRogueInstance.RogueCells[selectCellId];
        var curCell = chessRogueInstance.CurCell;

        targetCell.BlockType = curCell?.BlockType ?? RogueDLCBlockTypeEnum.Empty;

        // Send packet to update the cell
        await chessRogueInstance.Player.SendPacket(new PacketChessRogueCellUpdateNotify(targetCell,
            chessRogueInstance.CurBoardExcel?.ChessBoardID ?? 0, modifierInstance.SourceType,
            ChessRogueCellUpdateReason.Modifier));
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