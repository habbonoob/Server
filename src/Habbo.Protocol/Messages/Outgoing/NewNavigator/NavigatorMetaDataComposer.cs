using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Protocol.Messages.Outgoing.NewNavigator;

/// <summary>3474 NavigatorMetaDataComposer — the navigator-ready gate.</summary>
[Message(ServerMessages.NavigatorMetaDataComposer)]
public sealed partial record NavigatorMetaDataComposer(IReadOnlyList<NavigatorContextData> Contexts)
{
    public static NavigatorMetaDataComposer From(IReadOnlyList<NavigatorContext> contexts) =>
        new(contexts
            .Select(context => new NavigatorContextData(
                context.SearchCode,
                context.QuickLinks
                    .Select(link => new NavigatorQuickLinkData(
                        link.Id, link.SearchCode, link.Filter, link.Localization))
                    .ToList()))
            .ToList());
}
