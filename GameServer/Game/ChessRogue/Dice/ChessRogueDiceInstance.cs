using March7thHoney.Data;
using March7thHoney.Data.Custom;
using March7thHoney.Database.ChessRogue;
using March7thHoney.GameServer.Game.ChessRogue.Modifier;
using March7thHoney.GameServer.Server.Packet.Send.RogueModifier;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Game.ChessRogue.Dice;

public class ChessRogueDiceInstance(ChessRogueInstance instance, ChessRogueNousDiceData diceData)
{
    public int CheatTimes { get; set; } = 1;

    public int CurSurfaceId { get; set; }
    public ChessRogueNousDiceData DiceData { get; set; } = diceData;

    public ChessRogueDiceStatus DiceStatus { get; set; } = ChessRogueDiceStatus.ChessRogueDiceIdle;
    public ChessRogueInstance Instance { get; set; } = instance;
    public int RerollTimes { get; set; } = 1;

    public ChessRogueDiceSurfaceEffectConfig? CurrentSurfaceEffectConfig =>
        GameData.ChessRogueDiceSurfaceEffectData.GetValueOrDefault(CurSurfaceId);

    public ChessRogueDiceModifierInstance? Modifier { get; set; }

    public void RollDice()
    {
        CurSurfaceId = DiceData.Surfaces.ToList().RandomElement().Value;
        DiceStatus = ChessRogueDiceStatus.ChessRogueDiceRolled;
    }

    public async ValueTask ConfirmDice()
    {
        DiceStatus = ChessRogueDiceStatus.ChessRogueDiceConfirmed;

        if (CurrentSurfaceEffectConfig == null) return;

        if (Modifier != null) // Remove the previous modifier
            await Instance.Player.SendPacket(new PacketRogueModifierDelNotify(Modifier));

        Modifier = new ChessRogueDiceModifierInstance(Instance.CurModifierId++,
            CurrentSurfaceEffectConfig.ContentEffects.First());

        await Modifier.OnConfirmed(Instance);

        await Instance.Player.SendPacket(new PacketRogueModifierAddNotify(Modifier));
    }


    public ChessRogueDiceInfo ToProto()
    {
        var index = DiceData.Surfaces.ToList().FindIndex(x => x.Value == CurSurfaceId) + 1;
        return new ChessRogueDiceInfo
        {
            GameBranchId = (uint)DiceData.BranchId,
            Dice = DiceData.ToProto(),
            DiceStatus = DiceStatus,
            CurSurfaceId = (uint)CurSurfaceId,
            CheatTimes = (uint)CheatTimes,
            RerollTimes = (uint)RerollTimes,
            GameDiceBranchId = (uint)DiceData.BranchId,
            DiceType = ChessRogueDiceType.ChessRogueDiceEditable,
            FNEIDJIMJPH = true,
            CurSurfaceSlotId = (uint)(index > 0 ? index : 0),
            CanRerollDice = RerollTimes > 0,
            RogueModifider = Modifier?.ToProto() ?? new RogueModifier(),
            EDPHLDEGJLM = new EENDHPKPFLP()
        };
    }
}