namespace March7thHoney.Data.Custom;

/// <summary>
/// Runtime-only GridFight corrections that are not represented by the bundled resource schema.
/// This file is loaded independently from Resource.bin so deployments can adjust it without
/// rebuilding the resource cache.
/// </summary>
public sealed class GridFightRuntimeConfig
{
    public uint RotationPortalId { get; set; }
    public Dictionary<uint, List<uint>> ShopRarityWeightOverrides { get; set; } = [];
}
