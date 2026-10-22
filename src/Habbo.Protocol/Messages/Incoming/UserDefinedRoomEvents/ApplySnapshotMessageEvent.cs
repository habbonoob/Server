using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

/// <summary>50 ApplySnapshot — the dialog's "apply" button on a match-to-snapshot effect: put the picked furni back as the snapshot recorded them.</summary>
[Incoming(ClientMessages.ApplySnapshotMessageEvent)]
public sealed partial record ApplySnapshotMessageEvent(
    int ItemId);
