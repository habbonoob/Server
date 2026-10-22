using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>GetCategoriesWithUserCountMessageEvent</summary>
[Incoming(ClientMessages.GetCategoriesWithUserCountMessageEvent)]
public sealed partial record GetCategoriesWithUserCountMessageEvent;
