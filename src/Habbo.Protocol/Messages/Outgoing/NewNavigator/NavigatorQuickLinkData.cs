using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.NewNavigator;

/// <summary>A saved search inside a navigator tab.</summary>
[WireStruct]
public sealed partial record NavigatorQuickLinkData(
    int Id, string SearchCode, string Filter, string Localization);
