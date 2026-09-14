using March7thHoney.Data;
using March7thHoney.Enums.Scene;
using March7thHoney.GameServer.Server.Packet.Send.Activity.B51Racing;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.GameServer.Server.Packet.Send.Lineup;
using March7thHoney.Util;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Activity.B51Racing;

public sealed partial class B51RacingManager
{
    private RaidSceneData? _raidScene;

    public bool IsRacingRaid(uint raidID) => raidID != 0 && raidID == Resources.RaidID;

    public SceneInfo? GetRaidScene()
    {
        lock (_stateLock) return _raidScene?.SceneInfo.Clone();
    }

    public LineupInfo? GetRaidLineup()
    {
        lock (_stateLock) return _raidScene?.Lineup.Clone();
    }

    public StartRaidScRsp EnterRaid(StartRaidCsReq request)
    {
        lock (_stateLock)
        {
            if (!IsRacingRaid(request.RaidId) || _race == null || Player.SceneInstance == null ||
                Player.RaidManager?.Data.CurRaidId > 0 || !GameData.MapEntranceData.TryGetValue(Resources.RaidID, out var entrance) ||
                !GameData.MazePlaneData.TryGetValue(entrance.PlaneID, out var plane) || plane.PlaneType != PlaneTypeEnum.Raid)
                return new StartRaidScRsp { Retcode = (uint)Retcode.RetReqParaInvalid };
            if (_raidScene != null)
            {
                _race.EnteredRaid = true;
                return new StartRaidScRsp { Scene = _raidScene.Clone() };
            }
            GameData.GetFloorInfo(entrance.PlaneID, entrance.FloorID, out var floor);
            var anchor = floor?.GetAnchorInfo(entrance.StartGroupID != 0 ? entrance.StartGroupID : floor.StartGroupID,
                entrance.StartAnchorID != 0 ? entrance.StartAnchorID : floor.StartAnchorID);
            var lineup = Player.LineupManager?.GetCurLineup()?.ToProto();
            if (anchor == null || lineup == null || lineup.AvatarList.Count == 0)
                return new StartRaidScRsp { Retcode = (uint)Retcode.RetFail };

            var scene = new SceneInfo
            {
                PlaneId = (uint)entrance.PlaneID, FloorId = (uint)entrance.FloorID, EntryId = (uint)entrance.ID,
                WorldId = (uint)plane.WorldID, GameModeType = (uint)plane.PlaneType, ClientPosVersion = 1,
                DimensionId = (uint)(floor!.DimensionList.FirstOrDefault()?.ID ?? 0),
                SceneIdentifier = new SceneIdentifier { FloorId = (uint)entrance.FloorID },
                SceneMissionInfo = new MissionStatusBySceneInfo()
            };
            var actors = new SceneEntityGroupInfo();
            foreach (var avatar in Player.SceneInstance.AvatarInfo.Values)
            {
                var actor = avatar.ToProto();
                actor.Motion = new MotionInfo { Pos = anchor.ToPositionProto().ToProto(), Rot = anchor.ToRotationProto().ToProto() };
                actors.EntityList.Add(actor);
            }
            if (actors.EntityList.Count == 0) return new StartRaidScRsp { Retcode = (uint)Retcode.RetFail };
            scene.EntityGroupList.Add(actors);
            scene.LeaderEntityId = actors.EntityList.Any(actor => actor.EntityId == Player.SceneInstance.LeaderEntityId)
                ? (uint)Player.SceneInstance.LeaderEntityId : actors.EntityList[0].EntityId;
            lineup.IsVirtual = true;
            lineup.Name = string.Empty;
            lineup.Index = 0;
            lineup.PlaneId = scene.PlaneId;
            _raidScene = new RaidSceneData { SceneInfo = scene, Lineup = lineup, RaidId = request.RaidId };
            _race.EnteredRaid = true;
            return new StartRaidScRsp { Scene = _raidScene.Clone() };
        }
    }

    public bool HandleSceneMove(SceneEntityMoveCsReq request)
    {
        lock (_stateLock)
        {
            if (_raidScene == null) return request.EntryId == Resources.RaidID;
            if (request.EntryId != _raidScene.SceneInfo.EntryId) return true;
            foreach (var motion in request.EntityMotionList)
            {
                var actor = _raidScene.SceneInfo.EntityGroupList.SelectMany(group => group.EntityList)
                    .FirstOrDefault(entity => entity.EntityId == motion.EntityId && entity.Actor != null);
                if (actor != null && motion.Motion != null) actor.Motion = motion.Motion.Clone();
            }
            return true;
        }
    }

    public async ValueTask LeaveRaid()
    {
        uint planeID;
        lock (_stateLock)
        {
            planeID = _raidScene?.SceneInfo.PlaneId ?? 0;
            _raidScene = null;
            _race = null;
        }
        if (planeID == 0 || Player.SceneInstance == null) return;
        await Player.SendPacket(new PacketVirtualLineupDestroyNotify((int)planeID));
        await Player.SendPacket(new PacketEnterSceneByServerScNotify(Player.SceneInstance));
    }

    public async ValueTask SendRaidStart(StartRaidScRsp response)
    {
        if (response.Retcode == 0)
            await Player.SendPacket(new PacketB51RacingData(CmdIds.RaidInfoNotify,
                new RaidInfoNotify { RaidId = response.Scene.RaidId, Status = RaidStatus.Doing }));
    }
}
