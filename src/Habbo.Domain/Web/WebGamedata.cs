namespace Habbo.Domain.Web;

/// <summary>A client-side static resource (external_variables, external_texts, furnidata, productdata)
/// served by the web seam. HabboAir keeps these in the database too — import your existing rows freely.</summary>
public sealed class WebGamedata
{
    public required string Name { get; set; }
    public required string Content { get; set; }
    public required string Hash { get; set; }
}
