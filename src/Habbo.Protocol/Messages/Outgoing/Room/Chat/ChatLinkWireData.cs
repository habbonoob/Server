using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>A clickable link inside a chat bubble.</summary>
[WireStruct]
public sealed partial record ChatLinkWireData(string Text, string Url, bool Trusted);
