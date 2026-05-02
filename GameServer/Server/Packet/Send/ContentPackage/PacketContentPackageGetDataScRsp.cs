using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.ContentPackage;

public class PacketContentPackageGetDataScRsp : BasePacket
{
    public PacketContentPackageGetDataScRsp() : base(CmdIds.ContentPackageGetDataScRsp)
    {
        var proto = new ContentPackageGetDataScRsp
        {
            Data = new ContentPackageData
            {
                ContentPackageList =
                {
                    GameData.ContentPackageConfigData.Select(x => new ContentPackageInfo
                    {
                        ContentId = (uint)x.Key,
                        Status = ContentPackageStatus.Finished
                    })
                }
            }
        };

        SetData(proto);
    }
}