using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Camera;

/// <summary>CompetitionStatusMessageComposer — whether the photo was entered in the competition, and if not why (a
/// camera.competition.error localization key).</summary>
[Message(ServerMessages.CompetitionStatusMessageComposer)]
public sealed partial record CompetitionStatusMessageComposer(bool Ok, string ErrorReason);
