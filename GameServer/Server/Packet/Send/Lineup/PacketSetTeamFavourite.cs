using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Lineup;

public class PacketSetTeamFavourite : BasePacket
{
    public PacketSetTeamFavourite(uint index, bool isFavourite, Retcode retcode = Retcode.RetSucc)
        : base(CmdIds.SetTeamFavourite)
    {
        var proto = new SetTeamFavourite
        {
            Index = index,
            IsFavourite = isFavourite,
            Retcode = (uint)retcode
        };

        SetData(proto);
    }
}
