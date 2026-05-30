using March7thHoney.Kcp;
using March7thHoney.Proto;
// 4.3: RechargeGift proto 混淆名整体变更
//   GetRechargeGiftInfoScRsp.FEKLJCAOGII -> OFIHOCCHNCN (元素 ABIPCNKGFNI -> JBNGCJJDLHL)
//   JBNGCJJDLHL: DNNHCNCKJCM(开始时间) -> IFEPJHGNCOI, FLHLJMEMIHL -> CFNNMOCBBFM (元素 PPPFOMBLOIF -> IJHPILBBHDI)
//   状态枚举 BNDHJNDFFED -> 嵌套 EGCFNCBLHJC，值 Amjcmcjdpkd(=1) -> Acbhglibjbg(=1)
using GiftRecordStatus = March7thHoney.Proto.IJHPILBBHDI.Types.BBIBCFCKPJH.Types.EGCFNCBLHJC;

namespace March7thHoney.GameServer.Server.Packet.Send.RechargeGift;

public class PacketGetRechargeGiftInfoScRsp : BasePacket
{
    public PacketGetRechargeGiftInfoScRsp() : base(CmdIds.GetRechargeGiftInfoScRsp)
    {
        var proto = new GetRechargeGiftInfoScRsp
        {
            OFIHOCCHNCN =
            {
                new JBNGCJJDLHL
                {
                    IFEPJHGNCOI = 1777112805,
                    EndTime = 1777976805,
                    GiftType = 16,
                    CFNNMOCBBFM =
                    {
                        new IJHPILBBHDI { Status = GiftRecordStatus.Acbhglibjbg },
                        new IJHPILBBHDI { Index = 2, Status = GiftRecordStatus.Acbhglibjbg },
                        new IJHPILBBHDI { Index = 3, Status = GiftRecordStatus.Acbhglibjbg },
                        new IJHPILBBHDI { Index = 4, Status = GiftRecordStatus.Acbhglibjbg },
                        new IJHPILBBHDI { Index = 5, Status = GiftRecordStatus.Acbhglibjbg }
                    }
                }
            }
        };

        SetData(proto);
    }
}

