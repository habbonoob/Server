using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.NewNavigator;

/// <summary>NavigatorAddCollapsedCategoryMessageEvent</summary>
[Incoming(ClientMessages.NavigatorAddCollapsedCategoryMessageEvent)]
public sealed partial record NavigatorAddCollapsedCategoryMessageEvent(string Category);
