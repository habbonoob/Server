using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Avatar;

/// <summary>1048 CheckUserNameResultMessageComposer.</summary>
[Message(ServerMessages.CheckUserNameResultMessageComposer)]
public sealed partial record CheckUserNameResultMessageComposer(int ResultCode, string Name, IReadOnlyList<string> NameSuggestions);
