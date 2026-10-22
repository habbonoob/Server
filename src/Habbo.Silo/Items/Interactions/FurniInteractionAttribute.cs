namespace Habbo.Silo.Items.Interactions;

/// <summary>Names the behaviour a <see cref="FurniInteraction"/> implements: the client logic
/// names (e.g. "furniture_multistate") or the server-only names from seed/furni_interactions.json
/// that items run it under.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class FurniInteractionAttribute(params string[] names) : Attribute
{
    public IReadOnlyList<string> Names { get; } = names;
}
