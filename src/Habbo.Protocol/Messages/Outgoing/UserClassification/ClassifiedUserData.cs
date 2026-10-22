using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserClassification;

[WireStruct]
public sealed partial record ClassifiedUserData(int UserId, string UserName, string UserType);
