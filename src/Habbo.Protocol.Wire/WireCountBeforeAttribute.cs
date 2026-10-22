namespace Habbo.Protocol.Wire;

/// <summary>On a list: its count is written just before the named member, which comes earlier in
/// the wire order, while the items stay where the list is declared. For the few packets whose
/// parser reads a count, then something else, and only then the items. Like [WireWhen], the member
/// is named by string; the generator fails the build (HAB010) unless it names an earlier member.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
public sealed class WireCountBeforeAttribute(string member) : Attribute
{
    /// <summary>The member the count is written in front of.</summary>
    public string Member { get; } = member;
}
