using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightTraitSyncData(GridFightSrc src, GridFightGameTraitEffectPb trait, uint groupId = 0, params uint[] syncParams) : BaseGridFightSyncData(src, groupId, syncParams)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            TraitSyncInfo = trait.ToSyncInfo()
        };
    }
}