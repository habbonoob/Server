using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.NewNavigator;

/// <summary>NavigatorRemoveCollapsedCategoryMessageEvent</summary>
[Incoming(ClientMessages.NavigatorRemoveCollapsedCategoryMessageEvent)]
public sealed partial record NavigatorRemoveCollapsedCategoryMessageEvent(string Category);
