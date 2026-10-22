using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>Shared projection for the two category messages, which carry identical rows.</summary>
internal static class FlatCategoryDataFactory
{
    public static FlatCategoryData From(FlatCategory category) => new(
        category.NodeId,
        category.NodeName,
        category.Visible,
        category.Automatic,
        category.AutomaticCategoryKey,
        category.GlobalCategoryKey,
        category.StaffOnly);
}
