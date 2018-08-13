namespace Q4SRM.Settings
{
    public static class IOptionsExtensions
    {
        public static void SetDefaults(this IOptions options)
        {
            options.SetDefaultThresholds();
            options.CreatedThresholdsFileThresholdLevel = 0.5;
        }
    }
}
