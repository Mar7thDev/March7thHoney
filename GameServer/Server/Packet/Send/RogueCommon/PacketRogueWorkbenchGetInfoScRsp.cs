using March7thHoney.GameServer.Game.RogueTourn.Scene;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueCommon;

public class PacketRogueWorkbenchGetInfoScRsp : BasePacket
{
    public PacketRogueWorkbenchGetInfoScRsp(Retcode ret, RogueWorkbenchProp? prop) : base(
        CmdIds.RogueWorkbenchGetInfoScRsp)
    {
        var proto = new RogueWorkbenchGetInfoScRsp
        {
            Retcode = (uint)ret
        };

        if (prop != null)
            foreach (var rogueWorkbenchFunc in prop.WorkbenchFuncs)
                proto.FuncInfoMap.Add((uint)rogueWorkbenchFunc.FuncId, rogueWorkbenchFunc.ToProto());

        SetData(proto);
    }
}