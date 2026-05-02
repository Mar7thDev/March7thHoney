using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.MarkChest;

public class PacketGetMarkChestScRsp : BasePacket
{
    public PacketGetMarkChestScRsp(PlayerInstance player) : base(CmdIds.GetMarkChestScRsp)
    {
        var proto = new GetMarkChestScRsp
        {
            MarkChestFuncInfo =
            {
                player.SceneData!.MarkedChestData.Select(x => new MarkChestFuncInfo
                {
                    FuncId = (uint)x.Key,
                    MarkChestInfoList = { x.Value.Select(y => y.ToProto()) }
                })
            }
        };

        SetData(proto);
    }
}