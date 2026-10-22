using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Handshake;

/// <summary>3847 AuthenticationOKMessageComposer. The actions are counted as an int but written
/// as shorts, which is why the element format is spelled out.</summary>
[Message(ServerMessages.AuthenticationOKMessageComposer)]
public sealed partial record AuthenticationOKMessageComposer(
    int AccountId,
    [property: WireFormat(WireType.I16)] IReadOnlyList<int> SuggestedLoginActions,
    int IdentityId);
