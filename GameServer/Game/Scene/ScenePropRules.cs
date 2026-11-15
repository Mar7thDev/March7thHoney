using March7thHoney.Data.Excel;
using March7thHoney.Enums.Scene;

namespace March7thHoney.GameServer.Game.Scene;

/// <summary>Saved-state and rogue-door checks shared by scene loading.</summary>
public static class ScenePropRules
{
    /// <summary>
    ///     A saved state outside a bridge prop's declared list, or Open on a rotate-bridge console, is a
    ///     write the prop cannot answer. Ignoring it lets the prop ship its authored state again.
    /// </summary>
    public static bool SavedStateAllowed(MazePropExcel? excel, PropStateEnum state)
    {
        if (excel == null || excel.PropStateList.Count == 0) return true;
        if (state == PropStateEnum.Open &&
            excel.JsonPath.Contains("RotateBridgeConsole", StringComparison.Ordinal))
            return false;

        var bridgeFamily = false;
        foreach (var name in excel.PropStateList)
        {
            if (name is >= PropStateEnum.BridgeState1 and <= PropStateEnum.BridgeState4)
                bridgeFamily = true;
            if (name == state) return true;
        }

        return !bridgeFamily;
    }

    /// <summary>
    ///     A rogue door only moves to a state its own PropStateList declares. Classic portals open on Open;
    ///     Divergent Universe doors open on CustomState04. Writing the other flavour's state leaves the
    ///     interact button unarmed.
    /// </summary>
    public static PropStateEnum RogueDoorOpenState(MazePropExcel excel, PropStateEnum preferred)
    {
        if (excel.PropStateList.Contains(preferred)) return preferred;
        if (excel.PropStateList.Contains(PropStateEnum.Open)) return PropStateEnum.Open;
        return preferred;
    }
}
