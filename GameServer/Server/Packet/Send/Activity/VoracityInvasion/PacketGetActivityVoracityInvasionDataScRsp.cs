using March7thHoney.Kcp;
using March7thHoney.Proto;
using March7thHoney.Util;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.VoracityInvasion;

/// <summary>
///     Voracity Invasion progress. Field identities follow the client's VoracityInvasionWorldActivity, matched by
///     their unique types: uint64 LOPGJCDNLJF = world CurProgress, double JKFLIHPNDHE = world DisplayPercentage,
///     int64 LBEGAHHDKBE = LastUpdateTime (seconds), score = personal CurProgress. The 4.6.51 client ships an empty
///     ActivityVoracityInvasionPro table, so there is no world progress to report; the update time keeps the
///     client from treating the data as expired.
/// </summary>
public class PacketGetActivityVoracityInvasionDataScRsp : BasePacket
{
    public PacketGetActivityVoracityInvasionDataScRsp() : base(CmdIds.GetActivityVoracityInvasionDataScRsp)
    {
        SetData(new GetActivityVoracityInvasionDataScRsp
        {
            LOPGJCDNLJF = 0,
            JKFLIHPNDHE = 0,
            LBEGAHHDKBE = Extensions.GetUnixSec(),
            Score = 0,
        });
    }
}
