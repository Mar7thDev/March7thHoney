using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RelicPreset;

public class PacketRelicPresetUpdatePlanScRsp : BasePacket
{
    /// <summary>
    ///     原样回带请求里改动的那一项（改名或换遗器），客户端靠它更新本地的方案缓存。
    /// </summary>
    public PacketRelicPresetUpdatePlanScRsp(RelicPresetUpdatePlanCsReq req, Retcode retcode = Retcode.RetSucc)
        : base(CmdIds.RelicPresetUpdatePlanScRsp)
    {
        var proto = new RelicPresetUpdatePlanScRsp
        {
            UniqueId = req.UniqueId,
            Retcode = (uint)retcode
        };

        if (req.RelicList != null) proto.RelicList = req.RelicList;
        else proto.Name = req.Name;

        SetData(proto);
    }
}
