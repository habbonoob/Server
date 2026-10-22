using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Avatar;

/// <summary>2023 FigureUpdateComposer — the player's own new look.</summary>
[Message(ServerMessages.FigureUpdateComposer)]
public sealed partial record FigureUpdateComposer(string Figure, string Gender);
