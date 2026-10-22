using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>CategoryVisitorCountData</summary>
[WireStruct]
public sealed partial record CategoryVisitorCountData(int CategoryId, int CurrentUsers, int MaxUsers);
