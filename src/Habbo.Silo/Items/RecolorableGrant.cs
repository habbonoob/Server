using System.Globalization;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Items.Interactions;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Items;

/// <summary>A recolourable furni (Builders Club's bc_*_recolorable) bought or borrowed in the colour picked from the
/// catalog's palette: the client sends it as the offer's extra parameter, "r,g,b" (RecolorableFurnitureCatalogWidget),
/// and the furni keeps it in its map data as the recolour window would give it.</summary>
public static class RecolorableGrant
{
    /// <summary>Gives the new item the colour picked, when it is recolourable and its palette has the colour, with the
    /// state its product names kept beside it; true when it did, so its behaviour's own data is not given as well.</summary>
    public static async Task<bool> GrantColoredAsync(IClientVariables variables, FurniGrant grant)
    {
        if (grant.Definition.Logic != WiredInternalVariableAccess.RecolorableLogic
            || Parse(grant.ClientParam) is not var (red, green, blue)
            || !(await RecolorPalettes.ForAsync(variables, grant.Definition.ClassName)).Contains(red << 16 | green << 8 | blue))
            return false;

        FurniDataStore.Attach(grant.Db, grant.Item,
            WiredInternalVariableAccess.WithColor(FurniDataView.WithState(grant.ProductParam), red, green, blue));
        return true;
    }

    /// <summary>"r,g,b", each 0 to 255.</summary>
    private static (int Red, int Green, int Blue)? Parse(string text)
    {
        var parts = text.Split(',');
        if (parts.Length != 3)
            return null;

        var channels = new int[3];
        for (var i = 0; i < 3; i++)
        {
            if (!int.TryParse(parts[i].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out channels[i])
                || channels[i] > byte.MaxValue)
                return null;
        }

        return (channels[0], channels[1], channels[2]);
    }
}
