using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>GetGuildEditorDataMessageEvent — the badge parts and colours the editor offers</summary>
[Incoming(ClientMessages.GetGuildEditorDataMessageEvent)]
public sealed partial record GetGuildEditorDataMessageEvent;
