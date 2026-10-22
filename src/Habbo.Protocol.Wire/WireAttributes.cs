namespace Habbo.Protocol.Wire;

/// <summary>How a value is laid out on the wire. The CLR type says what the value is;
/// this says how the client reads it.</summary>
public enum WireType
{
    /// <summary>Chosen from the CLR type: int → I32, short → I16, string → Utf8, and so on.</summary>
    Inferred = 0,

    I32,
    I16,
    U8,
    Bool,
    Utf8,
    F32,
    I64,
    F64,

    /// <summary>A height the client reads as a string, always with one decimal, e.g. "1.0".</summary>
    HeightString,

    /// <summary>An integer the client reads as a string and parses back, e.g. a credit balance.</summary>
    IntegerString,

    /// <summary>An integer the client reads as a string and parses base 16, e.g. group colours.</summary>
    HexString,

    /// <summary>As a list's count (<see cref="WireCountAttribute"/>): no count at all, for a list whose length the
    /// client already knows from what it read before it. Outgoing only.</summary>
    Uncounted,
}

/// <summary>An outgoing message. The generator emits the composer for it.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MessageAttribute(ushort header) : Attribute
{
    public ushort Header { get; } = header;
}

/// <summary>A reusable group of fields written inline wherever it appears.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class WireStructAttribute : Attribute;

/// <summary>Overrides how a value is written. On a list, applies to the elements.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
public sealed class WireFormatAttribute(WireType type) : Attribute
{
    public WireType Type { get; } = type;
}

/// <summary>Overrides the width of a list's count prefix, which defaults to <see cref="WireType.I32"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
public sealed class WireCountAttribute(WireType type) : Attribute
{
    public WireType Type { get; } = type;
}

/// <summary>Part of a bitmask group. A nullable member sets its bit and writes its payload when
/// present; a bool member is the bit itself and writes nothing. The mask is written once, where
/// the first flagged member sits, and payloads follow in ascending bit order.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
public sealed class WireFlagAttribute(int bit) : Attribute
{
    public int Bit { get; } = bit;
}

/// <summary>Writes this member only when the named bool member of the same type is true.
/// Unlike [WireFlag] there is no mask on the wire: the client decides what follows from a
/// value it has already read, so both sides must agree on the same condition.
/// The name is a string because nameof cannot reach a type's members from an attribute on a
/// primary constructor parameter; the generator resolves it and fails the build (HAB009) when
/// it does not name a bool member, so a rename still cannot silently break the layout.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class WireWhenAttribute(string condition) : Attribute
{
    /// <summary>The name of a bool member on the declaring type.</summary>
    public string Condition { get; } = condition;
}

/// <summary>An incoming message. The generator emits the reader for it, and the handler
/// registry takes the header from here rather than from the handler.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IncomingAttribute(ushort header) : Attribute
{
    public ushort Header { get; } = header;
}
