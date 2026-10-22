using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.NewNavigator;

/// <summary>One top-level navigator tab and its quick links.</summary>
[WireStruct]
public sealed partial record NavigatorContextData(
    string SearchCode, IReadOnlyList<NavigatorQuickLinkData> QuickLinks);
