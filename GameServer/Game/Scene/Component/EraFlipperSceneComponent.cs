using March7thHoney.GameServer.Server.Packet.Send.EraFlipper;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Scene.Component;

public class EraFlipperSceneComponent(SceneInstance scene) : BaseSceneComponent(scene)
{
    public int CurRegionId { get; set; }
    public int RegionState { get; set; }

    public override async ValueTask Initialize()
    {
        CurRegionId = SceneInst.Player.SceneData!.EraFlipperData.CurRegionId;
        if (CurRegionId != 0)
        {
            RegionState = SceneInst.Player.SceneData!.EraFlipperData.RegionState.GetValueOrDefault(CurRegionId, 0);

            await SceneInst.Player.SendPacket(
                new PacketEraFlipperDataChangeScNotify(SceneInst.FloorId, CurRegionId, RegionState));
        }
    }

    public void EnterEraFlipperRegion(int regionId, int state)
    {
        CurRegionId = regionId;
        RegionState = state;

        SceneInst.Player.SceneData!.EraFlipperData.CurRegionId = regionId;
        SceneInst.Player.SceneData!.EraFlipperData.RegionState[CurRegionId] = state;
    }

    public void LeaveFlipperRegion()
    {
        CurRegionId = 0;
        RegionState = 0;

        SceneInst.Player.SceneData!.EraFlipperData.CurRegionId = 0;
    }

    public void ChangeEraFlipperState(int state)
    {
        if (CurRegionId == 0) return;
        RegionState = state;

        // save
        SceneInst.Player.SceneData!.EraFlipperData.RegionState[CurRegionId] = state;
    }

    public void ChangeEraFlipperStates(System.Collections.IEnumerable dataList)
    {
        foreach (var data in dataList)
        {
            var t = data.GetType();
            var regionIdObj = t.GetProperty("EraFlipperRegionId")?.GetValue(data);
            var stateObj = t.GetProperty("State")?.GetValue(data);
            if (regionIdObj == null || stateObj == null) continue;

            var regionId = (int)Convert.ToUInt32(regionIdObj);
            var state = (int)Convert.ToUInt32(stateObj);

            SceneInst.Player.SceneData!.EraFlipperData.RegionState[regionId] = state;
            if (regionId == CurRegionId) RegionState = state;
        }
    }
}
