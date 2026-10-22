using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>BadgePartData</summary>
[WireStruct]
public sealed partial record BadgePartData(int Id, string FileName, string MaskFileName);
