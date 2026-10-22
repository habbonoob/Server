using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Register;

/// <summary>1289 UpdateFigureData — the avatar editor's save.</summary>
[Incoming(ClientMessages.UpdateFigureDataMessageEvent)]
public sealed partial record UpdateFigureDataMessageEvent(string Gender, string Figure);
