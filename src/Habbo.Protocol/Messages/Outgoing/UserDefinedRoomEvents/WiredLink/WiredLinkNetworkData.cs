using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredLink;

/// <summary>A wired link network, as a Link's dialog lists it (the client's WiredLinkNetwork): its id as a decimal
/// string, its name, how many Links are connected to it, and whether a Link in the room the dialog is open in is.</summary>
[WireStruct]
public sealed partial record WiredLinkNetworkData(
    string Id,
    string Name,
    int ConnectionCount,
    bool LinkedInCurrentRoom);
