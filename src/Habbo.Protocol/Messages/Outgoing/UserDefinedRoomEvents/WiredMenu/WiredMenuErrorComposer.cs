using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>3612 WiredMenuErrorComposer — a wired menu request could not be answered.</summary>
[Message(ServerMessages.WiredMenuErrorComposer)]
public sealed partial record WiredMenuErrorComposer([property: WireFormat(WireType.I16)] short ErrorCode)
{
    /// <summary>The object asked about is not there (any more): the inspection stops waiting for it.</summary>
    public const short ObjectNotFound = 0;
}
