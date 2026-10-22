using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>829 ScrGetUserInfo — the client asks where one subscription stands, naming the
/// product it means.</summary>
[Incoming(ClientMessages.ScrGetUserInfoMessageEvent)]
public sealed partial record ScrGetUserInfoMessageEvent(string ProductName);
