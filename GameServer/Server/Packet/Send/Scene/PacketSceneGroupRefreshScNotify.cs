using March7thHoney.GameServer.Game.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Scene;

public class PacketSceneGroupRefreshScNotify : BasePacket
{
    public PacketSceneGroupRefreshScNotify(PlayerInstance player, List<BaseGameEntity>? addEntity = null,
        List<BaseGameEntity>? removeEntity = null,
        SceneGroupRefreshType refreshType = SceneGroupRefreshType.Afibfmafncc)
        : base(CmdIds.SceneGroupRefreshScNotify)
    {
        // Entity deltas update an existing group. LOADED requires the full-group overload below.
        if (refreshType == SceneGroupRefreshType.Loaded)
            refreshType = SceneGroupRefreshType.Afibfmafncc;

        var proto = new SceneGroupRefreshScNotify
        {
            FloorId = (uint)player.Data.FloorId,
            DimensionId = (uint)player.SceneInstance!.ResolveDimensionId()
        };
        Dictionary<int, GroupRefreshInfo> refreshInfo = [];

        foreach (var e in removeEntity ?? [])
        {
            var group = new GroupRefreshInfo
            {
                GroupId = (uint)e.GroupId,
                RefreshType = refreshType
            };
            group.RefreshEntity.Add(new SceneEntityRefreshInfo
            {
                DeleteEntity = (uint)e.EntityId
            });

            if (refreshInfo.TryGetValue(e.GroupId, out var value))
                value.RefreshEntity.AddRange(group.RefreshEntity);
            else
                refreshInfo[e.GroupId] = group;
        }

        foreach (var e in addEntity ?? [])
        {
            var group = new GroupRefreshInfo
            {
                GroupId = (uint)e.GroupId,
                RefreshType = refreshType
            };
            group.RefreshEntity.Add(new SceneEntityRefreshInfo
            {
                AddEntity = e.ToProto()
            });

            if (refreshInfo.TryGetValue(e.GroupId, out var value))
                value.RefreshEntity.AddRange(group.RefreshEntity);
            else
                refreshInfo[e.GroupId] = group;
        }

        proto.GroupRefreshList.AddRange(refreshInfo.Values);

        SetData(proto);
    }

    public PacketSceneGroupRefreshScNotify(PlayerInstance player, BaseGameEntity? addEntity = null,
        BaseGameEntity? removeEntity = null,
        SceneGroupRefreshType refreshType = SceneGroupRefreshType.Afibfmafncc) :
        this(player, addEntity == null ? [] : [addEntity], removeEntity == null ? [] : [removeEntity], refreshType)
    {
    }

    public PacketSceneGroupRefreshScNotify(SceneInstance scene, List<int> loadedGroupIds,
        List<int>? unloadedGroupIds = null) : base(CmdIds.SceneGroupRefreshScNotify)
    {
        var proto = new SceneGroupRefreshScNotify
        {
            FloorId = (uint)scene.FloorId,
            DimensionId = (uint)scene.ResolveDimensionId()
        };
        foreach (var groupId in unloadedGroupIds?.Distinct() ?? [])
        {
            proto.GroupRefreshList.Add(new GroupRefreshInfo
            {
                GroupId = (uint)groupId,
                RefreshType = SceneGroupRefreshType.Unload
            });
            scene.ClientSceneSnapshotGroupIds.Remove(groupId);
        }
        foreach (var groupId in loadedGroupIds.Distinct())
        {
            proto.GroupRefreshList.Add(new GroupRefreshInfo
            {
                GroupId = (uint)groupId,
                RefreshType = SceneGroupRefreshType.Loaded,
                GroupInfo = scene.BuildEntityGroupInfo(groupId)
            });
            scene.ClientSceneSnapshotGroupIds.Add(groupId);
        }
        SetData(proto);
    }

    public PacketSceneGroupRefreshScNotify(SceneInstance scene, List<GroupPropertyRefreshData> refreshDataList) : base(
        CmdIds.SceneGroupRefreshScNotify)
    {
        var proto = new SceneGroupRefreshScNotify
        {
            FloorId = (uint)scene.FloorId,
            DimensionId = (uint)scene.ResolveDimensionId()
        };

        Dictionary<int, List<GroupPropertyRefreshData>> refreshDataDict = [];
        foreach (var data in refreshDataList)
        {
            if (!refreshDataDict.TryGetValue(data.GroupId, out var list))
            {
                list = [];
                refreshDataDict[data.GroupId] = list;
            }

            list.Add(data);
        }

        foreach (var (groupId, dataList) in refreshDataDict)
        {
            var group = new GroupRefreshInfo
            {
                GroupId = (uint)groupId,
                RefreshType = SceneGroupRefreshType.Afibfmafncc
            };

            foreach (var data in dataList)
                group.CDMKBCHLPAB.Add(new AKEKCKDIHLA
                {
                    NewValue = data.NewValue,
                    OldValue = data.OldValue,
                    PropertyName = data.PropertyName
                });

            proto.GroupRefreshList.Add(group);
        }

        SetData(proto);
    }
}

public record GroupPropertyRefreshData(int GroupId, string PropertyName, int OldValue, int NewValue);

