using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;

public class PacketUpdateFloorSavedValueNotify : BasePacket
{
    public PacketUpdateFloorSavedValueNotify(string name, int savedValue, PlayerInstance player) : base(
        CmdIds.UpdateFloorSavedValueNotify)
    {
        var proto = new UpdateFloorSavedValueNotify
        {
            FloorId = (uint)player.SceneInstance!.FloorId,
            PlaneId = (uint)player.SceneInstance!.PlaneId
        };

        proto.SavedValue.Add(name, savedValue);

        SetData(proto);
    }

    public PacketUpdateFloorSavedValueNotify(Dictionary<string, int> update, PlayerInstance player) : base(
        CmdIds.UpdateFloorSavedValueNotify)
    {
        var proto = new UpdateFloorSavedValueNotify
        {
            FloorId = (uint)player.SceneInstance!.FloorId,
            PlaneId = (uint)player.SceneInstance!.PlaneId
        };

        foreach (var i in update) proto.SavedValue.Add(i.Key, i.Value);

        SetData(proto);
    }
}