using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.HeartDial;

public class PacketSubmitEmotionItemScRsp : BasePacket
{
    public PacketSubmitEmotionItemScRsp(uint scriptId) : base(CmdIds.SubmitEmotionItemScRsp)
    {
        var proto = new SubmitEmotionItemScRsp
        {
            ScriptId = scriptId
        };

        SetData(proto);
    }
}