using March7thHoney.GameServer.Game.GridFight.Component;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.GridFight.Sync;

public class GridFightEliteBranchSyncData(GridFightSrc src, GridFightGameSectionInfo section, uint groupId = 0, params uint[] syncParams) : BaseGridFightSyncData(src, groupId, syncParams)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            EliteBranchSyncInfo = new GridFightEliteBranchSyncInfo
            {
                EliteBranchId = section.BranchId
            }
        };
    }
}