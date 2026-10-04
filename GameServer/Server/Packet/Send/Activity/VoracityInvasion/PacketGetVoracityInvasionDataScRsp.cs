using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.VoracityInvasion;

/// <summary>Voracity Invasion module data: retcode 0 and no typed entries (nothing seen or unlocked yet).</summary>
public class PacketGetVoracityInvasionDataScRsp : BasePacket
{
    public PacketGetVoracityInvasionDataScRsp() : base(CmdIds.GetVoracityInvasionDataScRsp)
    {
        SetData(new GetVoracityInvasionDataScRsp());
    }
}
