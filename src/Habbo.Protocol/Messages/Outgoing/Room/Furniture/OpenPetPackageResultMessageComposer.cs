using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>OpenPetPackageResultMessageComposer — whether the box opened, or why the name would not do</summary>
[Message(ServerMessages.OpenPetPackageResultMessageComposer)]
public sealed partial record OpenPetPackageResultMessageComposer(int ItemId, int NameValidationStatus, string NameValidationInfo);
