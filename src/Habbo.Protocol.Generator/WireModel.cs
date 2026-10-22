using System.Collections.Generic;

namespace Habbo.Protocol.Generator;

/// <summary>How one value is written. Mirrors Habbo.Protocol.Wire.WireType by name.</summary>
internal enum WireType
{
    Inferred,
    I32,
    I16,
    U8,
    Bool,
    Utf8,
    F32,
    I64,
    F64,
    HeightString,
    IntegerString,
    HexString,
    Uncounted,
}

/// <summary>What a field turned out to be once its type was resolved.</summary>
internal enum FieldKind
{
    /// <summary>A single value written with a MessageWriter call.</summary>
    Primitive,

    /// <summary>Another [WireStruct], written inline.</summary>
    Struct,

    /// <summary>A count prefix followed by elements.</summary>
    List,
}

/// <summary>One member of a message or struct, in wire order.</summary>
internal sealed class WireField
{
    public string Name = "";
    public FieldKind Kind;

    /// <summary>For Primitive, and for List whose elements are primitives.</summary>
    public WireType Type;

    /// <summary>For Struct, and for List whose elements are structs.</summary>
    public string? StructTypeName;

    /// <summary>For List: how the count is written.</summary>
    public WireType CountType = WireType.I32;

    /// <summary>For List with [WireCountBefore]: the earlier member its count is written in front of.</summary>
    public string? CountBefore;

    /// <summary>For List: whether the elements are another [WireStruct].</summary>
    public bool ElementIsStruct;

    /// <summary>Set when the member carries [WireFlag].</summary>
    public int? FlagBit;

    /// <summary>A nullable member with no flag: written only when it has a value.</summary>
    public bool Optional;

    /// <summary>Set when the member carries [WirePresence]: a bool for whether it has a value goes first.</summary>
    public bool Presence;

    /// <summary>Set when the member carries [WireWhen]: the bool member gating it.</summary>
    public string? Condition;

    /// <summary>For [WireWhen]: the names the condition is, or its declaration mentions, which a reader must have
    /// read already.</summary>
    public List<string> ConditionReads = new();

    /// <summary>Set when the member carries [WireSince]: the release that added it.</summary>
    public string? Since;

    /// <summary>Set when the member carries [WireUntil]: the release that dropped it.</summary>
    public string? Until;

    /// <summary>For Primitive: the enum the value is, which a read casts to; null for anything else.</summary>
    public string? EnumTypeName;

    /// <summary>True when the member is a nullable value type, which needs .Value to write.</summary>
    public bool NullableValueType;

    /// <summary>A flagged member that writes a payload when present, rather than being the bit itself.</summary>
    public bool FlagHasPayload;

    /// <summary>The declared type, used in diagnostics.</summary>
    public string DeclaredType = "";

    /// <summary>Where the member is declared, for diagnostics reported once every layout is known.</summary>
    public Microsoft.CodeAnalysis.Location Location = Microsoft.CodeAnalysis.Location.None;
}

/// <summary>A message or struct the generator emits a writer for.</summary>
internal sealed class WireType_Model
{
    public string Namespace = "";
    public string TypeName = "";

    /// <summary>Null for a [WireStruct]; set for a [Message] or [Incoming].</summary>
    public string? HeaderExpression;

    /// <summary>True when this is an incoming message: the generator emits a reader, not a writer.</summary>
    public bool IsIncoming;

    /// <summary>The header's numeric value, for the manifest.</summary>
    public int HeaderValue = -1;

    public readonly List<WireField> Fields = new();

    public string FullName => string.IsNullOrEmpty(Namespace) ? TypeName : Namespace + "." + TypeName;
}
