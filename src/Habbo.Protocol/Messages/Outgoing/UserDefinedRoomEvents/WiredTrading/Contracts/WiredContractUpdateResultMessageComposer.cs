using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Contracts;

/// <summary>WiredContractUpdateResultMessageComposer — how saving a contract went</summary>
[Message(ServerMessages.WiredContractUpdateResultMessageComposer)]
public sealed partial record WiredContractUpdateResultMessageComposer(int ContractId, bool IsSuccess, string FailCode);
