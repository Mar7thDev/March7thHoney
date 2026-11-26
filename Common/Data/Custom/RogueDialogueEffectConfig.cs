namespace March7thHoney.Data.Custom;

/// <summary>
///     What a classic Simulated Universe Occurrence option pays, from Config/Custom/RogueDialogueEffects.json. The
///     DialogueEvent table that held these effects is absent from every 4.x dump and the RogueDialogueOption rows carry
///     no parameters, so the payouts come from the option texts and the official captures. An option with no entry
///     pays nothing (for example "Do nothing"). Loaded from Config/Custom on every boot and not stored in Resource.bin.
/// </summary>
public class RogueDialogueEffectConfig
{
    public List<RogueDialogueEffect> Effects { get; set; } = [];

    public RogueDialogueEffect? Find(int npcId, int optionId)
    {
        return Effects.FirstOrDefault(effect => effect.NpcId == npcId && effect.OptionId == optionId);
    }
}

public class RogueDialogueEffect
{
    /// <summary>The RogueNPC event the option belongs to.</summary>
    public int NpcId { get; set; }

    public int OptionId { get; set; }

    /// <summary>Cosmic Fragments the option pays.</summary>
    public int Fragments { get; set; }

    /// <summary>The RogueBuffGroup a three-card blessing draft is dealt from; 0 for no draft.</summary>
    public int BuffGroup { get; set; }
}
