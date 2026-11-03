using System.Xml.Linq;
using Habbo.Domain.Furniture;
using Habbo.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Habbo.Silo.Marketplace;

/// <summary>Which product each NFT is. The NFT marketplace names an NFT by its productdata code, which is not always its
/// furni's class name: habbo.com lists the furni nft_a0club_sofa as nft_a0_clubsofa, and answers stats asked for by the
/// class name with nothing (habbo.com, 2026-11-03). Built once, from the hotel's productdata (the gamedata the
/// client is served), its catalog and its furni.</summary>
public sealed class NftProducts(IDbContextFactory<AppDbContext> dbFactory, ILogger<NftProducts> logger)
{
    private const string ProductDataName = "productdata";

    private NftProductMap? _map;
    private readonly SemaphoreSlim _loading = new(1, 1);

    public async Task<NftProductMap> MapAsync()
    {
        if (_map is not null)
            return _map;

        await _loading.WaitAsync();
        try
        {
            return _map ??= await LoadAsync();
        }
        finally
        {
            _loading.Release();
        }
    }

    private async Task<NftProductMap> LoadAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var definitions = await db.ItemDefinitions.AsNoTracking().Where(d => d.ClassName.StartsWith(ItemDefinition.NftClassPrefix))
            .Select(d => new NftDefinition(d.Id, d.ClassName, d.Name)).ToListAsync();
        var definitionIds = definitions.Select(d => d.Id).ToList();
        var catalog = await db.CatalogProducts.AsNoTracking()
            .Where(p => p.DefinitionId != null && definitionIds.Contains(p.DefinitionId.Value))
            .Select(p => new NftCatalogName(p.DefinitionId!.Value, p.Offer!.LocalizationId)).ToListAsync();
        var productData = await db.WebGamedata.AsNoTracking().Where(g => g.Name == ProductDataName).Select(g => g.Content).FirstOrDefaultAsync();
        return NftProductMap.Build(definitions, ParseProducts(productData), catalog);
    }

    /// <summary>The products in productdata (XML, as Habbo serves it: product elements with a code and a description).</summary>
    private List<NftProduct> ParseProducts(string? productData)
    {
        if (string.IsNullOrWhiteSpace(productData))
            return [];

        try
        {
            return XDocument.Parse(productData).Descendants("product")
                .Select(p => new NftProduct((string?)p.Attribute("code") ?? string.Empty, (string?)p.Element("description") ?? string.Empty))
                .Where(p => p.Code.Length > 0)
                .ToList();
        }
        catch (System.Xml.XmlException e)
        {
            logger.LogWarning(e, "The hotel's productdata could not be read; NFTs are named by their class names");
            return [];
        }
    }
}

public sealed record NftDefinition(int Id, string ClassName, string Name);

public sealed record NftProduct(string Code, string Description);

/// <summary>A catalog offer's name (its localization id, a productdata code) for furni it sells.</summary>
public sealed record NftCatalogName(int DefinitionId, string LocalizationId);

/// <summary>Each NFT furni's product code, and each product's furni.</summary>
public sealed class NftProductMap
{
    private readonly Dictionary<int, string> _codes;
    private readonly ILookup<string, int> _definitions;

    private NftProductMap(Dictionary<int, string> codes)
    {
        _codes = codes;
        _definitions = codes.ToLookup(c => c.Value, c => c.Key, StringComparer.Ordinal);
    }

    /// <summary>The product code an NFT furni is listed under: what habbo.com was seen listing it as (NftProductCodes); else the name of a catalog offer selling it, when that is a
    /// product; else its class name, when that is one (most are: 1301 of habbo.com's 1584 NFT furni); else the one NFT
    /// product no furni is named after whose description is the furni's name, when exactly one is and no other furni has
    /// that name (how nft_a0club_sofa, "Club sofa", is nft_a0_clubsofa, "Club sofa": the one case recorded, so this rule is
    /// inferred from it, not confirmed against habbo.com); else, with no product to name it, its class name.</summary>
    public static NftProductMap Build(IReadOnlyCollection<NftDefinition> definitions, IReadOnlyCollection<NftProduct> products,
        IEnumerable<NftCatalogName> catalog)
    {
        var known = products.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
        var codes = new Dictionary<int, string>();
        // first what habbo.com was seen listing them as (NftProductCodes), where no rule below would find it
        foreach (var definition in definitions)
        {
            if (NftProductCodes.Recorded.TryGetValue(definition.ClassName, out var recorded))
                codes[definition.Id] = recorded;
        }


        foreach (var named in catalog.GroupBy(c => c.DefinitionId).Where(g => !codes.ContainsKey(g.Key)))
        {
            var names = named.Select(c => c.LocalizationId).Where(known.Contains).Distinct(StringComparer.Ordinal).ToList();
            if (names.Count == 1)
                codes[named.Key] = names[0];
        }

        foreach (var definition in definitions.Where(d => !codes.ContainsKey(d.Id) && known.Contains(d.ClassName)))
            codes[definition.Id] = definition.ClassName;

        // the products left over, by description, and the furni left over, by name: a pair only where each side is alone
        var classNames = definitions.Select(d => d.ClassName).ToHashSet(StringComparer.Ordinal);
        var taken = codes.Values.ToHashSet(StringComparer.Ordinal);
        var spare = products.Where(p => p.Code.StartsWith(ItemDefinition.NftClassPrefix, StringComparison.Ordinal)
                                        && !classNames.Contains(p.Code) && !taken.Contains(p.Code) && p.Description.Trim().Length > 0)
            .GroupBy(p => p.Description.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(p => p.Code).Distinct(StringComparer.Ordinal).Count() == 1)
            .ToDictionary(g => g.Key, g => g.First().Code, StringComparer.OrdinalIgnoreCase);
        var unnamed = definitions.Where(d => !codes.ContainsKey(d.Id) && d.Name.Trim().Length > 0)
            .GroupBy(d => d.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() == 1);
        foreach (var alone in unnamed)
        {
            if (spare.TryGetValue(alone.Key, out var code))
                codes[alone.Single().Id] = code;
        }

        foreach (var definition in definitions.Where(d => !codes.ContainsKey(d.Id)))
            codes[definition.Id] = definition.ClassName;

        return new NftProductMap(codes);
    }

    /// <summary>The product code an NFT furni is listed under.</summary>
    public string CodeOf(ItemDefinition definition) => _codes.GetValueOrDefault(definition.Id) ?? definition.ClassName;

    /// <summary>The furni listed under a product code; none for a code no NFT is listed under (a class name that is not
    /// its furni's code among them).</summary>
    public IReadOnlyList<int> DefinitionsOf(string productCode) => _definitions[productCode].ToList();
}
