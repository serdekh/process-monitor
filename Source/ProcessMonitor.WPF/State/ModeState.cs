namespace ProcessMonitor.WPF.State;

public enum ModeState
{
    Startup,
    Settings,
    Running
}

public static class ModeStateExtensions
{
    public static string ToString(this ModeState state)
    {
        return state switch
        {
            ModeState.Startup => "Startup",
            ModeState.Settings => "Settings",
            ModeState.Running => "Running",
            _ => "Unknown"
        };
    }
}