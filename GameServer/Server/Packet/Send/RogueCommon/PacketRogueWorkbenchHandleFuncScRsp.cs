using March7thHoney.GameServer.Game.RogueTourn.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueCommon;

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