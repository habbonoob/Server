using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>ClubGiftData</summary>
[WireStruct]
public sealed partial record ClubGiftData(int OfferId, bool IsVip, int DaysRequired, bool IsSelectable);
