using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Habbo.Protocol.Generator;

/// <summary>Turns a [Message] or [WireStruct] symbol into a layout, reporting anything it cannot
/// resolve rather than guessing. Primary constructor parameter order is the wire order.</summary>
internal static class WireModelBuilder
{
    public const string MessageAttribute = "Habbo.Protocol.Wire.MessageAttribute";
    public const string WireStructAttribute = "Habbo.Protocol.Wire.WireStructAttribute";
    public const string WireFormatAttribute = "Habbo.Protocol.Wire.WireFormatAttribute";
    public const string WireCountAttribute = "Habbo.Protocol.Wire.WireCountAttribute";
    public const string WireFlagAttribute = "Habbo.Protocol.Wire.WireFlagAttribute";
    public const string WireWhenAttribute = "Habbo.Protocol.Wire.WireWhenAttribute";
    public const string WireCountBeforeAttribute = "Habbo.Protocol.Wire.WireCountBeforeAttribute";
    public const string WirePresenceAttribute = "Habbo.Protocol.Wire.WirePresenceAttribute";
    public const string IncomingAttribute = "Habbo.Protocol.Wire.IncomingAttribute";
    public const string WireSinceAttribute = "Habbo.Protocol.Wire.WireSinceAttribute";
    public const string WireUntilAttribute = "Habbo.Protocol.Wire.WireUntilAttribute";

    public static WireType_Model? Build(
        INamedTypeSymbol type, bool isMessage, List<Diagnostic> diagnostics, bool isIncoming = false)
    {
        var constructor = PrimaryConstructor(type);

        // A message may have no body at all; a struct with nothing in it would be a mistake.
        if (constructor is null && !isIncoming && (!isMessage || DeclaresProperties(type)))
        {
            diagnostics.Add(Diagnostic.Create(
                WireDiagnostics.NoPrimaryConstructor, Location(type), type.Name));
            return null;
        }

        var model = new WireType_Model
        {
            Namespace = type.ContainingNamespace.IsGlobalNamespace
                ? ""
                : type.ContainingNamespace.ToDisplayString(),
            TypeName = type.Name,
        };

        model.IsIncoming = isIncoming;

        if (isMessage)
        {
            var wanted = isIncoming ? IncomingAttribute : MessageAttribute;
            var attribute = type.GetAttributes()
                .First(a => a.AttributeClass?.ToDisplayString() == wanted);
            model.HeaderValue = attribute.ConstructorArguments.Length > 0
                                && attribute.ConstructorArguments[0].Value is not null
                ? System.Convert.ToInt32(attribute.ConstructorArguments[0].Value)
                : -1;
            model.HeaderExpression = model.HeaderValue.ToString();
        }

        var seenFlag = false;
        var previousBit = int.MinValue;
        var usedBits = new HashSet<int>();

        var parameters = constructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;

        foreach (var parameter in parameters)
        {
            var field = Resolve(type, parameter, diagnostics);
            if (field is null)
                continue;

            if (field.FlagBit is { } bit)
            {
                seenFlag = true;

                if (!usedBits.Add(bit))
                {
                    diagnostics.Add(Diagnostic.Create(
                        WireDiagnostics.DuplicateFlagBit, Location(parameter), type.Name, bit));
                }
                else if (bit <= previousBit)
                {
                    diagnostics.Add(Diagnostic.Create(
                        WireDiagnostics.FlagsOutOfOrder, Location(parameter),
                        type.Name, field.Name, bit, previousBit));
                }

                previousBit = bit;
            }
            else if (seenFlag)
            {
                diagnostics.Add(Diagnostic.Create(
                    WireDiagnostics.FlagsMustBeLast, Location(parameter), type.Name, field.Name));
            }

            if (isIncoming && field.FlagBit is not null)
            {
                diagnostics.Add(Diagnostic.Create(
                    WireDiagnostics.NotSupportedOnIncoming, Location(parameter),
                    type.Name, field.Name, "[WireFlag]"));
                continue;
            }

            // the reader reads values the client writes as themselves, and a list's count off the wire; a formatted
            // string or an uncounted list would be read as something else, silently out of step with the client
            if (isIncoming && (field.Type is WireType.HeightString or WireType.IntegerString or WireType.HexString
                               || (field.Kind == FieldKind.List && field.CountType == WireType.Uncounted)))
            {
                diagnostics.Add(Diagnostic.Create(
                    WireDiagnostics.NotSupportedOnIncoming, Location(parameter),
                    type.Name, field.Name, field.Kind == FieldKind.List ? "an uncounted list" : $"[WireFormat({field.Type})]"));
                continue;
            }

            if (field.CountBefore is { } countBefore)
            {
                // the reader reads each list whole, in its place
                if (isIncoming)
                {
                    diagnostics.Add(Diagnostic.Create(
                        WireDiagnostics.NotSupportedOnIncoming, Location(parameter),
                        type.Name, field.Name, "[WireCountBefore]"));
                    continue;
                }

                if (field.Kind != FieldKind.List || field.Condition is not null || field.Optional || field.Presence
                    || model.Fields.All(earlier => earlier.Name != countBefore || earlier.FlagBit is not null))
                {
                    diagnostics.Add(Diagnostic.Create(
                        WireDiagnostics.CountBeforeNotFound, Location(parameter),
                        type.Name, field.Name, countBefore));
                    continue;
                }
            }

            model.Fields.Add(field);
        }

        return model;
    }

    /// <summary>What a layout that is read back must also hold to, since the reader decides as it goes: an optional
    /// member is read only if bytes are left, so only optional members may follow it, and a struct read in a message
    /// may not have one at all; and a condition is read off
    /// the half-filled value, so it must name a member read before, on every release, whatever its own condition.
    /// An optional member that is release-gated is nullable for the releases without it, not left out at will.</summary>
    public static void CheckReadOrder(WireType_Model model, List<Diagnostic> diagnostics)
    {
        WireField? optional = null;
        for (var i = 0; i < model.Fields.Count; i++)
        {
            var field = model.Fields[i];
            if (optional is not null && !field.Optional)
            {
                diagnostics.Add(Diagnostic.Create(
                    WireDiagnostics.OptionalNotTrailing, optional.Location, model.TypeName, optional.Name, field.Name));
                optional = null;
            }

            if (field.Optional && field.Since is null && field.Until is null)
                optional ??= field;

            // a computed condition is fine as long as every member it reads is read before it, on every release
            var earlier = model.Fields.Take(i)
                .Where(f => f.Condition is null && f.FlagBit is null && f.Since is null && f.Until is null)
                .Select(f => f.Name)
                .ToList();
            if (field.Condition is { } condition
                && field.ConditionReads.Any(read => model.Fields.Any(f => f.Name == read) && !earlier.Contains(read)))
            {
                diagnostics.Add(Diagnostic.Create(
                    WireDiagnostics.ConditionNotReadYet, field.Location, model.TypeName, field.Name, condition));
            }
        }

        // a struct is read in the middle of something, so even its last member has bytes after it
        if (optional is not null && !model.IsIncoming)
        {
            diagnostics.Add(Diagnostic.Create(
                WireDiagnostics.OptionalNotTrailing, optional.Location, model.TypeName, optional.Name, "what follows the struct"));
        }
    }

    /// <summary>The constructor whose parameters are the record's positional members. A record
    /// also gets a compiler-generated copy constructor, which must not be mistaken for it.</summary>
    private static IMethodSymbol? PrimaryConstructor(INamedTypeSymbol type) =>
        type.InstanceConstructors
            .Where(c => c.Parameters.Length > 0 && !c.IsStatic && !IsCopyConstructor(c, type))
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();

    private static bool IsCopyConstructor(IMethodSymbol constructor, INamedTypeSymbol type) =>
        constructor.Parameters.Length == 1
        && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, type);

    private static WireField? Resolve(
        INamedTypeSymbol owner, IParameterSymbol parameter, List<Diagnostic> diagnostics)
    {
        var field = new WireField
        {
            Name = PropertyName(parameter),
            DeclaredType = parameter.Type.ToDisplayString(),
            Location = Location(parameter),
        };

        if (TryGetInt(parameter, WireFlagAttribute, out var bit))
            field.FlagBit = bit;

        if (TryGetString(parameter, WireCountBeforeAttribute, out var countBefore))
            field.CountBefore = countBefore;

        if (TryGetString(parameter, WireSinceAttribute, out var since))
            field.Since = since;

        if (TryGetString(parameter, WireUntilAttribute, out var until))
            field.Until = until;

        if ((field.Since is not null || field.Until is not null)
            && (field.FlagBit is not null || field.CountBefore is not null || (field.Since is not null && field.Until is not null)))
        {
            diagnostics.Add(Diagnostic.Create(
                WireDiagnostics.ReleaseGateNotSupported, Location(parameter), owner.Name, field.Name));
            return null;
        }

        if (TryGetString(parameter, WireWhenAttribute, out var condition))
        {
            if (!IsBoolMember(owner, condition))
            {
                diagnostics.Add(Diagnostic.Create(
                    WireDiagnostics.ConditionNotFound, Location(parameter),
                    owner.Name, field.Name, condition));
                return null;
            }

            field.Condition = condition;
            field.ConditionReads = ConditionReads(owner, condition);
        }

        var type = parameter.Type;
        var requested = TryGetWireType(parameter, WireFormatAttribute);

        // A nullable payload flag is written when present; unwrap to what it holds.
        var isNullable = type.NullableAnnotation == NullableAnnotation.Annotated
                         || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        if (field.FlagBit is not null)
        {
            var isBool = type.SpecialType == SpecialType.System_Boolean;
            if (!isBool && !isNullable)
            {
                diagnostics.Add(Diagnostic.Create(
                    WireDiagnostics.FlagPayloadMustBeNullable, Location(parameter),
                    owner.Name, field.Name));
                return null;
            }

            field.FlagHasPayload = !isBool;
        }

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && type is INamedTypeSymbol { TypeArguments.Length: 1 } nullable)
        {
            type = nullable.TypeArguments[0];
            field.NullableValueType = true;
        }

        // A nullable member with no flag is an optional field: the client only reads it when
        // we send it, so it is written only when it has a value. A [WireWhen] member is
        // nullable for a different reason — the condition, not the value, decides — so the
        // condition governs and a null under a true condition is a bug we let throw rather
        // than silently drop a field the client is waiting for.
        if (HasAttribute(parameter, WirePresenceAttribute))
        {
            if (!isNullable || field.FlagBit is not null || field.Condition is not null)
            {
                diagnostics.Add(Diagnostic.Create(
                    WireDiagnostics.PresenceMustBeNullable, Location(parameter), owner.Name, field.Name));
                return null;
            }

            field.Presence = true;
        }

        if (field.FlagBit is null && isNullable && field.Condition is null && !field.Presence)
            field.Optional = true;

        if (TryGetElementType(type, out var element))
        {
            field.Kind = FieldKind.List;
            field.CountType = TryGetWireType(parameter, WireCountAttribute) ?? WireType.I32;

            if (IsWireStruct(element))
            {
                field.ElementIsStruct = true;
                field.StructTypeName = element.ToDisplayString();
                return field;
            }

            var elementType = requested ?? Infer(element);
            if (elementType is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    WireDiagnostics.UnsupportedType, Location(parameter),
                    owner.Name, field.Name, element.ToDisplayString()));
                return null;
            }

            field.Type = elementType.Value;
            return field;
        }

        if (IsWireStruct(type))
        {
            field.Kind = FieldKind.Struct;
            field.StructTypeName = type.ToDisplayString();
            return field;
        }

        var resolved = requested ?? Infer(type);
        if (resolved is null)
        {
            diagnostics.Add(Diagnostic.Create(
                WireDiagnostics.UnsupportedType, Location(parameter),
                owner.Name, field.Name, type.ToDisplayString()));
            return null;
        }

        if (!FitsDeclaredType(resolved.Value, type))
        {
            diagnostics.Add(Diagnostic.Create(
                WireDiagnostics.InvalidWireFormat, Location(parameter),
                owner.Name, field.Name, type.ToDisplayString(), resolved.Value));
            return null;
        }

        field.Kind = FieldKind.Primitive;
        field.Type = resolved.Value;
        if (type.TypeKind == TypeKind.Enum)
            field.EnumTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return field;
    }

    /// <summary>The wire type a CLR type maps to with no attribute. Deliberately narrow: a type
    /// that is not here needs [WireFormat], which forces the ambiguity to be spelled out.</summary>
    private static WireType? Infer(ITypeSymbol type) => type.SpecialType switch
    {
        SpecialType.System_Int32 => WireType.I32,
        SpecialType.System_Int16 => WireType.I16,
        SpecialType.System_Byte => WireType.U8,
        SpecialType.System_Boolean => WireType.Bool,
        SpecialType.System_String => WireType.Utf8,
        SpecialType.System_Single => WireType.F32,
        SpecialType.System_Int64 => WireType.I64,
        SpecialType.System_Double => WireType.F64,
        _ => type.TypeKind == TypeKind.Enum ? WireType.I32 : null,
    };

    /// <summary>Guards against asking for a format the value cannot produce.</summary>
    private static bool FitsDeclaredType(WireType wire, ITypeSymbol type)
    {
        var isNumeric = type.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int16
            or SpecialType.System_Byte or SpecialType.System_Double or SpecialType.System_Single
            or SpecialType.System_Int64
            || type.TypeKind == TypeKind.Enum;

        return wire switch
        {
            WireType.Utf8 => type.SpecialType == SpecialType.System_String,
            WireType.Bool => type.SpecialType == SpecialType.System_Boolean,
            WireType.HeightString or WireType.IntegerString or WireType.HexString => isNumeric,
            _ => isNumeric,
        };
    }

    private static bool IsWireStruct(ITypeSymbol type) =>
        type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == WireStructAttribute);

    /// <summary>Recognises the list shapes we accept; anything else falls through to a diagnostic.</summary>
    private static bool TryGetElementType(ITypeSymbol type, out ITypeSymbol element)
    {
        element = null!;

        if (type is IArrayTypeSymbol array)
        {
            element = array.ElementType;
            return true;
        }

        if (type is not INamedTypeSymbol { IsGenericType: true } named)
            return false;

        var definition = named.ConstructedFrom.ToDisplayString();
        var isList = definition is "System.Collections.Generic.List<T>"
            or "System.Collections.Generic.IReadOnlyList<T>"
            or "System.Collections.Generic.IList<T>"
            or "System.Collections.Generic.IEnumerable<T>"
            or "System.Collections.Generic.ICollection<T>"
            or "System.Collections.Generic.IReadOnlyCollection<T>";

        if (!isList)
            return false;

        element = named.TypeArguments[0];
        return true;
    }

    private static string PropertyName(IParameterSymbol parameter) =>
        char.ToUpperInvariant(parameter.Name[0]) + parameter.Name.Substring(1);

    private static WireType? TryGetWireType(IParameterSymbol parameter, string attributeName)
    {
        var attribute = Attributes(parameter)
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeName);

        if (attribute is null || attribute.ConstructorArguments.Length == 0)
            return null;

        return (WireType)System.Convert.ToInt32(attribute.ConstructorArguments[0].Value);
    }

    /// <summary>A [WireWhen] condition has to resolve to something the generated writer can
    /// put in an if, on the same type it is writing.</summary>
    private static bool IsBoolMember(INamedTypeSymbol owner, string name) =>
        owner.GetMembers(name).Any(member => member switch
        {
            IPropertySymbol property => property.Type.SpecialType == SpecialType.System_Boolean,
            IFieldSymbol field => field.Type.SpecialType == SpecialType.System_Boolean,
            _ => false,
        });

    /// <summary>The condition's own name and every name its declaration mentions: a computed one (IsFurni =&gt; Type ==
    /// Furni) depends on the members it reads.</summary>
    private static List<string> ConditionReads(INamedTypeSymbol owner, string name) =>
        new[] { name }
            .Concat(owner.GetMembers(name)
                .SelectMany(member => member.DeclaringSyntaxReferences)
                .SelectMany(reference => reference.GetSyntax().DescendantNodes())
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax>()
                .Select(identifier => identifier.Identifier.ValueText))
            .Distinct()
            .ToList();

    private static bool HasAttribute(IParameterSymbol parameter, string attributeName) =>
        Attributes(parameter).Any(a => a.AttributeClass?.ToDisplayString() == attributeName);

    /// <summary>Whether a type declares members of its own that would have been its fields.</summary>
    private static bool DeclaresProperties(INamedTypeSymbol type) =>
        type.GetMembers().OfType<IPropertySymbol>()
            .Any(p => !p.IsStatic && !p.IsImplicitlyDeclared && p.Name != "EqualityContract");

    private static bool TryGetString(IParameterSymbol parameter, string attributeName, out string value)
    {
        value = "";
        var attribute = Attributes(parameter)
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeName);

        if (attribute is null || attribute.ConstructorArguments.Length == 0)
            return false;

        value = attribute.ConstructorArguments[0].Value?.ToString() ?? "";
        return value.Length > 0;
    }

    private static bool TryGetInt(IParameterSymbol parameter, string attributeName, out int value)
    {
        value = 0;
        var attribute = Attributes(parameter)
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeName);

        if (attribute is null || attribute.ConstructorArguments.Length == 0)
            return false;

        value = System.Convert.ToInt32(attribute.ConstructorArguments[0].Value);
        return true;
    }

    /// <summary>Attributes written as [property: X] land on the generated property, not the
    /// parameter, so both places have to be checked.</summary>
    private static IEnumerable<AttributeData> Attributes(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
            yield return attribute;

        var property = parameter.ContainingType
            .GetMembers(PropertyName(parameter))
            .OfType<IPropertySymbol>()
            .FirstOrDefault();

        if (property is null)
            yield break;

        foreach (var attribute in property.GetAttributes())
            yield return attribute;
    }

    private static Location Location(ISymbol symbol) =>
        symbol.Locations.FirstOrDefault() ?? Microsoft.CodeAnalysis.Location.None;
}
