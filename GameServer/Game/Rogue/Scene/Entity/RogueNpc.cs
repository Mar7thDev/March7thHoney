using CastoriceHoney.Server.Data.Config.Scene;
using CastoriceHoney.Server.GameServer.Game.Rogue.Event;
using CastoriceHoney.Server.GameServer.Game.Scene;
using CastoriceHoney.Server.GameServer.Game.Scene.Entity;
using CastoriceHoney.Server.GameServer.Server.Packet.Send.Scene;
using CastoriceHoney.Server.Proto;

namespace CastoriceHoney.Server.GameServer.Game.Rogue.Scene.Entity;

public class RogueNpc(SceneInstance scene, GroupInfo group, NpcInfo npcInfo) : EntityNpc(scene, group, npcInfo)
{
    public int RogueNpcId { get; set; }
    public int UniqueId { get; set; }
    private bool IsFinish { get; set; }

    public RogueEventInstance? RogueEvent { get; set; }

    public async ValueTask FinishDialogue()
    {
        IsFinish = true;
        await Scene.Player.SendPacket(new PacketSceneGroupRefreshScNotify(Scene.Player, this));
    }

    public override SceneEntityInfo ToProto()
    {
        var proto = base.ToProto();

        if (RogueNpcId <= 0 || RogueEvent == null) return proto;
        proto.Npc.ExtraInfo = new NpcExtraInfo
        {
            RogueGameInfo = new NpcRogueGameInfo
            {
                TalkDialogueId = (uint)RogueNpcId,
                EventUniqueId = (uint)UniqueId,
                FinishDialogue = IsFinish
                //DialogueGroupId = (uint)GroupID
            }
        };

        return proto;
    }
}