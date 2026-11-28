using March7thHoney.Enums.Mission;
using March7thHoney.GameServer.Game.Player;
using March7thHoney.GameServer.Server.Packet.Send.Scene;

namespace March7thHoney.GameServer.Game.Mission.FinishAction.Handler;

[MissionFinishAction(FinishActionTypeEnum.SetFloorSavedValue)]
public class MissionHandlerSetFloorSavedValue : MissionFinishActionHandler
{
    public override async ValueTask OnHandle(List<int> @params, List<string> paramString, PlayerInstance player)
    {
        _ = int.TryParse(paramString[0], out var plane);
        _ = int.TryParse(paramString[1], out var floor);
        player.SceneData!.FloorSavedData.TryGetValue(floor, out var value);
        if (value == null)
        {
            value = [];
            player.SceneData.FloorSavedData[floor] = value;
        }

        value[paramString[2]] = int.Parse(paramString[3]); // ParamString[2] is the key
        March7thHoney.Database.DatabaseHelper.MarkDirty(player.Uid);
        // The value belongs to the floor named by the action. Notifying while the player stands on
        // another floor makes the client apply it to the scene they are in.
        if (player.Data.FloorId == floor || player.SceneInstance?.FloorId == floor)
            await player.SendPacket(
                new PacketUpdateFloorSavedValueNotify(paramString[2], int.Parse(paramString[3]), player));

        player.TaskManager?.SceneTaskTrigger.TriggerFloor(plane, floor, player.SceneInstance);
    }
}
