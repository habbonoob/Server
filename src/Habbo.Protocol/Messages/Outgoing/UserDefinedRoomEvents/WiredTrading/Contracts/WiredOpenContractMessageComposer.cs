using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Contracts;

/// <summary>WiredOpenContractMessageComposer — the client should open this contract</summary>
[Message(ServerMessages.WiredOpenContractMessageComposer)]
public sealed partial record WiredOpenContractMessageComposer(int ContractId);
