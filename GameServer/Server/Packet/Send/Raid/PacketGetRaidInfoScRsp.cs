using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Raid;

public class PacketGetRaidInfoScRsp : BasePacket
{
    public PacketGetRaidInfoScRsp(PlayerInstance player) : base(CmdIds.GetRaidInfoScRsp)
    {
        var proto = new GetRaidInfoScRsp();

        foreach (var recordDict in player.RaidManager!.RaidData.RaidRecordDatas)
        foreach (var record in recordDict.Value)
            if (record.Value.Status == RaidStatus.Finish)
                proto.FinishedRaidInfoList.Add(new FinishedRaidInfo
                {
                    RaidId = (uint)record.Value.RaidId,
                    WorldLevel = (uint)record.Value.WorldLevel
                });

        SetData(proto);
    }
}