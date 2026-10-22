using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.NewNavigator;

/// <summary>NavigatorCollapsedCategoriesMessageComposer — the result blocks the player keeps folded</summary>
[Message(ServerMessages.NavigatorCollapsedCategoriesMessageComposer)]
public sealed partial record NavigatorCollapsedCategoriesMessageComposer(IReadOnlyList<string> Categories);
