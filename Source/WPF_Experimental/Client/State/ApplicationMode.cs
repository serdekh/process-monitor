namespace WPF_Experimental.Client.State;

public enum ApplicationMode
{
    Startup,
    Settings,
    Running
}

public static class ApplicationModeExtensions
{
    public static string AsString(this ApplicationMode mode)
    {
        return mode switch
        {
            ApplicationMode.Startup => "Startup",
            ApplicationMode.Settings => "Settings",
            ApplicationMode.Running => "Running",
            _ => "Unknown"
        };       
    }
}