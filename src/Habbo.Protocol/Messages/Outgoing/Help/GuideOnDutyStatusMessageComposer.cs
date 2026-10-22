using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideOnDutyStatusMessageComposer — whether the player is on duty, and how many are</summary>
[Message(ServerMessages.GuideOnDutyStatusMessageComposer)]
public sealed partial record GuideOnDutyStatusMessageComposer(bool OnDuty, int GuidesOnDuty, int HelpersOnDuty, int GuardiansOnDuty);
