using March7thHoney.Data;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Scene;

public class PacketGetEnteredSceneScRsp : BasePacket
{
    public PacketGetEnteredSceneScRsp(PlayerInstance player) : base(CmdIds.GetEnteredSceneScRsp)
    {
        var proto = new GetEnteredSceneScRsp();

        if (player.MissionEnabled)
        {
            // Story on: only the floors this player has actually entered (persisted by LoadScene). Reporting
            // every story-gated entrance as entered unlocked all map regions ahead of the missions.
            var entered = player.SceneData?.EnteredSceneData ?? [];
            var seen = new HashSet<(int, int)>();
            foreach (var (planeId, floors) in entered)
            foreach (var floorId in floors)
                if (seen.Add((planeId, floorId)))
                    proto.EnteredSceneInfoList.Add(new EnteredSceneInfo
                    {
                        PlaneId = (uint)planeId,
                        FloorId = (uint)floorId
                    });

            // The current scene always counts as entered, even on a save that predates the record.
            if (player.Data.PlaneId != 0 && player.Data.FloorId != 0 &&
                seen.Add((player.Data.PlaneId, player.Data.FloorId)))
            {
                player.RecordEnteredScene(player.Data.PlaneId, player.Data.FloorId);
                proto.EnteredSceneInfoList.Add(new EnteredSceneInfo
                {
                    PlaneId = (uint)player.Data.PlaneId,
                    FloorId = (uint)player.Data.FloorId
                });
            }
        }
        else
        {
            // Story off: every story-gated entrance reads as entered so the whole map is open (legacy behaviour).
            foreach (var excel in GameData.MapEntranceData.Values)
            {
                if (excel.FinishMainMissionList.Count == 0 && excel.FinishSubMissionList.Count == 0) continue;

                proto.EnteredSceneInfoList.Add(new EnteredSceneInfo
                {
                    FloorId = (uint)excel.FloorID,
                    PlaneId = (uint)excel.PlaneID
                });
            }
        }

        SetData(proto);
    }
}
