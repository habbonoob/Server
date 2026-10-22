namespace Habbo.Protocol.Wire;

/// <summary>On a member: on the wire only to and from clients older than this release (one of
/// <see cref="ClientReleases"/>), which dropped it.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
public sealed class WireUntilAttribute(string release) : Attribute
{
    public string Release { get; } = release;
}
