using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Action;

/// <summary>863 RemoveAllRights — the owner takes rights back from everyone in the room.</summary>
[Incoming(ClientMessages.RemoveAllRightsMessageEvent)]
public sealed partial record RemoveAllRightsMessageEvent(int RoomId);
