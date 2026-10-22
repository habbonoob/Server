using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserClassification;

/// <summary>2474 UserClassificationMessageComposer.</summary>
[Message(ServerMessages.UserClassificationMessageComposer)]
public sealed partial record UserClassificationMessageComposer(IReadOnlyList<ClassifiedUserData> Users);
