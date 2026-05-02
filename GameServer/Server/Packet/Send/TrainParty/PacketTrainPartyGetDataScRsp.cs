using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.TrainParty;

public class PacketTrainPartyGetDataScRsp : BasePacket
{
    public PacketTrainPartyGetDataScRsp(PlayerInstance player) : base(CmdIds.TrainPartyGetDataScRsp)
    {
        var proto = new TrainPartyGetDataScRsp
        {
            TrainPartyData = player.TrainPartyManager!.ToProto()
        };

        SetData(proto);
    }
}