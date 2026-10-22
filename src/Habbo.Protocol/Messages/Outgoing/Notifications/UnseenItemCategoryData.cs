using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>The unseen items of one inventory category.</summary>
[WireStruct]
public sealed partial record UnseenItemCategoryData(int Category, IReadOnlyList<int> ItemIds);
