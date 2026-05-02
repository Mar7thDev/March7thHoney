using CastoriceHoney.Server.GameServer.Game.RogueTourn.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;

public class PacketRogueWorkbenchHandleFuncScRsp : BasePacket
{
    public PacketRogueWorkbenchHandleFuncScRsp(Retcode retcode, uint funcId, RogueWorkbenchFunc? func) : base(
        CmdIds.RogueWorkbenchHandleFuncScRsp)
    {
        var proto = new RogueWorkbenchHandleFuncScRsp
        {
            Retcode = (uint)retcode,
            FuncId = funcId
        };

        if (func != null) proto.TargetFuncInfo = func.ToProto();

        SetData(proto);
    }
}