using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.CallForHelp;

/// <summary>One reason a player can report something for. The consequence is the text the client
/// warns the reporter with before it sends the report.</summary>
[WireStruct]
public sealed partial record CallForHelpTopicData(string Name, int Id, string Consequence);
