using March7thHoney.Enums.Rogue;

namespace March7thHoney.GameServer.Game.ChessRogue.Modifier.ModifierEffect;

[AttributeUsage(AttributeTargets.Class)]
public class ModifierEffectAttribute(ModifierEffectTypeEnum effectType) : Attribute
{
    public ModifierEffectTypeEnum EffectType { get; } = effectType;
}