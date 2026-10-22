using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>GiftWrappingConfigurationComposer — the wrappings a gift may come in: paid wraps by furni class, boxes, ribbons, and the free wraps</summary>
[Message(ServerMessages.GiftWrappingConfigurationComposer)]
public sealed partial record GiftWrappingConfigurationComposer(bool IsWrappingEnabled, int WrappingPrice, IReadOnlyList<int> StuffTypes, IReadOnlyList<int> BoxTypes, IReadOnlyList<int> RibbonTypes, IReadOnlyList<int> DefaultStuffTypes);
