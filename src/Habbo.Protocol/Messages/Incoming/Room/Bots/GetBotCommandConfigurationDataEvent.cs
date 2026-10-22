using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Bots;

/// <summary>GetBotCommandConfigurationDataEvent — how one of a bot's skills is set up</summary>
[Incoming(ClientMessages.GetBotCommandConfigurationDataEvent)]
public sealed partial record GetBotCommandConfigurationDataEvent(int BotId, int SkillId);
