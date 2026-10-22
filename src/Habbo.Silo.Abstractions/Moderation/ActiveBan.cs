namespace Habbo.Silo.Abstractions.Moderation;

/// <summary>A ban in force: why, and until when.</summary>
[GenerateSerializer]
public sealed partial record ActiveBan([property: Id(0)] string Message, [property: Id(1)] DateTimeOffset ExpiresAt);
