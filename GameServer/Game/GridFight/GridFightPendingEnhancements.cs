using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight;

public sealed partial class GridFightSession
{
    public bool HasPendingSelection => EquipmentRecommendation != null || HeadPlayerPending != null || FateDivinerPending != null;

    public (bool TraitActivated, bool PendingStarted) PrepareNextHeadPlayerEnhancement() =>
        RefreshHeadPlayerAfterRoleStateChange();
}

public sealed partial class GridFightManager
{
    private void AppendNextTraitEnhancement(GridFightSyncUpdateResultScNotify notify)
    {
        if (Session == null) return;
        var head = Session.PrepareNextHeadPlayerEnhancement();
        if (head.TraitActivated) AppendSilverWolfTraitActivationResults(notify);
        if (head.PendingStarted) AppendHeadPlayerPendingResult(notify);
        AppendFateDivinerInitialActivationResults(notify);
    }
}
