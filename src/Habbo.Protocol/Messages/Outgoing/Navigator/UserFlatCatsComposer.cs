using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>1025 UserFlatCatsComposer — the legacy navigator's room categories.</summary>
[Message(ServerMessages.UserFlatCatsComposer)]
public sealed partial record UserFlatCatsComposer(IReadOnlyList<FlatCategoryData> Categories)
{
    public static UserFlatCatsComposer From(IReadOnlyList<FlatCategory> categories) =>
        new(categories.Select(FlatCategoryDataFactory.From).ToList());
}
