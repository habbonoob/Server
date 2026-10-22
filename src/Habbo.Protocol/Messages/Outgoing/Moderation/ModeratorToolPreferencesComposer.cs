using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>ModeratorToolPreferencesComposer</summary>
[Message(ServerMessages.ModeratorToolPreferencesComposer)]
public sealed partial record ModeratorToolPreferencesComposer(int WindowX, int WindowY, int WindowWidth, int WindowHeight);
