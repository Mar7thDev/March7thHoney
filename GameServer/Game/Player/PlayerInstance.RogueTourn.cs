namespace March7thHoney.GameServer.Game.Player;

public partial class PlayerInstance
{
    public RogueTourn.RogueTournManager? RogueTournManager { get; private set; }

    /// <summary>
    ///     The scene position version the client last reported (SceneUpdatePositionVersionNotify). The client adopts
    ///     the version a scene carries, so a Divergent Universe room that does not advance it keeps a frozen world.
    /// </summary>
    public int ClientPosVersion { get; set; } = 1;
}
