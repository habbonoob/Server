using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>2239 ExtendedProfileMessageComposer.</summary>
[Message(ServerMessages.ExtendedProfileMessageComposer)]
public sealed partial record ExtendedProfileMessageComposer(ExtendedProfileData Data);
