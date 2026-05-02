using CastoriceHoney.Server.GameServer.Game.GridFight;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.GridFight;

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