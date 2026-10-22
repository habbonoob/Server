namespace Habbo.Domain.Moderation;

/// <summary>Where a report came from, as the mod tool's IssueCategoryNames names them.</summary>
public static class ModerationIssueSources
{
    public const int Normal = 1;
    public const int Automatic = 3;
    public const int AutomaticInstantMessage = 4;
    public const int GuideSystem = 5;
    public const int InstantMessage = 6;
    public const int Room = 7;
    public const int Panic = 8;
    public const int Guardian = 9;
    public const int AutomaticHelper = 10;
    public const int Discussion = 11;
    public const int Selfie = 12;
    public const int Photo = 14;
    public const int Ambassador = 15;
}
