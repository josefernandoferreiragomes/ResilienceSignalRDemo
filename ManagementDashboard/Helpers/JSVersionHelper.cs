namespace ManagementDashboard.Helpers;

public static class JsVersionHelper
{
    public static string Version =>
        DateTime.UtcNow.Ticks.ToString();

}
