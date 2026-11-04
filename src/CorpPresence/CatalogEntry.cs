using System;
using System.Collections.Generic;

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



	private GameItem _sample;

	private string _name;

	private string _nameEn;

    private WeakReference<PlayerStore> _sampleStore;
    private WeakReference<Dictionary<string,string>> _sampleData;
    private string _sampleRun, _sampleId, _samplePage;
    private int _sampleDay;
    private long _sampleEpoch, _sampleGeneration;
    private bool _reading, _releasing, _retired, _priceErrorLogged;
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
		return Goods.MakeSpecial(Id) ?? ItemRegistry.Create(Id);
	}

    private bool SampleIsCurrent()
    {
        var store = SaveSlot.CurrentStore;
        if (!GameResources.Ready || CatalogPricing.IsResetting || store == null || _sample == null || _retired ||
            _sampleStore == null || !_sampleStore.TryGetTarget(out var owner) || !ReferenceEquals(owner, store) ||
            _sampleRun != store.runID || _sampleDay != StoreStation.GetDayCounter() ||
            _sampleEpoch != CatalogPricing.Epoch || _sampleGeneration != GameResources.Generation ||
            _sampleId != Id || _samplePage != Page || StorePlacement.Destroyed(_sample)) return false;
        if (_sampleData == null) return store.modData == null;
        return _sampleData.TryGetTarget(out var data) && ReferenceEquals(data, store.modData);
    }

    internal bool ReleaseQuoteSample()
    {
        var sample = _sample;
        if (sample == null) return true;
        if (_releasing) return false;
        _releasing = true;
        _retired = true;
        try
        {
            // A sample unexpectedly attached by another system is no longer ours to destroy.
            if (sample.parentInventory == null && !StorePlacement.Destroyed(sample)) sample.Destroy();
            _sample = null;
            _sampleStore = null;
            _sampleData = null;
            _sampleRun = _sampleId = _samplePage = null;
            return true;
        }
        catch (Exception error)
        {
            WarnPrice(error); // Keep the retired object for a later cleanup attempt.
            return false;
        }
        finally { _releasing = false; }
    }

    private void WarnPrice(Exception error)
    {
        if (_priceErrorLogged) return;
        _priceErrorLogged = true;
        try { ModLog.Warn("目录估价失败，暂停该项报价：" + Id + "；" + error); }
        catch { }
    }

    private bool TryValues(bool includeEvents, out long baseline, out long eventValue)
    {
        baseline = eventValue = 0;
        if (_reading || !GameResources.Ready || CatalogPricing.IsResetting || SaveSlot.CurrentStore == null) return false;
        _reading = true;
        try
        {
            var store = SaveSlot.CurrentStore;
            var data = store.modData;
            var run = store.runID;
            var id = Id;
            var page = Page;
            int day = StoreStation.GetDayCounter();
            long epoch = CatalogPricing.Epoch, generation = GameResources.Generation;
            if (!SampleIsCurrent())
            {
                if (!ReleaseQuoteSample()) return false;
                // Destruction callbacks can reset or replace the active session.
                if (!GameResources.Ready || CatalogPricing.IsResetting ||
                    !ReferenceEquals(store, SaveSlot.CurrentStore) || store.runID != run ||
                    !ReferenceEquals(data, store.modData) || Id != id || Page != page ||
                    StoreStation.GetDayCounter() != day || CatalogPricing.Epoch != epoch ||
                    GameResources.Generation != generation) return false;
                var sample = Make();
                if (sample == null) return false;
                _sample = sample;
                CatalogPricing.Track(this);
                _retired = false;
                _sampleStore = new WeakReference<PlayerStore>(store);
                _sampleData = data == null ? null : new WeakReference<Dictionary<string,string>>(data);
                _sampleRun = run; _sampleId = id; _samplePage = page;
                _sampleDay = day; _sampleEpoch = epoch; _sampleGeneration = generation;
                if (!SampleIsCurrent() || sample.parentInventory != null)
                {
                    ReleaseQuoteSample();
                    return false;
                }
            }
            var current = _sample;
            baseline = current.GetCurrentValue(useRetailMarkup:false, withChild:true, includeEvents:false, forceMarkup:false, 0L);
            eventValue = includeEvents && baseline > 0
                ? current.GetCurrentValue(useRetailMarkup:false, withChild:true, includeEvents:true, forceMarkup:false, 0L)
                : baseline;
            if (baseline < 0 || eventValue < 0 || !SampleIsCurrent() || current.parentInventory != null)
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
        finally { _reading = false; }
    }

    private bool TryTotal(long value, int bundle, out int total)
    {
        total = 0;
        int count = Math.Max(1, bundle);
        if (value < 0 || value > int.MaxValue / count) return false;
        total = (int)(value * count);
        return true;
    }

    public int BaseValue()
    {
        if (Id == "@modbox") return 500;
        int bundle = Bundle;
        return TryValues(false, out var baseline, out _) && Bundle == bundle && TryTotal(baseline, bundle, out var total) ? total : -1;
    }

    public int Quote(double rate)
    {
        if (double.IsNaN(rate) || double.IsInfinity(rate) || rate <= 0) return 0;
        double value;
        if (Id == "@modbox") value = 500;
        else
        {
            int bundle = Bundle;
            if (!TryValues(Settings.OrderEvents, out _, out var current) || Bundle != bundle || !TryTotal(current, bundle, out var total)) return 0;
            value = total;
        }
        double price = Math.Round(value * rate);
        if (double.IsNaN(price) || double.IsInfinity(price) || price > int.MaxValue) return 0;
        return Math.Max(1, (int)price);
    }

    public double EventFactor()
    {
        if (!Settings.OrderEvents || Id == "@modbox") return 1.0;
        return TryValues(true, out var baseline, out var current) && baseline > 0 ? (double)current / baseline : 1.0;
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

