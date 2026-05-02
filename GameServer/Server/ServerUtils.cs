using System.Reflection;
using CastoriceHoney.Server.GameServer.Game.Mission;
using CastoriceHoney.Server.GameServer.Game.Mission.FinishAction;
using CastoriceHoney.Server.GameServer.Game.Mission.FinishType;

namespace CastoriceHoney.Server.GameServer.Server;

public static class ServerUtils
{
    public static void InitializeHandlers()
    {
        // mission handlers
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<MissionFinishActionAttribute>();
                if (attr != null)
                {
                    var handler = (MissionFinishActionHandler)Activator.CreateInstance(type, null)!;
                    MissionManager.ActionHandlers.Add(attr.FinishAction, handler);
                }

                var attr2 = type.GetCustomAttribute<MissionFinishTypeAttribute>();
                if (attr2 != null)
                {
                    var handler = (MissionFinishTypeHandler)Activator.CreateInstance(type, null)!;
                    MissionManager.FinishTypeHandlers.Add(attr2.FinishType, handler);
                }
            }
        }

    }
}
