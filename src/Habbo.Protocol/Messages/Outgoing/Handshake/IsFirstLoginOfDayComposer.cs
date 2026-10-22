using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Handshake;

/// <summary>2990 IsFirstLoginOfDay — true on the player's first login of the day, which the
/// client uses to decide whether to play the daily welcome.</summary>
[Message(ServerMessages.IsFirstLoginOfDayComposer)]
public sealed partial record IsFirstLoginOfDayComposer(bool IsFirstLoginOfDay);
