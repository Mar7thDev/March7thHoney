using March7thHoney.Data.Config.Scene;
using March7thHoney.GameServer.Game.Rogue.Event;
using March7thHoney.GameServer.Game.Scene;
using March7thHoney.GameServer.Game.Scene.Entity;
using March7thHoney.GameServer.Server.Packet.Send.Scene;
using March7thHoney.Proto;

namespace March7thHoney.GameServer.Game.Rogue.Scene.Entity;

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