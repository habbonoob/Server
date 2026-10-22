namespace Habbo.Silo.Abstractions.Moderation;

/// <summary>A chat line picked as evidence: who said it, by account id, and what.</summary>
[GenerateSerializer]
public sealed partial record ReportedChatLine([property: Id(0)] int AccountId, [property: Id(1)] string Text);
