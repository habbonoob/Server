using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>3979 WiredGetVariablesForObject — the inspection tab asks what a furni (by item id), a
/// user (by room index) or the room (-10) holds.</summary>
[Incoming(ClientMessages.WiredGetVariablesForObjectMessageEvent)]
public sealed partial record WiredGetVariablesForObjectMessageEvent(int Target, int ObjectId);
