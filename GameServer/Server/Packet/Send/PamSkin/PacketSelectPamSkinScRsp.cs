using March7thHoney.GameServer.Game.Player;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.PamSkin;

public class PacketSelectPamSkinScRsp : BasePacket
{
    public PacketSelectPamSkinScRsp(PlayerInstance player, int setSkinId, Retcode retcode = Retcode.RetSucc)
        : base(CmdIds.SelectPamSkinScRsp)
    {
        var proto = new SelectPamSkinScRsp
        {
            CurSkin = (uint)player.Data.CurrentPamSkin,
            SetSkin = (uint)setSkinId,
            Retcode = (uint)retcode
        };

        SetData(proto);
    }
}
