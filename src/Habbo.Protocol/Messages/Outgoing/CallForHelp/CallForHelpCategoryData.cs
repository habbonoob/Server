using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.CallForHelp;

/// <summary>One group of report reasons, as the client lists them in the help tool.</summary>
[WireStruct]
public sealed partial record CallForHelpCategoryData(string Name, IReadOnlyList<CallForHelpTopicData> Topics);
