using CastoriceHoney.Server.GameServer.Game.GridFight.Component;
using CastoriceHoney.Server.Proto;
using CastoriceHoney.Server.Proto.ServerSide;

namespace CastoriceHoney.Server.GameServer.Game.GridFight.Sync;

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