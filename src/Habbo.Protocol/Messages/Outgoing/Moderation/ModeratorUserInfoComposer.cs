using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>ModeratorUserInfoComposer</summary>
[Message(ServerMessages.ModeratorUserInfoComposer)]
public sealed partial record ModeratorUserInfoComposer(int UserId, string UserName, string Figure, int RegistrationAgeInMinutes, int MinutesSinceLastLogin, bool Online, int CfhCount, int AbusiveCfhCount, int CautionCount, int BanCount, int TradingLockCount, string TradingExpiryDate, string LastPurchaseDate, int IdentityId, int IdentityRelatedBanCount, string PrimaryEmailAddress, string UserClassification, string LastSanctionTime, int SanctionAgeHours);
