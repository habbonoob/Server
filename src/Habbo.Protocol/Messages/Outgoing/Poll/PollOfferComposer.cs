using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Poll;

/// <summary>PollOfferComposer — a poll the player may take</summary>
[Message(ServerMessages.PollOfferComposer)]
public sealed partial record PollOfferComposer(int Id, string Type, string Headline, string Summary);
