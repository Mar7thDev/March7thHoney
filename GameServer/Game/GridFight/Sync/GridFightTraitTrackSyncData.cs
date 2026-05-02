using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightTraitTrackSyncData(GridFightSrc src, GridFightBasicInfoPb info, uint groupId = 0, params uint[] syncParams) : BaseGridFightSyncData(src, groupId, syncParams)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            TraitTrackSyncInfo = new GridFightTraitTrackSyncInfo
            {
                TrackTraitIdList = { info.TrackingTraits }
            }
        };
    }
}