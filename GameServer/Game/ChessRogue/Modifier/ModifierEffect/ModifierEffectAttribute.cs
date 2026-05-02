using CastoriceHoney.Server.Enums.Rogue;

namespace CastoriceHoney.Server.GameServer.Game.ChessRogue.Modifier.ModifierEffect;

[AttributeUsage(AttributeTargets.Class)]
public class ModifierEffectAttribute(ModifierEffectTypeEnum effectType) : Attribute
{
    public ModifierEffectTypeEnum EffectType { get; } = effectType;
}