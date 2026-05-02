using March7thHoney.Enums.Fight;
using March7thHoney.GameServer.Game.MultiPlayer.MarbleGame;
using March7thHoney.GameServer.Game.MultiPlayer.MarbleGame.Sync;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Fight;

public class PacketFightGeneralScNotify : BasePacket
{
    public PacketFightGeneralScNotify(MarbleNetWorkMsgEnum msgType, MarbleNetWorkMsgEnum syncType,
        MarbleGameRoomInstance game) : base(CmdIds.FightGeneralScNotify)
    {
        var proto = new FightGeneralScNotify
        {
            NetworkMsgType = (uint)msgType,
            FightGeneralInfo = new FightGeneralServerInfo
            {
                FightGameInfo =
                {
                    new FightGameInfo
                    {
                        MarbleGameInfo = game.ToProto(),
                        GameMessageType = (uint)syncType
                    }
                }
            }
        };

        SetData(proto);
    }

    public PacketFightGeneralScNotify(MarbleNetWorkMsgEnum msgType, MarbleNetWorkMsgEnum syncType,
        FightMarbleSealInfo sealInfo) : base(CmdIds.FightGeneralScNotify)
    {
        var proto = new FightGeneralScNotify
        {
            NetworkMsgType = (uint)msgType,
            FightGeneralInfo = new FightGeneralServerInfo
            {
                FightGameInfo =
                {
                    new FightGameInfo
                    {
                        FightMarbleSealInfo = sealInfo,
                        GameMessageType = (uint)syncType
                    }
                }
            }
        };

        SetData(proto);
    }

    public PacketFightGeneralScNotify(MarbleNetWorkMsgEnum msgType, List<MarbleGameBaseSyncData> sync) : base(
        CmdIds.FightGeneralScNotify)
    {
        var proto = new FightGeneralScNotify
        {
            NetworkMsgType = (uint)msgType,
            FightGeneralInfo = new FightGeneralServerInfo
            {
                FightGameInfo = { sync.Select(x => x.ToProto()) }
            }
        };

        SetData(proto);
    }
}