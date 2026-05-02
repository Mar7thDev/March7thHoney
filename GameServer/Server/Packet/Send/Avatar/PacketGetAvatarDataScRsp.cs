using CastoriceHoney.Server.Data;
using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Avatar;

public class PacketGetAvatarDataScRsp : BasePacket
{
    public PacketGetAvatarDataScRsp(PlayerInstance player) : base(CmdIds.GetAvatarDataScRsp)
    {
        var proto = new GetAvatarDataScRsp
        {
            IsGetAll = true,
            PlayerOutfitData = player.Data.ToPlayerOutfitProto()
        };

        player.PlayerUnlockData!.Skins.Values.ToList().ForEach(skin =>
            proto.SkinList.AddRange(skin.Select(x => (uint)x)));

        var basicTypeIds = new HashSet<uint>();

        foreach (var avatar in player.AvatarManager?.AvatarData?.FormalAvatars ?? Enumerable.Empty<CastoriceHoney.Server.Database.Avatar.FormalAvatarInfo>())
        {
            proto.AvatarList.Add(avatar.ToProto());
            var pathProtos = avatar.ToAvatarPathDataProto();
            proto.AvatarPathDataInfoList.AddRange(pathProtos);

            foreach (var path in pathProtos)
                if (GameData.MultiplePathAvatarConfigData.ContainsKey((int)path.AvatarId))
                    basicTypeIds.Add(path.AvatarId);
        }

        proto.BasicTypeIdList.AddRange(basicTypeIds);

        SetData(proto);
    }
}

