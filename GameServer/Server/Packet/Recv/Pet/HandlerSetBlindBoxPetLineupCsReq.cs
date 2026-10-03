using March7thHoney.GameServer.Game.Pet;
using March7thHoney.GameServer.Server.Packet.Send.Pet;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Pet;

/// <summary>
///     设置盲盒宠物随行编队。官服顺序：先 PetDataChangeScNotify{blind_box_lineup}，再 ScRsp，两者都回显请求里的
///     来源枚举。非法请求（未拥有 / 重复实例、超过 3 槽）不改存档，只回失败码。
/// </summary>
[Opcode(CmdIds.SetBlindBoxPetLineupCsReq)]
public class HandlerSetBlindBoxPetLineupCsReq : Handler<SetBlindBoxPetLineupCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, SetBlindBoxPetLineupCsReq req)
    {
        var pets = player.GetComponent<PetManager>();
        if (!pets.SetBlindBoxLineup(req.POJGKJENENA))
        {
            await connection.SendPacket(new PacketSetBlindBoxPetLineupScRsp(Retcode.RetReqParaInvalid));
            return;
        }

        await connection.SendPacket(new PacketPetDataChangeScNotify(pets.ToLineupProto(req.CGBJOAJEMKN)));
        await connection.SendPacket(new PacketSetBlindBoxPetLineupScRsp(pets.ToLineupProto(req.CGBJOAJEMKN)));
    }
}
