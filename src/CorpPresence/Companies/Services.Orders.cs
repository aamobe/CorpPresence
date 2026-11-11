using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class Services
{
	private static bool _placingOrder;
	public static bool CanOrderToday(Company c)
	{
		return Companies.StateOf(c)?.LastOrderDay != Day;
	}

	public static string OrderBlocked(Company c)
	{
		if (Malice.Malice.IsInstalled && c != null)
		{
			return Ext.Ask("OrderBlocked", () => Malice.Malice.OrderBlocked(c), null);
		}
		return null;
	}

	public static string PlaceOrder(Company c, Dictionary<CatalogEntry, int> cart, Func<CatalogEntry, int> unitPrice)
	{
		if (_placingOrder)
			return L.K("companies.services.orders.placeOrder.orderNotReadyHaveNot", "订单未就绪，尚未扣款。", "Order not ready. You have not been charged.");
		_placingOrder = true;
		try { return PlaceOrderCore(c, cart, unitPrice); }
		finally { _placingOrder = false; }
	}

	private static string PlaceOrderCore(Company c, Dictionary<CatalogEntry, int> cart, Func<CatalogEntry, int> unitPrice)
	{
		CompanyState s = Companies.StateOf(c);
		StoreSession session = StoreSession.Capture();
		if (s == null || s.Orders == null || s.LimitedBought == null)
		{
			return L.K("companies.services.orders.placeOrder.saveNotReady", "存档未就绪", "Save not ready");
		}
		if (s.LastOrderDay == Day)
		{
			return L.K("companies.services.orders.placeOrder.alreadyOrderedToday", "今天已经下过单了", "Already ordered today");
		}
		string text = OrderBlocked(c);
		if (text != null)
		{
			return text;
		}
		if (cart == null || unitPrice == null) return L.K("companies.services.orders.placeOrder.orderNotReadyHaveNot", "订单未就绪，尚未扣款。", "Order not ready. You have not been charged.");
		KeyValuePair<CatalogEntry, int>[] purchases = cart.ToArray();
		if (purchases.Any(kv => CargoWarehouse.Price(kv.Key?.Id) > 0) &&
			(c is not JacksonContact || !CargoWarehouse.Unlocked()))
			return CargoWarehouse.UnlockHint();
		if (purchases.Where(kv => CargoWarehouse.Limit(kv.Key?.Id) > 0).GroupBy(kv => kv.Key.Id).Any(group => group.Count() > 1))
			return L.K("companies.services.orders.placeOrder.invalidOrderQuantityOrAmount", "订单数量或金额无效，尚未扣款。", "Invalid order quantity or amount. You have not been charged.");
		if (c is SolanaCo && purchases.Any(kv => !Perfume.Catalog.Contains(kv.Key) || kv.Key.MinTier > c.CatalogTier(s)))
			return L.K("companies.services.orders.placeOrder.perfumerySuppliesRequireGoldMembership", "香水工艺供应仅向金卡及以上会员开放。", "Perfumery supplies require Gold membership or above.");
		if (purchases.Any((KeyValuePair<CatalogEntry, int> kv) => kv.Key == null || kv.Value <= 0 || kv.Key.BaseValue() < 0))
		{
			return L.K("companies.services.orders.placeOrder.someItemsAreUnavailableRight", "有货品暂不可用，请稍后再试。尚未扣款。", "Some items are unavailable right now. Try again later. You have not been charged.");
		}
		if (c is MeridianCo)
		{
			string permitError = purchases.Select(kv => Rosen.SaleBlocked(kv.Key.Id)).FirstOrDefault(reason => reason != null);
			if (permitError != null) return permitError;
		}
		CatalogEntry catalogEntry = purchases.FirstOrDefault(kv => kv.Value > Left(s, kv.Key, refreshControllerStock: true)).Key;
		if (catalogEntry != null)
		{
			return L.K("companies.services.orders.placeOrder.overLimitCanOrderMore", $"{catalogEntry.Name()}超出限购，还能订 {Left(s, catalogEntry)} 份", (System.FormattableString)$"{catalogEntry.Name()} is over the limit. You can order {Left(s, catalogEntry)} more");
		}
		int num = 0;
		int orderDay = Day;
		string period = PeriodKey;
		var limits = new (string Id, int WindowDays, int Count, string Key)[purchases.Length];
		int line = 0;
		Order order = new Order { Day = orderDay + 1 };
		foreach (KeyValuePair<CatalogEntry, int> item in purchases)
		{
			string id = item.Key.Id;
			int window = item.Key.WindowDays;
			int bundle = item.Key.Bundle;
			if (string.IsNullOrEmpty(id)) return L.K("companies.services.orders.placeOrder.itemIdentifierNotReadyHave", "货品标识未就绪，尚未扣款。", "Item identifier not ready. You have not been charged.");
			string key = id == ProductionWorkBay.OrderId
				? ProductionWorkBay.PurchaseKey
				: CargoWarehouse.Limit(id) > 0 ? id + "@lifetime" : QuotaCounts.Key(id, window, orderDay, period);
			if (id != CargoWarehouse.ControllerOrderId && !QuotaCounts.TryAdd(s.LimitedBought, key, item.Value, out _))
				return L.K("companies.services.orders.placeOrder.purchaseHistoryInvalidOrOut", "历史订购计数异常或超出范围，尚未扣款。", "Purchase history is invalid or out of range. You have not been charged.");
			int cargoPrice = CargoWarehouse.Price(id);
			if (!OrderAmounts.TryAdd(ref num, cargoPrice > 0 ? cargoPrice : unitPrice(item.Key), item.Value, bundle, out int count))
				return L.K("companies.services.orders.placeOrder.invalidOrderQuantityOrAmount", "订单数量或金额无效，尚未扣款。", "Invalid order quantity or amount. You have not been charged.");
			for (int i = 0; i < count; i++) order.Items.Add(id);
			limits[line++] = (id, window, item.Value, key);
		}
		if (num <= 0) return L.K("companies.services.orders.placeOrder.nothingSelectedYet", "还没有选货", "Nothing selected yet");
		// Finish allocating and validating the order before invoking payment.
		if (!session.IsCurrent || !ReferenceEquals(Companies.StateOf(c), s) || !session.IsCurrent || Day != orderDay)
			return L.K("companies.services.orders.placeOrder.orderNotReadyHaveNot", "订单未就绪，尚未扣款。", "Order not ready. You have not been charged.");
		string paymentError = Pay(c, num, "order");
		if (paymentError != null) return paymentError;
		s.Orders.Add(order);
		s.LastOrderDay = orderDay;
		foreach (var item2 in limits)
		{
			if (item2.Id == CargoWarehouse.ControllerOrderId) continue; // The queued order reserves the only controller.
			if (item2.Key == ProductionWorkBay.PurchaseKey) s.LimitedBought[item2.Key] = 1;
			else if (CargoWarehouse.Limit(item2.Id) > 0)
				s.LimitedBought[item2.Key] = s.LimitedBought.TryGetValue(item2.Key, out int used) ? used + item2.Count : item2.Count;
			else RecordLimited(s, item2.Id, item2.WindowDays, item2.Count, orderDay, period, item2.Key);
		}
		if (c is not JacksonContact && session.IsCurrent && ReferenceEquals(Companies.StateOf(c), s) && session.IsCurrent)
			AddCoop(c, (double)num / Settings.CoopCredits, "电话订货");
		try { ModLog.Info($"{c.Name}订单：{order.Items.Count} 件，{num}₵，第 {order.Day} 天送达"); } catch { }
		return null;
	}

	private static int PeriodIndex(string key, int fallback)
	{
		return Day / Settings.Period(key, fallback);
	}

	public static int LimitedUsed(CompanyState s, CatalogEntry e)
	{
		return e == null ? int.MaxValue : QuotaCounts.Used(s?.LimitedBought, e.Id, e.WindowDays, Day, e.WindowDays <= 0 ? PeriodKey : null);
	}

	public static int Left(CompanyState s, CatalogEntry e, bool refreshControllerStock = false)
	{
		if (s?.LimitedBought == null || e == null) return 0;
		if (e.Id == ProductionWorkBay.OrderId)
			return s.Orders == null || ProductionWorkBay.AlreadyPurchased(s) ? 0 : 1;
		if (e.Id == CargoWarehouse.ControllerOrderId)
			return CargoWarehouse.Unlocked() && CargoWarehouse.ControllerAvailable(s, refreshControllerStock) ? 1 : 0;
		int cargoLimit = CargoWarehouse.Limit(e.Id);
		if (cargoLimit > 0)
		{
			if (!CargoWarehouse.Unlocked()) return 0;
			s.LimitedBought.TryGetValue(e.Id + "@lifetime", out int used);
			return used < 0 || used >= cargoLimit ? 0 : cargoLimit - used;
		}
		int num = e.Limit();
		if (num <= 0)
		{
			return int.MaxValue;
		}
		return (int)Math.Max(0L, (long)num - LimitedUsed(s, e));
	}

	public static string LeftText(CompanyState s, CatalogEntry e)
	{
		int num = Left(s, e);
		if (e?.Id == ProductionWorkBay.OrderId)
			return num == 0
				? L.K("production.workBay.order.purchased", "本存档已购买", "Already purchased in this save")
				: L.K("production.workBay.order.once", "每个存档限购 1 件", "Limit 1 per save");
		if (CargoWarehouse.Limit(e?.Id) > 0)
			return L.F("production.cargoWarehouse.order.remaining", "本存档还可购买 {0} 件", "{0} remaining in this save", num);
		if (num != int.MaxValue)
		{
			if (e.WindowDays <= 0)
			{
				return L.K("companies.services.orders.leftText.periodQuotaRemaining", $"\u3000{PeriodWord}剩 {num}", (System.FormattableString)$"  {num} left {PeriodWord}");
			}
			return L.K("companies.services.orders.leftText.quotaRemaining", $"\u3000剩余 {num}", (System.FormattableString)$"  {num} left");
		}
		return "";
	}

	private static void RecordLimited(CompanyState s, string id, int windowDays, int count, int day, string period, string preparedKey = null)
	{
		bool rolling = windowDays > 0;
		string dailyPrefix = id + "@d";
		string prefix = rolling ? dailyPrefix : id + "@";
		string current = preparedKey ?? QuotaCounts.Key(id, windowDays, day, period);
		if (!QuotaCounts.TryAdd(s.LimitedBought, current, count, out int next))
		{
			try { ModLog.Warn("限购计数在付款期间发生异常，保留已登记订单及原记录：" + current); } catch { }
			return;
		}
		s.LimitedBought[current] = next;
		List<string> obsolete = null;
		foreach (string key in s.LimitedBought.Keys)
		{
			bool remove = rolling
				? key.StartsWith(prefix) && int.TryParse(key.Substring(prefix.Length), out int recordedDay) && recordedDay <= (long)day - windowDays
				: key.StartsWith(prefix) && key != current && !key.StartsWith(dailyPrefix);
			if (remove) (obsolete ??= new List<string>()).Add(key);
		}
		if (obsolete != null) foreach (string key in obsolete) s.LimitedBought.Remove(key);
	}

}

