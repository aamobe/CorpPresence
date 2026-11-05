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
		return L.T("未签约", "Unsigned");
	}

	public int NextTierAt(CompanyState s)
	{
		return TierAt.FirstOrDefault((int t) => s.Coop < t);
	}

	public int CatalogTier(CompanyState s)
	{
		int tier = Tier(s);
		Func<Company, CompanyState, int, int> f = Ext.CatalogTier;
		if (f != null)
		{
			return Ext.Ask("CatalogTier", () => f(this, s, tier), tier);
		}
		return tier;
	}

	public static double PriceRate(int tier)
	{
		return Settings.OrderMarkup - 0.05 * (double)(Math.Max(1, Math.Min(3, tier)) - 1);
	}

	public static double OrderRate(Company c, int tier)
	{
		double num = PriceRate(tier);
		Func<Company, double> f = Ext.OrderFactor;
		if (f != null)
		{
			return num * Ext.Ask("OrderFactor", () => f(c), 1.0);
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
