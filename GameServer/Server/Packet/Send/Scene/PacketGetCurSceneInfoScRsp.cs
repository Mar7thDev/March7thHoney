using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Scene;

public class PacketGetCurSceneInfoScRsp : BasePacket
{
    public PacketGetCurSceneInfoScRsp(PlayerInstance player) : base(CmdIds.GetCurSceneInfoScRsp)
    {
        var scene = player.SceneInstance!;
        var proto = new GetCurSceneInfoScRsp
        {
            Scene = player.B51RacingManager?.GetRaidScene() ?? scene.ToProto()
        };
        scene.MarkClientSceneSnapshotSynced();

        SetData(proto);
    }
}
