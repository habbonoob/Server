namespace Habbo.Bundles;

/// <summary>A named text of a library: its index, assets, visualization or logic, a window layout, a skin.</summary>
public sealed record BundleDocument(string Name, BundleDocumentKind Kind, string Text);
