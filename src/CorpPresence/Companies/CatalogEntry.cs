using System;
using System.Collections.Generic;

namespace CorpPresence;

internal sealed class CatalogEntry
{
	public string Id;

	public string Page;

	public int MinTier = 1;

	public int WeeklyLimit;

	public int WindowDays = 0;

	public string Label;

	public string LabelEn;

	public int Bundle;

	private GameItem _sample;

	private WeakReference<PlayerStore> _sampleStore;

	private WeakReference<Dictionary<string, string>> _sampleData;

	private string _sampleRun;

	private string _sampleId;

	private string _samplePage;

	private int _sampleDay;

	private long _sampleEpoch;

	private long _sampleGeneration;

	private bool _reading;

	private bool _releasing;

	private bool _retired;

	private bool _priceErrorLogged;

	public static readonly (int MaxValue, int Limit)[] PriceTiers = new(int, int)[4]
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
		return Goods.MakeSpecial(Id) ?? ItemRegistry.Create(Id);
	}

	private bool SampleIsCurrent()
	{
		PlayerStore currentStore = SaveSlot.CurrentStore;
		if (!GameResources.Ready || CatalogPricing.IsResetting || currentStore == null || _sample == null || _retired || _sampleStore == null || !_sampleStore.TryGetTarget(out var target) || target != currentStore || _sampleRun != currentStore.runID || _sampleDay != StoreStation.GetDayCounter() || _sampleEpoch != CatalogPricing.Epoch || _sampleGeneration != GameResources.Generation || _sampleId != Id || _samplePage != Page || StorePlacement.Destroyed(_sample))
		{
			return false;
		}
		if (_sampleData == null)
		{
			return currentStore.modData == null;
		}
		if (_sampleData.TryGetTarget(out var target2))
		{
			return target2 == currentStore.modData;
		}
		return false;
	}

	internal bool ReleaseQuoteSample()
	{
		GameItem sample = _sample;
		if (sample == null)
		{
			return true;
		}
		if (_releasing)
		{
			return false;
		}
		_releasing = true;
		_retired = true;
		try
		{
			if (sample.parentInventory == null && !StorePlacement.Destroyed(sample))
			{
				sample.Destroy();
			}
			_sample = null;
			_sampleStore = null;
			_sampleData = null;
			_sampleRun = (_sampleId = (_samplePage = null));
			return true;
		}
		catch (Exception error)
		{
			WarnPrice(error);
			return false;
		}
		finally
		{
			_releasing = false;
		}
	}

	private void WarnPrice(Exception error)
	{
		if (_priceErrorLogged)
		{
			return;
		}
		_priceErrorLogged = true;
		try
		{
			ModLog.Warn("目录估价失败，暂停该项报价：" + Id + "；" + error);
		}
		catch
		{
		}
	}

	private bool TryValues(bool includeEvents, out long baseline, out long eventValue)
	{
		baseline = (eventValue = 0L);
		if (_reading || !GameResources.Ready || CatalogPricing.IsResetting || SaveSlot.CurrentStore == null)
		{
			return false;
		}
		_reading = true;
		try
		{
			PlayerStore currentStore = SaveSlot.CurrentStore;
			Dictionary<string, string> modData = currentStore.modData;
			string runID = currentStore.runID;
			string id = Id;
			string page = Page;
			int dayCounter = StoreStation.GetDayCounter();
			long epoch = CatalogPricing.Epoch;
			long generation = GameResources.Generation;
			if (!SampleIsCurrent())
			{
				if (!ReleaseQuoteSample())
				{
					return false;
				}
				if (!GameResources.Ready || CatalogPricing.IsResetting || currentStore != SaveSlot.CurrentStore || currentStore.runID != runID || modData != currentStore.modData || Id != id || Page != page || StoreStation.GetDayCounter() != dayCounter || CatalogPricing.Epoch != epoch || GameResources.Generation != generation)
				{
					return false;
				}
				GameItem gameItem = Make();
				if (gameItem == null)
				{
					return false;
				}
				_sample = gameItem;
				CatalogPricing.Track(this);
				_retired = false;
				_sampleStore = new WeakReference<PlayerStore>(currentStore);
				_sampleData = ((modData == null) ? null : new WeakReference<Dictionary<string, string>>(modData));
				_sampleRun = runID;
				_sampleId = id;
				_samplePage = page;
				_sampleDay = dayCounter;
				_sampleEpoch = epoch;
				_sampleGeneration = generation;
				if (!SampleIsCurrent() || gameItem.parentInventory != null)
				{
					ReleaseQuoteSample();
					return false;
				}
			}
			GameItem sample = _sample;
			baseline = sample.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: false, forceMarkup: false, 0L);
			eventValue = ((includeEvents && baseline > 0) ? sample.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: true, forceMarkup: false, 0L) : baseline);
			if (baseline < 0 || eventValue < 0 || !SampleIsCurrent() || sample.parentInventory != null)
			{
				ReleaseQuoteSample();
				return false;
			}
			_priceErrorLogged = false;
			return true;
		}
		catch (Exception error)
		{
			WarnPrice(error);
			ReleaseQuoteSample();
			return false;
		}
		finally
		{
			_reading = false;
		}
	}

	private bool TryTotal(long value, int bundle, out int total)
	{
		total = 0;
		int num = Math.Max(1, bundle);
		if (value < 0 || value > int.MaxValue / num)
		{
			return false;
		}
		total = (int)(value * num);
		return true;
	}

	public int BaseValue()
	{
		if (Id == "@modbox")
		{
			return 500;
		}
		int bundle = Bundle;
		if (!TryValues(includeEvents: false, out var baseline, out var _) || Bundle != bundle || !TryTotal(baseline, bundle, out var total))
		{
			return -1;
		}
		return total;
	}

	public int Quote(double rate)
	{
		if (double.IsNaN(rate) || double.IsInfinity(rate) || rate <= 0.0)
		{
			return 0;
		}
		double num;
		if (Id == "@modbox")
		{
			num = 500.0;
		}
		else
		{
			int bundle = Bundle;
			if (!TryValues(Settings.OrderEvents, out var _, out var eventValue) || Bundle != bundle || !TryTotal(eventValue, bundle, out var total))
			{
				return 0;
			}
			num = total;
		}
		double num2 = Math.Round(num * rate);
		if (double.IsNaN(num2) || double.IsInfinity(num2) || num2 > 2147483647.0)
		{
			return 0;
		}
		return Math.Max(1, (int)num2);
	}

	public double EventFactor()
	{
		if (!Settings.OrderEvents || Id == "@modbox")
		{
			return 1.0;
		}
		if (!TryValues(includeEvents: true, out var baseline, out var eventValue) || baseline <= 0)
		{
			return 1.0;
		}
		return (double)eventValue / (double)baseline;
	}

	public int Limit()
	{
		if (WeeklyLimit > 0)
		{
			return WeeklyLimit;
		}
		Func<CatalogEntry, int?> f = Ext.Limit;
		int? num = ((f == null) ? null : Ext.Ask("Limit", () => f(this), null));
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
		// Resolve the current language from our owned sample; never destroy a factory
		// result that has become attached to player inventory during a callback.
		string label = L.En ? LabelEn : Label;
		if (!string.IsNullOrEmpty(label)) return label;
		if (!TryValues(false, out _, out _)) return Id;
		try
		{
			string name = _sample.GetDisplayName()?.TrimEnd('。', '.');
			if (!SampleIsCurrent() || _sample.parentInventory != null) return Id;
			return string.IsNullOrEmpty(name) ? Id : name;
		}
		catch (Exception error) { WarnPrice(error); ReleaseQuoteSample(); return Id; }
	}
}
