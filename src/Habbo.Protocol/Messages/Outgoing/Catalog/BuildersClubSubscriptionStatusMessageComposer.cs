using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>3985 BuildersClubSubscriptionStatusMessageComposer — how long the Builders Club
/// membership has left and how much it lets the player build with. The grace period is sent
/// apart from the membership itself: the client keeps the items usable until it runs out.</summary>
[Message(ServerMessages.BuildersClubSubscriptionStatusMessageComposer)]
public sealed partial record BuildersClubSubscriptionStatusMessageComposer(
    int SecondsLeft,
    int FurniLimit,
    int MaxFurniLimit,
    int SecondsLeftWithGrace);
