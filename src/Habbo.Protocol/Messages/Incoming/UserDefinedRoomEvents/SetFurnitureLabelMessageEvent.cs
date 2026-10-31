using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

/// <summary>SetFurnitureLabel (October 2026) — the user rewrites a wired label's text.</summary>
[Incoming(ClientMessages.SetFurnitureLabelMessageEvent)]
public sealed partial record SetFurnitureLabelMessageEvent(
    int ItemId,
    string Text);
