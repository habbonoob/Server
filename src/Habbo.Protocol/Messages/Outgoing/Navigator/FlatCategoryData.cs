using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>One row of UserFlatCats / UserEventCats.</summary>
[WireStruct]
public sealed partial record FlatCategoryData(
    int NodeId,
    string NodeName,
    bool Visible,
    bool Automatic,
    string AutomaticCategoryKey,
    string GlobalCategoryKey,
    bool StaffOnly);
