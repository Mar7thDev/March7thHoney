using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace March7thHoney.GameServer.Server.Packet;

/// <summary>
///     Fills unset singular sub-message fields with empty instances, so the client receives a present-but-empty
///     sub-message instead of null. Some 4.6 data responses hand a sub-message straight to client code that
///     dereferences it; the generic empty-body reply for an unhandled request leaves it null and the client throws.
/// </summary>
public static class ProtoPresence
{
    private const int DefaultDepth = 5;

    public static T FillEmptySubMessages<T>(T message, int maxDepth = DefaultDepth) where T : IMessage
    {
        Fill(message, maxDepth);
        return message;
    }

    private static void Fill(IMessage message, int depth)
    {
        if (depth <= 0) return;
        foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
        {
            // Lists stay empty, and a oneof can carry only one arm, so its choice is left to the caller.
            if (field.FieldType != FieldType.Message || field.IsRepeated || field.RealContainingOneof != null)
                continue;

            if (field.Accessor.GetValue(message) is not IMessage child)
            {
                child = field.MessageType.Parser.ParseFrom(ByteString.Empty);
                field.Accessor.SetValue(message, child);
            }

            Fill(child, depth - 1);
        }
    }
}
