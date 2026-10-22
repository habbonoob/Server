using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Avatar;

/// <summary>2572 ChangeUserNameResultMessageComposer.</summary>
[Message(ServerMessages.ChangeUserNameResultMessageComposer)]
public sealed partial record ChangeUserNameResultMessageComposer(int ResultCode, string Name, IReadOnlyList<string> NameSuggestions);
