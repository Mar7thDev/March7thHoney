using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.PendingAction;

public class GridFightTraitPendingAction(GridFightInstance inst, GridFightGameTraitEffectPb effect) : BaseGridFightPendingAction(inst)
{
    public GridFightGameTraitEffectPb Effect { get; set; } = effect;
    public override GridFightPendingAction ToProto()
    {
        return new GridFightPendingAction
        {
            QueuePosition = QueuePosition,
            TraitAction = new GridFightTraitActionInfo
            {
                EffectId = Effect.EffectId,
                TraitId = Effect.TraitId
            }
        };
    }
}