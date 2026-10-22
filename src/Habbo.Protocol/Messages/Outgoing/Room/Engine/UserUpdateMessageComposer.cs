using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>3657 UserUpdateMessageComposer — a batch of avatar changes.</summary>
[Message(ServerMessages.UserUpdateMessageComposer)]
public sealed partial record UserUpdateMessageComposer(IReadOnlyList<AvatarUpdateWireData> Updates)
{
    public static UserUpdateMessageComposer From(IReadOnlyList<AvatarUpdateData> updates) =>
        new(updates.Select(AvatarUpdateWireData.From).ToList());
}
