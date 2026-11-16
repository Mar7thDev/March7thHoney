namespace March7thHoney.Util.License;

public static class LimitedModeSwitch
{
#if LIMITED_MODE
    public const bool Enabled = true;
#else
    public const bool Enabled = false;
#endif
}

