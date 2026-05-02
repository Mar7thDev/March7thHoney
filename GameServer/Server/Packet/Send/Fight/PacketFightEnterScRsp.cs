using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Send.Fight;

public class PacketFightEnterScRsp : BasePacket
{
    public PacketFightEnterScRsp(Retcode code) : base(CmdIds.FightEnterScRsp)
    {
        var proto = new FightEnterScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }

    public PacketFightEnterScRsp(ulong keySeed) : base(CmdIds.FightEnterScRsp)
    {
        var proto = new FightEnterScRsp
        {
            SecretKeySeed = keySeed,
            ServerTimestampMs = (ulong)Extensions.GetUnixMs()
        };

        SetData(proto);
    }
}