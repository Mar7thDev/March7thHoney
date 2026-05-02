using CastoriceHoney.Server.GameServer.Game.RogueTourn.Scene;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;

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