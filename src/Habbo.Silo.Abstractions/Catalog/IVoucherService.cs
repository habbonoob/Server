using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>Voucher codes redeemed from the catalog.</summary>
public interface IVoucherService
{
    Task RedeemAsync(PlayerData player, string code);
}
