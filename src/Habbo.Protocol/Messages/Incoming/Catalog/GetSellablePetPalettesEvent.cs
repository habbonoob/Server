using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Catalog;

/// <summary>GetSellablePetPalettesEvent — the palettes a pet offer sells</summary>
[Incoming(ClientMessages.GetSellablePetPalettesEvent)]
public sealed partial record GetSellablePetPalettesEvent(string ProductCode);
