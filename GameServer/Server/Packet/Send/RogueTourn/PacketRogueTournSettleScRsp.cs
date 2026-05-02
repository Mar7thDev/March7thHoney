using March7thHoney.Data;
using March7thHoney.Data.Excel;
using March7thHoney.GameServer.Game.RogueTourn;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.RogueTourn;

public class PacketRogueTournSettleScRsp : BasePacket
{
    public PacketRogueTournSettleScRsp(RogueTournInstance instance) : base(CmdIds.RogueTournSettleScRsp)
    {
        var maxDivision = GameData.RogueTournDivisionData.Values.MaxBy(x => x.DivisionLevel) ?? new RogueTournDivisionExcel();

        var proto = new RogueTournSettleScRsp
        {
            RogueTournCurSceneInfo = instance.ToCurSceneInfo(),
            TournFinishInfo = new RogueTournFinishInfo
            {
                RogueTournCurInfo = instance.ToProto(),
                RogueLineupInfo = instance.Player.LineupManager!.GetCurLineup()!.ToProto(),
                CJCOJAMLEEL = new(),
                NewDivisionInfo = new RogueTournDivisionInfo
                {
                    DivisionLevel = (uint)maxDivision.DivisionLevel,
                    DivisionProgress = (uint)maxDivision.DivisionProgress
                },
                GCGLNKFDKKN = new(),
                KGCIAIAFIBE = new(),
                PFOEPFPHFNJ = new()
            }
        };

        SetData(proto);
    }

    public PacketRogueTournSettleScRsp() : base(CmdIds.RogueTournSettleScRsp)
    {
        var proto = new RogueTournSettleScRsp
        {
            Retcode = 1
        };

        SetData(proto);
    }
}