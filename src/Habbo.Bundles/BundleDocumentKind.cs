namespace Habbo.Bundles;

/// <summary>What a document's text is, so the client knows how to read it.</summary>
public enum BundleDocumentKind : byte
{
    Text = 0,
    Xml = 1,
    Json = 2,
}
