using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Activity.FinalityBattle;

public class PacketFinalityBattleSettleScNotify : BasePacket
{
    public PacketFinalityBattleSettleScNotify(BattleEndStatus endStatus, FinalityBattleLevelRecord record, bool isNewRecord)
        : base(CmdIds.FinalityBattleSettleScNotify)
    {
        SetData(new FinalityBattleSettleScNotify
        {
            EndStatus = endStatus,
            LevelRecord = record,
            IsNewRecord = isNewRecord
        });
    }
}
