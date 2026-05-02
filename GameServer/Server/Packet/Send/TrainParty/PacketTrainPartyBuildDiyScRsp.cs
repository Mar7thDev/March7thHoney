using CastoriceHoney.Server.Database.TrainParty;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.TrainParty;

public class PacketTrainPartyBuildDiyScRsp : BasePacket
{
    public PacketTrainPartyBuildDiyScRsp(TrainAreaInfo? area) : base(CmdIds.TrainPartyBuildDiyScRsp)
    {
        var proto = area == null
            ? new TrainPartyBuildDiyScRsp
            {
                Retcode = (uint)Retcode.RetTrainPartyDiyTagNotMatch
            }
            : new TrainPartyBuildDiyScRsp
            {
                AreaId = (uint)area.AreaId,
                DynamicInfo =
                {
                    area.DynamicInfo.Select(x => new AreaDynamicInfo
                    {
                        DiceSlotId = (uint)x.Key,
                        DiyDynamicId = (uint)x.Value
                    })
                }
                //FFLPKLLDHLM = {new CIKOHJNAGON  // TODO update
                //{
                //    AreaId = (uint)area.AreaId,
                //    KCJBMKJLFBA = area.DynamicInfo.Select(x => new AreaDynamicInfo
                //    {
                //        DiceSlotId = (uint)x.Key,
                //        DiyDynamicId = (uint)x.Value
                //    }).First(),
                //}}
            };

        SetData(proto);
    }
}