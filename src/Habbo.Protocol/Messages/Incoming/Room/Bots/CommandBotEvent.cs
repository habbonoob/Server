using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Bots;

/// <summary>CommandBotEvent — use one of a bot's skills</summary>
[Incoming(ClientMessages.CommandBotEvent)]
public sealed partial record CommandBotEvent(int BotId, int SkillId, string Data);
