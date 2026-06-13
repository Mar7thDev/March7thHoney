namespace March7thHoney.GameServer.Game.Battle;

// Lifecycle of a technique (秘技) cast intent recorded in the scene and consumed at the next battle.
//  Cast      = flagged and live (the old "present in the HashSet").
//  Abandoned = the caster un-flagged via a "...MazeSkill_Out" transform-exit (e.g. Himeko Nova leaving
//              mech). Recorded as an explicit transition instead of removing the entry; the reader treats
//              anything other than Cast as not-injectable, reproducing the old Remove exactly.
//  Committed = transient marker that the entering battle was triggered by this avatar's own SkillIndex==1
//              technique-attack. Cosmetic/auditing only — it never gates injection.
public enum TechniqueCastState
{
    Cast,
    Committed,
    Abandoned
}

// Per-avatar technique archetype, computed once at cast from the avatar's SkillMaze maze-buff UseType.
// Only Attack-vs-not is load-bearing (Attack is commit-gated at battle); Field/Summon/Buff are advisory.
public enum TechniqueKind
{
    Attack,
    Field,
    Summon,
    Buff
}

// Replaces the bare base-avatar-id the scene used to keep in a HashSet. Carries the cast intent so the
// archetype/commit-eligibility is computed once at cast instead of re-derived at battle entry. The
// per-buff UseType check in BattleManager.StartBattle stays the source of truth for injection — an avatar
// can hold multiple SkillMaze buffs with mixed UseTypes (Firefly 131001 + 131002) — and Kind is a
// precomputed convenience that agrees with it for the Attack case.
public sealed class TechniqueCastInfo
{
    public int BaseAvatarId { get; set; }
    public int SkillIndex { get; set; } = 1;
    public string MazeAbilityStr { get; set; } = "";
    public TechniqueCastState State { get; set; } = TechniqueCastState.Cast;
    public TechniqueKind Kind { get; set; } = TechniqueKind.Field;
}
