using March7thHoney.GameServer.Game.GridFight;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightStartGamePlayScRsp : BasePacket
{
    public PacketGridFightStartGamePlayScRsp(Retcode ret, GridFightInstance? inst) : base(CmdIds.GridFightStartGamePlayScRsp)
    {
        var rsp = new GridFightStartGamePlayScRsp
        {
            Retcode = (uint)ret
        };

        if (inst != null)
            rsp.FightCurrentInfo = inst.ToProto();

        SetData(rsp);
    }
}