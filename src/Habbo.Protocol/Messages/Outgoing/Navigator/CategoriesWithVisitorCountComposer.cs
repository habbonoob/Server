using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>CategoriesWithVisitorCountComposer</summary>
[Message(ServerMessages.CategoriesWithVisitorCountComposer)]
public sealed partial record CategoriesWithVisitorCountComposer(IReadOnlyList<CategoryVisitorCountData> Categories);
