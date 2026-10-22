namespace Habbo.Protocol.Wire;

/// <summary>On a nullable member: a bool saying whether it has a value is written first, then the
/// value when it has one. For the fields a client parser reads as "has X?" followed by X.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
public sealed class WirePresenceAttribute : Attribute;
