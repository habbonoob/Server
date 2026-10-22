using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>290 UserEventCatsComposer — the categories room promotions are listed under.</summary>
[Message(ServerMessages.UserEventCatsComposer)]
public sealed partial record UserEventCatsComposer(IReadOnlyList<EventCategoryData> Categories)
{
    public static UserEventCatsComposer From(IReadOnlyList<EventCategory> categories) =>
        new(categories.Select(c => new EventCategoryData(c.Id, c.Name, c.Visible)).ToList());
}
