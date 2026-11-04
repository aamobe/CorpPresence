using System;

namespace CorpPresence;

internal abstract class Character
{
	private string _prefix;

	public abstract string Id { get; }

	public abstract string Name { get; }

	public abstract string Faction { get; }

	public abstract int[] Thresholds { get; }

	public abstract int CashNeeded { get; }

	public abstract string PortraitRef { get; }

	public abstract int BuyGoodsNeeded { get; }

	public string Prefix => _prefix ?? (_prefix = "corp_" + Id + "_");

	public int Chapters => Thresholds.Length;

	public virtual string[] RefuseIds => Array.Empty<string>();

	public virtual (int Min, int Max) DefaultDailyGap => (Min: 3, Max: 5);

	public virtual bool HasDaily => true;

	public abstract string Portrait(CharacterState s, string kind);

	public abstract bool IsBuyChapter(int chapter);

	public abstract Visit Chapter(int chapter);

	public abstract Visit Daily(CharacterState s);

	public abstract bool Accepts(GameItem item);

	public abstract double PriceMultiplier(GameItem item, string kind);

	public virtual long? FixedPrice(GameItem item, string kind)
	{
		return null;
	}

	public abstract int BuyGoodsHeld();

	public virtual bool IsSide(int chapter)
	{
		return false;
	}

	public virtual bool SideAllowed(int chapter)
	{
		return true;
	}

	public virtual bool DailyBuyer(CharacterState s)
	{
		return false;
	}

	public virtual bool DailyAccepts(GameItem item)
	{
		return Accepts(item);
	}

	public virtual string[] WantTags(bool daily)
	{
		return Array.Empty<string>();
	}

	public virtual string[] WantIds(bool daily)
	{
		return Array.Empty<string>();
	}

	public int NextDailyGap()
	{
		return Settings.DailyGap(this);
	}

	public virtual int Rep()
	{
		return Engine.Reputation(Faction);
	}

	public virtual int CashFor(int chapter)
	{
		return CashNeeded;
	}

	public int PrevMain(int chapter)
	{
		for (int num = chapter - 1; num >= 1; num--)
		{
			if (!IsSide(num))
			{
				return num;
			}
		}
		return 0;
	}

	public virtual int SideAfter(int chapter)
	{
		return PrevMain(chapter);
	}

	public int NextMain(CharacterState s)
	{
		for (int i = s.Chapter + 1; i <= Chapters; i++)
		{
			if (!IsSide(i))
			{
				return i;
			}
		}
		return 0;
	}

	public virtual GameItem MakeItem(string id)
	{
		return Goods.MakeSpecial(id) ?? ItemRegistry.Create(id);
	}

	protected static string Act(string action, string line)
	{
		if (!L.En)
		{
			return "<i>（" + action + "）</i>" + line;
		}
		return "<i>(" + action + ")</i> " + line;
	}

	protected static int CountHeld(Func<GameItem, bool> match)
	{
		try
		{
			PlayerStore currentStore = SaveSlot.CurrentStore;
			if (!GameResources.Ready || currentStore == null)
			{
				return 0;
			}
			int num = 0;
			foreach (GameItem allRepoEligibleItem in currentStore.GetAllRepoEligibleItems())
			{
				if (match(allRepoEligibleItem))
				{
					num++;
				}
			}
			return num;
		}
		catch (Exception ex)
		{
			try
			{
				ModLog.Warn("统计库存失败：" + ex.Message);
			}
			catch
			{
			}
			return 0;
		}
	}

	public static bool HasTag(GameItem item, string tag)
	{
		try
		{
			return item?.IsTag(tag) ?? false;
		}
		catch
		{
			return false;
		}
	}

	public static bool IsLuxury(GameItem item)
	{
		try
		{
			return item?.itemTypes != null && item.itemTypes.Contains("LUXURY_ITEM");
		}
		catch
		{
			return false;
		}
	}
}
