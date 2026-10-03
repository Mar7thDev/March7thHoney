using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Send.Pet;

/// <summary>
///     背包“盲盒回收”页查询的盲盒池。BlindBoxPool 表不在缓存里，按 4.6.0 资源里唯一的池 1001 写死；
///     开始时间取官服 4.6 首期活动（1790499600），结束时间推到 2100-01-01，私服不过期（yuukisr_go c4f5979）。
///     开过盒子后才出现的计数字段 CANDEKPIMCC 新号抓包里没有，这里也不带。
/// </summary>
public class PacketBlindBoxGetDataScRsp : BasePacket
{
    public const uint PoolId = 1001;
    public const long PoolBeginTime = 1790499600;
    public const long PoolEndTime = 4102444800;

    public PacketBlindBoxGetDataScRsp() : base(CmdIds.BlindBoxGetDataScRsp)
    {
        var proto = new BlindBoxGetDataScRsp();
        proto.CEGOIBGJDBO.Add(new MFCCCIMGMFL
        {
            PoolId = PoolId,
            JPLIJJFCAOO = PoolBeginTime,
            EndTime = PoolEndTime
        });

        SetData(proto);
    }
}
