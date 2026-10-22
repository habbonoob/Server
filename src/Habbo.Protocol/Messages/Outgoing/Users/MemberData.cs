using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>MemberData — a member, admin, owner, applicant or blocked user</summary>
[WireStruct]
public sealed partial record MemberData(int Type, int UserId, string UserName, string Figure, string MemberSince);
