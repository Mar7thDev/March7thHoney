using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Message;

public class PacketFinishPerformSectionIdScRsp : BasePacket
{
    public PacketFinishPerformSectionIdScRsp() : this(null)
    {
    }

    /// <param name="payload">The request's nested payload; echoed back with an empty reward (official wire).</param>
    public PacketFinishPerformSectionIdScRsp(FPDBFGBJOFD? payload) : base(CmdIds.FinishPerformSectionIdScRsp)
    {
        var rsp = new FinishPerformSectionIdScRsp();

        if (payload != null)
        {
            var data = new LGMDACIPFHL
            {
                MGPGDLIJKLM = payload.MGPGDLIJKLM,
                EBMIJAAKMMH = new ItemList()
            };
            foreach (var item in payload.DPPAFBAHEFH)
                data.DPPAFBAHEFH.Add(new MessageItem
                {
                    ItemId = item.ItemId
                });
            rsp.MIINDKFMBBM = data;
        }

        SetData(rsp);
    }
}
