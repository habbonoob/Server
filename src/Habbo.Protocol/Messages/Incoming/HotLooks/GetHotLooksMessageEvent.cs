using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.HotLooks;

/// <summary>782 GetHotLooks — some looks other players wear, for the editor's inspiration panel.</summary>
[Incoming(ClientMessages.GetHotLooksMessageEvent)]
public sealed partial record GetHotLooksMessageEvent(byte Amount);
