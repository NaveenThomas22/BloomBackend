namespace Bloom.Core.Constants;

public class AppDefaultsOptions
{
    public const string SectionName = "AppDefaults";

    public ushort? DefaultCatTargetYear { get; set; }
    public string DefaultPreparationLevel { get; set; } = "Beginner";
    public string DefaultPreparationMode { get; set; } = "SelfStudy";
    public string DefaultTimezone { get; set; } = "Asia/Kolkata";
    public string DefaultTheme { get; set; } = "Sage Garden";
    public uint DefaultDailyStudyTargetMinutes { get; set; } = 360;
}

