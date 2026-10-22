namespace Habbo.Domain.Moderation;

/// <summary>What a sanction does, as the mod tool's action types number them.</summary>
public enum SanctionKind
{
    Alert = 1,
    Mute = 2,
    Ban = 3,
    Kick = 4,
    TradeLock = 5,
    Message = 6,
}
