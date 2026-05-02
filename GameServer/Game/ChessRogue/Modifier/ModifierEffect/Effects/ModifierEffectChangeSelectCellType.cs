using March7thHoney.Enums.Rogue;
using March7thHoney.GameServer.Game.Battle;
using March7thHoney.GameServer.Server.Packet.Send.ChessRogue;
using March7thHoney.GameServer.Server.Packet.Send.RogueModifier;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.ChessRogue.Modifier.ModifierEffect.Effects;

[ModifierEffect(ModifierEffectTypeEnum.ChangeSelectCellType)]
public class ModifierEffectChangeSelectCellType : ModifierEffectHandler
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
        var cell = chessRogueInstance.RogueCells[selectCellId];

        modifierInstance.SelectedCell = selectCellId;
        modifierInstance.IsConfirmed = true;

        await chessRogueInstance.Player.SendPacket(
            new PacketRogueModifierStageStartNotify(modifierInstance.SourceType));

        var types = modifierInstance.EffectConfig.Params.GetValueOrDefault("TargetType", "3").Split(";");
        var targetType = types.Select(x => (RogueDLCBlockTypeEnum)int.Parse(x)).ToList().RandomElement();

        cell.BlockType = targetType;

        await chessRogueInstance.Player.SendPacket(new PacketChessRogueCellUpdateNotify(cell,
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