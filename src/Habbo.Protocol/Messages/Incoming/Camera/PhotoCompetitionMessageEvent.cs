using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Camera;

/// <summary>PhotoCompetitionMessageEvent — enter the photo last rendered in the running photo competition.</summary>
[Incoming(ClientMessages.PhotoCompetitionMessageEvent)]
public sealed partial record PhotoCompetitionMessageEvent;
