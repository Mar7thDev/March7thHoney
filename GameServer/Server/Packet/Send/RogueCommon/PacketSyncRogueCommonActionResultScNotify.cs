using CastoriceHoney.Server.Enums.Rogue;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.RogueCommon;

public class PacketSyncRogueCommonActionResultScNotify : BasePacket
{
    public PacketSyncRogueCommonActionResultScNotify(RogueSubModeEnum rogueSubmode, RogueCommonActionResult result,
        RogueCommonActionResultDisplayType displayType = RogueCommonActionResultDisplayType.None) : base(
        CmdIds.SyncRogueCommonActionResultScNotify)
    {
        var proto = new SyncRogueCommonActionResultScNotify
        {
            RogueSubMode = (uint)rogueSubmode,
            DisplayType = displayType
        };

        proto.ActionResultList.Add(result);

        SetData(proto);
    }

    public PacketSyncRogueCommonActionResultScNotify(RogueSubModeEnum rogueSubmode,
        List<RogueCommonActionResult> results,
        RogueCommonActionResultDisplayType displayType = RogueCommonActionResultDisplayType.None) : base(
        CmdIds.SyncRogueCommonActionResultScNotify)
    {
        var proto = new SyncRogueCommonActionResultScNotify
        {
            RogueSubMode = (uint)rogueSubmode,
            DisplayType = displayType
        };

        proto.ActionResultList.AddRange(results);

        SetData(proto);
    }
}