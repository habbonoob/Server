using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>ModToolPreferencesEvent</summary>
[Incoming(ClientMessages.ModToolPreferencesEvent)]
public sealed partial record ModToolPreferencesEvent(int WindowX, int WindowY, int WindowWidth, int WindowHeight);
