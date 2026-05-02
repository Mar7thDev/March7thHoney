using CastoriceHoney.Server.GameServer.Game.Player;
using CastoriceHoney.Server.Kcp;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Server.Packet.Send.Message;

public class PacketGetNpcMessageGroupScRsp : BasePacket
{
    public PacketGetNpcMessageGroupScRsp(IEnumerable<uint> contactIdList, PlayerInstance instance) : base(
        CmdIds.GetNpcMessageGroupScRsp)
    {
        var proto = new GetNpcMessageGroupScRsp();

        foreach (var contactId in contactIdList)
        {
            var contact = instance.MessageManager!.GetMessageGroup((int)contactId);

            proto.MessageGroupList.AddRange(contact);
        }

        SetData(proto);
    }
}