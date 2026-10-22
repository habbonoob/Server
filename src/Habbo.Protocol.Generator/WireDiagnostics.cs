using Microsoft.CodeAnalysis;

namespace Habbo.Protocol.Generator;

/// <summary>Everything the generator refuses to guess at. Each one is a wire layout we would
/// otherwise get silently wrong.</summary>
internal static class WireDiagnostics
{
    private const string Category = "HabboWire";

    public static readonly DiagnosticDescriptor NoPrimaryConstructor = new(
        "HAB001",
        "Wire type needs a primary constructor",
        "'{0}' must declare its fields as primary constructor parameters — that order is the wire order",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedType = new(
        "HAB002",
        "No wire mapping for this type",
        "'{0}.{1}' is of type '{2}', which has no wire mapping. Use a supported primitive, a [WireStruct], a list, or annotate it with [WireFormat]",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateFlagBit = new(
        "HAB003",
        "Two members share a flag bit",
        "'{0}' uses bit {1} for more than one member; every [WireFlag] bit must be distinct",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FlagsOutOfOrder = new(
        "HAB004",
        "Flag members must be declared in ascending bit order",
        "'{0}.{1}' has bit {2}, which is not greater than the previous flag bit {3}. The client reads payloads in ascending bit order, so declaration order must match",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FlagPayloadMustBeNullable = new(
        "HAB005",
        "A payload flag must be nullable",
        "'{0}.{1}' carries [WireFlag] but is neither a bool nor nullable. A flag either is a bool, or is nullable and writes its payload when present",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FlagsMustBeLast = new(
        "HAB006",
        "Flag members must come after the fixed fields",
        "'{0}.{1}' is a fixed field declared after a [WireFlag] member. The mask and its payloads are written last",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotSupportedOnIncoming = new(
        "HAB008",
        "Feature is only available on outgoing messages",
        "'{0}.{1}' uses {2}, which the reader does not support: it reads each member in its place, without a mask",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConditionNotFound = new(
        "HAB009",
        "Wire condition does not name a bool member",
        "'{0}.{1}' has [WireWhen(\"{2}\")], but '{2}' is not a bool member of '{0}'. The condition must name a bool property on the same type",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CountBeforeNotFound = new(
        "HAB010",
        "A list's count must be written before an earlier member",
        "'{0}.{1}' has [WireCountBefore(\"{2}\")], but '{2}' is not a member declared before it. The count can only move ahead of the list",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PresenceMustBeNullable = new(
        "HAB011",
        "A presence-prefixed member must be nullable",
        "'{0}.{1}' carries [WirePresence] but is not nullable, or also carries [WireFlag] or [WireWhen]. The bool before it says whether it has a value",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ReleaseGateNotSupported = new(
        "HAB012",
        "A release-gated member cannot be flagged or have its count moved",
        "'{0}.{1}' carries [WireSince] or [WireUntil] with [WireFlag] or [WireCountBefore], or both [WireSince] and [WireUntil]. Only a plain member can come and go with a release",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OptionalNotTrailing = new(
        "HAB013",
        "An optional member that is read must come last",
        "'{0}.{1}' is optional, so it is read only if bytes are left, but '{2}' comes after it. A member left out would have the next one read in its place; make the later members optional too, or release-gate the optional one",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConditionNotReadYet = new(
        "HAB014",
        "A read member's condition must be a member read before it",
        "'{0}.{1}' has [WireWhen(\"{2}\")], but '{2}' is, or reads, a member that is not read before it on every release. The reader decides on what it has read so far, which would not match what the writer decides on",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidWireFormat = new(
        "HAB007",
        "Wire format does not fit the declared type",
        "'{0}.{1}' is '{2}' but [WireFormat({3})] was requested, which cannot be written from that type",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
}
