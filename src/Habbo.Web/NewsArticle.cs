namespace Habbo.Web;

/// <summary>A news article as the website shows it. The image is the hotel view's: a path under the image library,
/// or a full URL.</summary>
public sealed record NewsArticle(int Id, string Title, string Body, string Image);
