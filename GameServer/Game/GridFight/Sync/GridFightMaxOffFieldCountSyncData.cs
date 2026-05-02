using March7thHoney.Proto;
using March7thHoney.Proto.ServerSide;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightMaxOffFieldCountSyncData(GridFightSrc src, GridFightBasicInfoPb info, uint groupId = 0, params uint[] syncParams) : BaseGridFightSyncData(src, groupId, syncParams)
{
    public GridFightBasicInfoPb Info { get; set; } = info.Clone();
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            GridFightOffFieldMaxCount = Math.Min(9, Info.OffFieldAvatarNum)
        };
    }
}