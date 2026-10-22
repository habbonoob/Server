using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Habbo.Protocol.Generator;

/// <summary>Generates the wire writer for every [Message] and [WireStruct], plus a manifest
/// describing each layout so the protocol tests can diff it against the client's parsers.</summary>
[Generator]
public sealed class WireGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var messages = context.SyntaxProvider.ForAttributeWithMetadataName(
                WireModelBuilder.MessageAttribute,
                predicate: static (_, _) => true,
                transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol)
            .Collect();

        var incoming = context.SyntaxProvider.ForAttributeWithMetadataName(
                WireModelBuilder.IncomingAttribute,
                predicate: static (_, _) => true,
                transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol)
            .Collect();

        var structs = context.SyntaxProvider.ForAttributeWithMetadataName(
                WireModelBuilder.WireStructAttribute,
                predicate: static (_, _) => true,
                transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol)
            .Collect();

        context.RegisterSourceOutput(messages.Combine(structs).Combine(incoming), static (spc, pair) =>
        {
            var ((messageTypes, structTypes), incomingTypes) = pair;
            var diagnostics = new List<Diagnostic>();
            var models = new List<WireType_Model>();

            foreach (var type in structTypes)
            {
                var model = WireModelBuilder.Build(type, isMessage: false, diagnostics);
                if (model is not null)
                    models.Add(model);
            }

            foreach (var type in messageTypes)
            {
                var model = WireModelBuilder.Build(type, isMessage: true, diagnostics);
                if (model is not null)
                    models.Add(model);
            }

            foreach (var type in incomingTypes)
            {
                var model = WireModelBuilder.Build(type, isMessage: true, diagnostics, isIncoming: true);
                if (model is not null)
                    models.Add(model);
            }

            var readable = ReadableStructs(models);
            foreach (var model in ReadTypes(models, readable))
                WireModelBuilder.CheckReadOrder(model, diagnostics);

            foreach (var diagnostic in diagnostics)
                spc.ReportDiagnostic(diagnostic);

            // The hint name carries the namespace. There are hundreds of these, so a flat list of
            // bare type names is hard to find anything in — and two messages with the same name in
            // different namespaces would otherwise ask for the same file and fail the build.
            var varying = VaryingByRelease(models);
            var structWidths = StructWidths(models);
            foreach (var model in models)
                spc.AddSource($"{model.FullName}.Wire.g.cs", WireEmitter.Emit(model, readable, varying, structWidths));

            spc.AddSource("WireManifest.g.cs", Manifest(models));
        });
    }

    /// <summary>The structs that can be read back as well as written, for incoming messages that carry them: those
    /// whose every member can be read, nested structs included. Found by adding structs until none more qualify.</summary>
    private static HashSet<string> ReadableStructs(List<WireType_Model> models)
    {
        var structs = models.Where(m => m.HeaderExpression is null).ToList();
        var readable = new HashSet<string>(System.StringComparer.Ordinal);
        bool added;
        do
        {
            added = false;
            foreach (var model in structs.Where(m => !readable.Contains(m.FullName)))
            {
                if (model.Fields.All(field => WireEmitter.IsReadable(field, readable)))
                {
                    readable.Add(model.FullName);
                    added = true;
                }
            }
        }
        while (added);

        return readable;
    }

    /// <summary>The incoming messages, and the readable structs they read, however deep: the layouts a reader is
    /// generated from and actually run on.</summary>
    private static List<WireType_Model> ReadTypes(List<WireType_Model> models, HashSet<string> readable)
    {
        var read = models.Where(m => m.IsIncoming).ToList();
        var structs = models.Where(m => m.HeaderExpression is null && readable.Contains(m.FullName)).ToList();
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        for (var i = 0; i < read.Count; i++)
        {
            foreach (var name in read[i].Fields.Select(f => f.StructTypeName?.TrimEnd('?')).Where(n => n is not null))
            {
                if (seen.Add(name!))
                    read.AddRange(structs.Where(s => s.FullName == name));
            }
        }

        return read;
    }

    /// <summary>The fewest bytes each struct can be read from, for a list reader to tell a count it cannot hold. A
    /// struct that holds itself counts nothing for the copy inside.</summary>
    private static Dictionary<string, int> StructWidths(List<WireType_Model> models)
    {
        var structs = new Dictionary<string, WireType_Model>(System.StringComparer.Ordinal);
        foreach (var model in models.Where(m => m.HeaderExpression is null))
            structs[model.FullName] = model;

        var widths = new Dictionary<string, int>(System.StringComparer.Ordinal);
        int Width(string name)
        {
            if (widths.TryGetValue(name, out var known))
                return known;
            if (!structs.TryGetValue(name, out var model))
                return 1;

            widths[name] = 0;
            return widths[name] = WireEmitter.MinimumWidth(model, Width);
        }

        foreach (var name in structs.Keys.ToList())
            Width(name);

        return widths;
    }

    /// <summary>The messages and structs a client release lays out differently: those with a [WireSince] or
    /// [WireUntil] member, and those holding such a struct, however deep. Found by adding until none more qualify.</summary>
    private static HashSet<string> VaryingByRelease(List<WireType_Model> models)
    {
        var varying = new HashSet<string>(
            models.Where(m => m.Fields.Any(f => f.Since is not null || f.Until is not null)).Select(m => m.FullName),
            System.StringComparer.Ordinal);
        bool added;
        do
        {
            added = false;
            foreach (var model in models.Where(m => !varying.Contains(m.FullName)))
            {
                if (model.Fields.Any(f => f.StructTypeName is { } name && varying.Contains(name.TrimEnd('?'))))
                {
                    varying.Add(model.FullName);
                    added = true;
                }
            }
        }
        while (added);

        return varying;
    }

    /// <summary>The layouts as a string constant, so a test can read them without loading files.</summary>
    private static string Manifest(List<WireType_Model> models)
    {
        var ordered = models
            .OrderBy(m => m.HeaderValue)
            .ThenBy(m => m.TypeName, System.StringComparer.Ordinal)
            .Select(WireEmitter.Describe);

        var json = "[" + string.Join(",", ordered) + "]";

        var text = new StringBuilder();
        text.AppendLine("// <auto-generated/> Written by Habbo.Protocol.Generator.");
        text.AppendLine();
        text.AppendLine("namespace Habbo.Protocol.Wire;");
        text.AppendLine();
        text.AppendLine("/// <summary>Every generated wire layout, as JSON. The protocol tests diff this");
        text.AppendLine("/// against the client's own parsers to catch a layout drifting apart.</summary>");
        text.AppendLine("public static class WireManifest");
        text.AppendLine("{");
        text.AppendLine($"    public const string Json = {Quote(json)};");
        text.AppendLine("}");
        return text.ToString();
    }

    private static string Quote(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
