using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>SetClothingChangeDataMessageEvent — a changing booth's outfit for one gender</summary>
[Incoming(ClientMessages.SetClothingChangeDataMessageEvent)]
public sealed partial record SetClothingChangeDataMessageEvent(int ItemId, string Gender, string Figure);
