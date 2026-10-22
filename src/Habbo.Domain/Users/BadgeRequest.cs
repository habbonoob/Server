namespace Habbo.Domain.Users;

/// <summary>A badge a player can ask for by code: the landing view's "get the badge" buttons and
/// the moderation tool's secret codes send the code, and the hotel hands over the badge it
/// stands for. Which codes exist is the hotel's to configure.</summary>
public sealed class BadgeRequest
{
    public const int CodeMaxLength = 64;

    public required string Code { get; set; }

    /// <summary>The badge the request grants.</summary>
    public required string BadgeCode { get; set; }
}
