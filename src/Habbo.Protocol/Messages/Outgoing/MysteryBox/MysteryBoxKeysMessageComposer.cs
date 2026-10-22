using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.MysteryBox;

/// <summary>1549 MysteryBoxKeysMessageComposer — the colours of the box and key the hotel is
/// currently running a mystery box event with. Empty strings when no event is on.</summary>
[Message(ServerMessages.MysteryBoxKeysMessageComposer)]
public sealed partial record MysteryBoxKeysMessageComposer(string BoxColor, string KeyColor)
{
    /// <summary>What the client reads as "no mystery box event is running".</summary>
    public static MysteryBoxKeysMessageComposer None { get; } = new(string.Empty, string.Empty);
}
