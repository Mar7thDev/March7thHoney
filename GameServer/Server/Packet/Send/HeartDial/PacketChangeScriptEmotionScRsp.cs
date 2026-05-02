using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.HeartDial;

public class PacketChangeScriptEmotionScRsp : BasePacket
{
    public PacketChangeScriptEmotionScRsp(uint scriptId, HeartDialEmotionType emotion) : base(
        CmdIds.ChangeScriptEmotionScRsp)
    {
        var proto = new ChangeScriptEmotionScRsp
        {
            ScriptId = scriptId,
            EmotionType = emotion
        };

        SetData(proto);
    }
}