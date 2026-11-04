using System;

namespace CorpPresence;

internal sealed class CatalogEntry
{
	public string Id;

	public string Page;

	public int MinTier = 1;

	public int WeeklyLimit;

	public int WindowDays;

	public string Label;

	public string LabelEn;

	public int Bundle;

	private int _base = -1;

	private GameItem _sample;

	private string _name;

	private string _nameEn;

	private int? _eventKey;

	private double _eventFactor = 1.0;

	public static readonly (int MaxValue, int Limit)[] PriceTiers = new (int, int)[4]
	{
		(49, 20),
		(199, 10),
		(299, 5),
		(int.MaxValue, 3)
	};

	public GameItem Make()
	{
		if (Id == "@modbox")
		{
			return null;
		}
		if (Id == "@hemingway_genuine")
		{
			return PastelCo.GenuineHemingway();
		}
		if (Id == "@hemingway_limited")
		{
			return PastelCo.GenuineHemingway(limited: true);
		}
		if (Page == "药品")
		{
			return CarmichaelCo.MakeGenuine(Id);
		}
		if (Page == "制式")
		{
			return MeridianCo.MakeNew(Id);
		}
		if (Page == "净水" && Id.StartsWith("@") && !Goods.Has(Id.Substring(1)))
		{
			return DavreCo.MakePrebuilt(Id);
		}
		if (Page == "军械" && Id.StartsWith("@") && !Goods.Has(Id.Substring(1)))
		{
			return SpacerCo.MakeGun(Id.Substring(1));
		}
		return Goods.MakeSpecial(Id) ?? DirectoryMaster.Item(Id, isOwned: false);
	}

	public int BaseValue()
	{
		if (_base >= 0)
		{
			return _base;
		}
		if (Id == "@modbox")
		{
			return _base = 500;
		}
		try
		{
			GameItem gameItem = Make();
			if (gameItem == null)
			{
				return -1;
			}
			_sample = gameItem;
			return _base = (int)gameItem.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: false, forceMarkup: false, 0L) * Math.Max(1, Bundle);
		}
		catch
		{
			return -1;
		}
	}

	public int Quote(double rate)
	{
		int num = BaseValue();
		if (num >= 0)
		{
			return Math.Max(1, (int)Math.Round((double)num * rate * EventFactor()));
		}
		return 0;
	}

	public double EventFactor()
	{
		if (!Settings.OrderEvents || _sample == null)
		{
			return 1.0;
		}
		try
		{
			int num = StoreStation.GetDayCounter();
			foreach (StoreEvent activeEvent in StoreStation.Instance.storeEventManager.GetActiveEvents())
			{
				num = num * 31 + (activeEvent?.identifier?.GetHashCode()).GetValueOrDefault();
			}
			if (num == _eventKey)
			{
				return _eventFactor;
			}
			long currentValue = _sample.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: false, forceMarkup: false, 0L);
			long currentValue2 = _sample.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: true, forceMarkup: false, 0L);
			_eventKey = num;
			return _eventFactor = ((currentValue > 0) ? ((double)currentValue2 / (double)currentValue) : 1.0);
		}
		catch
		{
			return 1.0;
		}
	}

	public int Limit()
	{
		if (WeeklyLimit > 0)
		{
			return WeeklyLimit;
		}
		Func<CatalogEntry, int?> f = Ext.Limit;
		int? num = ((f == null) ? ((int?)null) : Ext.Ask("Limit", () => f(this), null));
		if (num.HasValue)
		{
			return num.Value;
		}
		if (!Settings.OrderLimits)
		{
			return 0;
		}
		int num2 = BaseValue();
		if (num2 >= 0)
		{
			return PriceLimit(num2);
		}
		return 0;
	}

	public static int PriceLimit(int value)
	{
		return Array.Find(PriceTiers, ((int MaxValue, int Limit) t) => value <= t.MaxValue).Limit;
	}

	public string Name()
	{
		bool en = L.En;
		string text = (en ? _nameEn : _name);
		if (text != null)
		{
			return text;
		}
		string text2 = (en ? LabelEn : Label);
		if (string.IsNullOrEmpty(text2))
		{
			GameItem gameItem = null;
			try
			{
				gameItem = Make();
				text2 = gameItem?.GetDisplayName()?.TrimEnd('。', '.');
			}
			catch
			{
			}
			try
			{
				gameItem?.Destroy();
			}
			catch
			{
			}
		}
		if (string.IsNullOrEmpty(text2))
		{
			return Id;
		}
		if (en)
		{
			_nameEn = text2;
		}
		else
		{
			_name = text2;
		}
		return text2;
	}
}
