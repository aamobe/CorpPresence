using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class CompanyVisitors
{
	// Null means no appointment was needed. False leaves every date intact for a retry.
	internal static bool? BookDelivery(List<Order> orders, int day, bool card, Func<bool> queued,
		Func<List<Order>, bool> insert, out List<Order> due)
	{
		due = orders.Where(o => o != null && o.Day <= day && (!IsCommission(o) || o.CourierBookedDay != day)).ToList();
		if (due.Count == 0 && !card || queued()) return null;
		if (!insert(due)) return false;
		foreach (Order order in due)
		{
			if (IsCommission(order)) order.CourierBookedDay = day;
			else order.Day = day + 1;
		}
		return true;
	}

	private static Action CreateCourierLeave(StoreClient client)
	{
		StoreSession session = StoreSession.Capture();
		bool done = false;
		return () =>
		{
			if (done || !session.IsCurrent || !ReferenceEquals(SaveSlot.CurrentStore.currentClientInstance?.GetClientBlueprint(), client)) return;
			try
			{
				DialogUIManager ui = UnityEngine.Object.FindObjectOfType<DialogUIManager>();
				if (ui == null) return;
				done = true;
				ui.CurrentClientLeave();
			}
			catch { }
		};
	}

	private static Action CreateDeliveryReceipt(Company c, CompanyState s, List<Order> orders, bool card)
	{
		PlayerStore store = SaveSlot.CurrentStore;
		CorpState root = CorpState.Current;
		int bookingDay = Day;
		StoreSession session = StoreSession.Capture(store, root);
		bool done = false;
		bool IsCurrent()
		{
			return !StoreSession.CompanyProgressPaused && session.IsCurrent && c != null && s?.Orders != null && orders != null
				&& root.Companies != null && root.Companies.TryGetValue(c.Id, out CompanyState current)
				&& ReferenceEquals(current, s) && SaveSlot.CanWrite("CorpPresence");
		}
		return () =>
		{
			if (done) return;
			if (!session.IsCurrent)
			{
				MaintenanceDiagnostics.RecordRuntime("delivery", "session", "expired", null);
				return;
			}
			if (!IsCurrent()) return;
			bool workshop = c is WorkshopContact || orders.Any(o => IsCommission(o) || IsContactSupply(c, o));
			if (workshop && !CompanyTransaction.TryEnter(store)) return;
			done = true;
			try
			{
				var due = orders.Where(order => order != null && s.Orders.Contains(order)).Distinct().ToList();
				foreach (var order in due.Where(IsCommission))
					MeridianFrameDelivery.Deliver(s, order, Day, () => bookingDay == Day && IsCurrent());
				var regular = due.Where(order => !IsCommission(order));
				foreach (var group in regular.GroupBy(o => c is WorkshopContact || IsContactSupply(c, o)))
				{
					if (!IsCurrent()) return;
					var ordersForCounter = group.ToList();
					if (group.Key && c.Id == "meridian")
						foreach (var order in ordersForCounter) order.Kind = Order.SpentContacts;
					Deliver(c, s, ordersForCounter, IsCurrent, group.Key);
				}
			}
			catch (Exception error)
			{
				try { ModLog.Warn("本次订单配送中断，保留未完成明细：" + error.Message); } catch { }
			}
			finally { if (workshop) CompanyTransaction.Exit(); }
			if (card && IsCurrent())
			{
				try { Services.MaybeGiveCard(c.CharacterId); }
				catch (Exception error)
				{
					try { ModLog.Warn("补送名片失败：" + error.Message); } catch { }
				}
			}
		};
	}

	public static bool CounterFull()
	{
		try
		{
			GameGridInventory gameGridInventory = EmporiumEntry.Instance?.frontInvinvElement;
			return gameGridInventory != null && !gameGridInventory.CanTakeAnything();
		}
		catch
		{
			return false;
		}
	}

	private const int MaxCratesPerDelivery = 3;

	private static bool IsCommission(Order order) => order?.Kind == MeridianCommissionReceipt.OrderKind;

	private static bool IsContactSupply(Company company, Order order) => company.Id == "meridian" && order != null
		&& (order.Kind == Order.SpentContacts || order.Items.Contains(Order.SpentContacts));

	private static void Deliver(Company company, CompanyState state, List<Order> orders, Func<bool> isCurrent, bool workshop)
	{
		bool Room() => workshop ? WorkshopCounter.HasRoom : !CounterFull();
		foreach (var order in orders) { if (!isCurrent()) return; order.Day = Day + 1; }
		if (Room())
		{
			foreach (var order in orders)
			{
				if (!isCurrent()) return;
				OrderContents.ExpandModBoxes(order, SpacerCo.RollModBox);
			}
		}

		var failed = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < MaxCratesPerDelivery && isCurrent() && Room(); i++)
		{
			if (!orders.Any(order => order.Items.Any(id => id != "@modbox" && !failed.Contains(id)))) break;
			var crate = Goods.Make(Crates.GoodsKey(company));
			if (crate == null)
			{
				ModLog.Warn($"{company.Name}配送箱未就绪：{Crates.GoodsKey(company)}，保留订单，明天重试。");
				break;
			}

			var packed = new List<(Order Order, string Id, GameItem Item)>();
			bool placed = false;
			try
			{
				// A free cell does not mean the actual crate will fit. Probe with the
				// real shell before creating the order items.
				if (!isCurrent()) break;
				if (!(workshop ? WorkshopCounter.CanFit(crate) : CounterMayFit(crate)))
				{
					MaintenanceDiagnostics.Record("delivery", "placement", "counter_no_space", null, crate.identifier);
					break;
				}
				if (!isCurrent()) break;
				foreach (var order in orders)
				{
					foreach (string id in order.Items)
					{
						if (!isCurrent()) return;
						if (id == "@modbox" || failed.Contains(id)) continue;
						GameItem item = null;
						try
						{
							item = MakeOrdered(company, id);
							if (!isCurrent()) return;
							if (item == null || item.parentInventory != null || StorePlacement.Destroyed(item))
							{
								failed.Add(id);
								ModLog.Warn($"{company.Name}配送无法生成可装箱的物品：{id}，保留订单。");
								continue;
							}
							if (Crates.Put(crate, item))
							{
								packed.Add((order, id, item));
								item = null; // The crate now owns it.
							}
							else if (packed.Count == 0) failed.Add(id);
						}
						catch (Exception error)
						{
							failed.Add(id);
							ModLog.Warn("送货失败，明天再送：" + id + "：" + error.Message);
						}
						finally { DiscardUnshipped(item); }
					}
				}
				if (packed.Count == 0 || !isCurrent()) break;
				if (!(workshop ? WorkshopCounter.TryPlaceOnCounter(crate) : TryPlaceDelivery(crate)))
				{
					MaintenanceDiagnostics.Record("delivery", "placement", "counter_rejected", null, crate.identifier);
					break;
				}
				placed = true;
				if (!isCurrent()) return;
				foreach (var entry in packed) entry.Order.Items.Remove(entry.Id);
			}
			finally
			{
				if (!placed)
				{
					foreach (var entry in packed) DiscardUnshipped(entry.Item);
					DiscardUnshipped(crate);
				}
			}
		}

		if (!isCurrent()) return;
		foreach (var order in orders)
		{
			if (!isCurrent()) return;
			if (order.Cash > 0)
			{
				try { CompanyMoney.DeliverCash(PlayerStore.Instance, order); }
				catch (Exception error) { ModLog.Warn("返点发放失败，明天再送：" + error.Message); }
			}
			if (!isCurrent()) return;
			if (order.Items.Count == 0 && order.Cash == 0) state.Orders.Remove(order);
		}

		int pending = orders.Sum(order => order.Items.Count);
		if (pending > 0)
		{
			ModLog.Info($"{company.Name}送货：还有 {pending} 件，明天再送");
			try
			{
				StoreUIManager.Instance?.Notify(L.K("companies.companyVisitors.deliveries.deliver.hasItemsLeftDeliverTomorrow", $"{company.Name}还有 {pending} 件货未送达，明天再送来。", (System.FormattableString)$"{company.Name} has {pending} items left to deliver tomorrow."));
			}
			catch { }
		}
	}

	internal static void DiscardUnshipped(GameItem item)
	{
		try { if (item != null && item.parentInventory == null && !StorePlacement.Destroyed(item)) item.Destroy(); }
		catch (Exception error) { ModLog.Warn("清理未交付物品失败：" + error.Message); }
	}

	private static bool CounterMayFit(GameItem crate)
	{
		try
		{
			var counter = EmporiumEntry.Instance?.frontInvinvElement;
			if (counter == null) return true;
			GeneralHelper.SetItemOwned(crate);
			return counter.TryFindOneValidInventorySlot(crate) != null;
		}
		catch
		{
			// If a custom inventory throws during the probe, keep the normal placement path.
			return true;
		}
	}

	private static bool TryPlaceDelivery(GameItem crate)
	{
		if (CounterFull()) return false;
		try
		{
			return PlayerStore.Instance.TryAddDirectSellingItemToTable(crate, isOwend: true) || crate.parentInventory != null;
		}
		catch
		{
			// Native placement may succeed before a UI callback throws.
			return crate.parentInventory != null;
		}
	}
	private static GameItem MakeOrdered(Company c, string id)
	{
		if (id.Contains('|'))
		{
			return MeridianCo.MakeRefurbished(id);
		}
		CatalogEntry catalogEntry = c.Catalog?.FirstOrDefault((CatalogEntry e) => e.Id == id);
		if (catalogEntry != null)
		{
			return catalogEntry.Make();
		}
		// Chapter gift bundles and genuine goods belong to the character factory.
		// Reuse it for deferred delivery instead of treating bundle keys as item IDs.
		if (id.StartsWith("@", StringComparison.Ordinal))
		{
			Character character = Engine.Find(c.CharacterId);
			if (character != null) return character.MakeItem(id);
		}
		if (c is CarmichaelCo)
		{
			return CarmichaelCo.MakeGenuine(id);
		}
		return Goods.MakeSpecial(id) ?? ItemRegistry.Create(id);
	}

}
