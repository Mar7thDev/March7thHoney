using March7thHoney.GameServer.Game.Pet;
using March7thHoney.GameServer.Server.Packet.Send.Pet;
using March7thHoney.Kcp;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Server.Packet.Recv.Pet;

/// <summary>
///     4.6 召唤：请求 { ref_type 枚举, oneof { 一代宠物 id, 盲盒宠物 unique id } }，回包把同样的内容放进 cur_pet。
///     官服抓包里召唤盲盒小队只带枚举（3），回包 cur_pet 也只有枚举。旧的 CurPetChangedScNotify 在 4.6 已不存在。
/// </summary>
[Opcode(CmdIds.SummonPetCsReq)]
public class HandlerSummonPetCsReq : Handler<SummonPetCsReq>
{
    protected override async Task OnHandle(Connection connection, PlayerInstance player, SummonPetCsReq req)
    {
        var curPet = player.GetComponent<PetManager>().Summon(req);

        await connection.SendPacket(new PacketSummonPetScRsp(curPet));
        await player.TrainCakeCatchManager!.BroadcastPlayerStateAsync();
    }
}
