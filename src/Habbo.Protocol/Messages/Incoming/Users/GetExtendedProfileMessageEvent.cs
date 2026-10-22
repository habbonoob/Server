using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>2829 GetExtendedProfile — a user's profile; the flag says whether to open the window.</summary>
[Incoming(ClientMessages.GetExtendedProfileMessageEvent)]
public sealed partial record GetExtendedProfileMessageEvent(int UserId, bool OpenProfileWindow);
