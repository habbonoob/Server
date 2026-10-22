using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Contracts;

/// <summary>WiredOpenContractMessageEvent — a contract's requirements, for its dialog</summary>
[Incoming(ClientMessages.WiredOpenContractMessageEvent)]
public sealed partial record WiredOpenContractMessageEvent(int ContractId);
