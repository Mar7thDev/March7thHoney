using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.PamSkin;

public class PacketGetPamSkinDataScRsp : BasePacket
{
    public PacketGetPamSkinDataScRsp(PlayerInstance player) : base(CmdIds.GetPamSkinDataScRsp)
    {
        var proto = new GetPamSkinDataScRsp
        {
            CurSkin = (uint)player.Data.CurrentPamSkin,
            UnlockSkinList = { GameData.PamSkinConfigData.Select(x => (uint)x.Key) }
        };

        SetData(proto);
    }
}