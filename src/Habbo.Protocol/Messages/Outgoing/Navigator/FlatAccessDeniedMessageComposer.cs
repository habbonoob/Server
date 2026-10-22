using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>FlatAccessDeniedMessageComposer</summary>
[Message(ServerMessages.FlatAccessDeniedMessageComposer)]
public sealed partial record FlatAccessDeniedMessageComposer(int FlatId, string UserName);
