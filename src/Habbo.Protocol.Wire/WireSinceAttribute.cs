namespace Habbo.Protocol.Wire;

/// <summary>On a member: on the wire only to and from clients of this release (one of <see cref="ClientReleases"/>)
/// and later ones, which added it. Older clients never see it, and never send it.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
public sealed class WireSinceAttribute(string release) : Attribute
{
    public string Release { get; } = release;
}
