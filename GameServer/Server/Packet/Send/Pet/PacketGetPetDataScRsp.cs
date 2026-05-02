using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Pet;

public class PacketGetPetDataScRsp : BasePacket
{
    public PacketGetPetDataScRsp(PlayerInstance player) : base(CmdIds.GetPetDataScRsp)
    {
        var proto = new GetPetDataScRsp
        {
            CurPetId = (uint)player.Data.Pet
        };

        foreach (var pet in GameData.PetData.Values) proto.UnlockedPetId.Add((uint)pet.PetID);

        SetData(proto);
    }
}