using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Pets;

/// <summary>PetCommandsMessageComposer — the commands a pet knows and those it obeys yet</summary>
[Message(ServerMessages.PetCommandsMessageComposer)]
public sealed partial record PetCommandsMessageComposer(int PetId, IReadOnlyList<int> AllCommands, IReadOnlyList<int> EnabledCommands);
