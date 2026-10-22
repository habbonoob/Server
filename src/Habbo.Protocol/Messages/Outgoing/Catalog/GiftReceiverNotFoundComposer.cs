using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>GiftReceiverNotFoundComposer — nobody by the name the gift was for</summary>
[Message(ServerMessages.GiftReceiverNotFoundComposer)]
public sealed partial record GiftReceiverNotFoundComposer;
