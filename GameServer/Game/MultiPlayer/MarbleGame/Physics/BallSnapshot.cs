using System.Numerics;

namespace March7thHoney.GameServer.Game.MultiPlayer.MarbleGame.Physics;

public class BallSnapshot
{
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
}