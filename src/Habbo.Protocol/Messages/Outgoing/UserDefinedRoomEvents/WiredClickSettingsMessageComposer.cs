using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>What the user's clicks do in this room, as wired set them: on users (0 as usual, 1 click the user but walk
/// behind them, 2 pass through), and on furni (0 as usual, 1 pass through). The client forgets it on leaving.</summary>
[Message(ServerMessages.WiredClickSettingsMessageComposer)]
public sealed partial record WiredClickSettingsMessageComposer(int UserOption, int FurniOption);
