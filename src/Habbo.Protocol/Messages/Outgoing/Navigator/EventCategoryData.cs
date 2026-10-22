using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>One of UserEventCats' categories.</summary>
[WireStruct]
public sealed partial record EventCategoryData(int CategoryId, string CategoryName, bool Visible);
