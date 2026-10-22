using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>1165 ReplenishRespect — buy the day's respects back for duckets.</summary>
[Incoming(ClientMessages.ReplenishRespectMessageEvent)]
public sealed partial record ReplenishRespectMessageEvent;
