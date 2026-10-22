using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.HotLooks;

[WireStruct]
public sealed partial record HotLookData(string Gender, string Figure);
