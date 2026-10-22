using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Moderation;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.CallForHelp;

/// <summary>Reads the report reasons the client's help tool offers. They are the same for
/// everyone and change rarely, so they are read once and kept.</summary>
public sealed class CallForHelpService(IDbContextFactory<AppDbContext> dbFactory) : ICallForHelpService
{
    private IReadOnlyList<CallForHelpCategoryView>? _categories;

    public async Task<IReadOnlyList<CallForHelpCategoryView>> GetCategoriesAsync(CancellationToken ct = default)
    {
        if (_categories is not null)
            return _categories;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var categories = await db.CallForHelpCategories
            .AsNoTracking()
            .Include(category => category.Topics)
            .OrderBy(category => category.SortOrder)
            .ToListAsync(ct);

        return _categories = categories
            .Select(category => new CallForHelpCategoryView(
                category.Name,
                category.Topics
                    .OrderBy(topic => topic.SortOrder)
                    .Select(topic => new CallForHelpTopicView(topic.Name, topic.Id, topic.Consequence))
                    .ToList()))
            .ToList();
    }
}
