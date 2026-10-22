namespace Habbo.Domain.LandingView;

/// <summary>An article the landing view promotes, and where its button leads.</summary>
public sealed class PromoArticle
{
    /// <summary>The button opens a web page.</summary>
    public const int WebLink = 0;

    /// <summary>The button follows a link inside the client.</summary>
    public const int ClientLink = 1;

    /// <summary>The article has no button.</summary>
    public const int NoLink = 2;

    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string BodyText { get; set; } = string.Empty;

    public string ButtonText { get; set; } = string.Empty;

    public int LinkType { get; set; } = NoLink;

    public string LinkContent { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Articles show in this order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Only active articles show.</summary>
    public bool Active { get; set; } = true;
}
