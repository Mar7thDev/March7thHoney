using System.Numerics;

namespace CastoriceHoney.Server.GameServer.Game.MultiPlayer.MarbleGame.Physics;

public record CollisionRecord(float Time, BallSnapshot BallA, BallSnapshot? BallB, Vector2 CollisionPos);

public record StopRecord(float Time, BallSnapshot Ball);

public record ChangeSpeedRecord(float Time, BallSnapshot Ball);