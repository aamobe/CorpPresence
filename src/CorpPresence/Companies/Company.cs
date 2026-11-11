using System;
using System.Linq;

namespace CorpPresence;

internal abstract class Company
{
	public static readonly int[] TierAt = new int[3] { 20, 50, 80 };

	public const int CardAt = 20;

	public abstract string Id { get; }

	public abstract string Name { get; }

	public abstract string CharacterId { get; }

	public abstract long PhoneNumber { get; }

	public abstract string[] TierNames { get; }

	public abstract string[] FrontDesk { get; }

	public abstract string FrontDeskDone { get; }

	public abstract string CardFlavor { get; }

	public abstract string DeskName { get; }

	public abstract string DeskAsk { get; }

	public virtual CatalogEntry[] Catalog => null;

	public virtual bool Active => true;

	public string OrderEntryError(CatalogEntry entry, CompanyState state)
	{
		if (!Active || state == null || entry == null || Catalog == null || !Catalog.Contains(entry))
			return UnavailableOrderEntry();
		if (entry.MinTier > CatalogTier(state))
		{
			if (entry.MinTier < 1 || entry.MinTier > TierNames.Length) return UnavailableOrderEntry();
			return L.En ? L.K("ui.carmichael.availability.tierRequired", (FormattableString)$"Opens at {TierNames[entry.MinTier - 1]}") : TierNames[entry.MinTier - 1] + "开放";
		}
		return EquipmentOrderRule.For(entry.Id)?.AccessError(this) ?? ContentOrderError(entry);
	}

	protected virtual string ContentOrderError(CatalogEntry entry) => null;

	protected static string UnavailableOrderEntry() => L.K("companies.services.orders.placeOrder.someItemsAreUnavailableRight", "有货品暂不可用，请稍后再试。尚未扣款。", "Some items are unavailable right now. Try again later. You have not been charged.");

	public int Tier(CompanyState s)
	{
		return TierAt.Count((int t) => s.Coop >= t);
	}

	public string TierName(CompanyState s)
	{
		if (Tier(s) != 0)
		{
			return TierNames[Tier(s) - 1];
		}
		return L.K("companies.company.tierName.unsigned", "未签约", "Unsigned");
	}

	public int NextTierAt(CompanyState s)
	{
		return TierAt.FirstOrDefault((int t) => s.Coop < t);
	}

	public int CatalogTier(CompanyState s)
	{
		int tier = Tier(s);
		if (Malice.Malice.IsInstalled)
		{
			return Ext.Ask("CatalogTier", () => Malice.Malice.CatalogTier(this, s, tier), tier);
		}
		return tier;
	}

	public int OrderUnitPrice(CatalogEntry entry, CompanyState state)
	{
		if (entry == null || state == null) return 0;
		int fixedPrice = EquipmentOrderRule.For(entry.Id)?.FixedPrice ?? 0;
		return fixedPrice > 0 ? fixedPrice : entry.Quote(OrderPriceRate(entry, state));
	}

	protected virtual double OrderPriceRate(CatalogEntry entry, CompanyState state) => OrderRate(this, Tier(state));

	public static double PriceRate(int tier)
	{
		return Settings.OrderMarkup - 0.05 * (double)(Math.Max(1, Math.Min(3, tier)) - 1);
	}

	public static double OrderRate(Company c, int tier)
	{
		double num = PriceRate(tier);
		if (Malice.Malice.IsInstalled)
		{
			return num * Ext.Ask("OrderFactor", () => Malice.Malice.Applies(c) ? Malice.Envoy.OrderFactor() : 1.0, 1.0);
		}
		return num;
	}

	public bool Owns(GameItem item)
	{
		try
		{
			TagState tagState = item?.GetTagReadonly("manufacturer");
			return tagState != null && tagState.GetString() == Id;
		}
		catch
		{
			return false;
		}
	}
}
