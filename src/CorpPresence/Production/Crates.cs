using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CorpPresence.Malice;

namespace CorpPresence;

internal static class Crates
{
	public const int InnerW = 12;

	public const int InnerH = 10;

	private static readonly List<(GameItem Item, StoreSession Session)> PendingDestroy = new List<(GameItem, StoreSession)>();
	private static bool _cleaning;
	private static long _cleanupGeneration;

	private static readonly ConditionalWeakTable<GameItem, GameInventory> Inner = new ConditionalWeakTable<GameItem, GameInventory>();
	private static readonly ConditionalWeakTable<GameItem, CleanupBinding> CleanupBindings = new ConditionalWeakTable<GameItem, CleanupBinding>();

	private sealed class CleanupBinding
	{
		public StoreSession Session;
		public long Generation;
		public GameInventory Inventory;
		public Action<GameItem, GameInventory, SlotMarker> Callback;
	}

	public static string GoodsKey(Company c)
	{
		if (!(c is RebelCo))
		{
			return "crate_" + c.Id;
		}
		return "crate_spacer";
	}

	public static GameItem CreateShell()
	{
		(PixelWindow, GameInventory) tuple = DirectoryUtils.CreateInventoryWindow(12, 10);
		PixelWindow item = tuple.Item1;
		GameInventory item2 = tuple.Item2;
		GameItem item3 = ItemDirectory.CreateEmptyItem().SetContentWindow(item);
		item3.EnableTag("IMPORTANT_TAG");
		item3.EnableTag("CONTAINER_TAG");
		Inner.AddOrUpdate(item3, item2);
		item2.mayInventoryAddItemFunc = (GameItem _, GameInventory _) => CrateFillGate.Allows(item2);
		BindCleanup(item3, item2, StoreSession.Capture());
		return item3;
	}

	private static void BindCleanup(GameItem crate, GameInventory inventory, StoreSession session)
	{
		if (!CleanupBindings.TryGetValue(crate, out var binding))
		{
			binding = new CleanupBinding();
			CleanupBindings.Add(crate, binding);
		}
		if (!ReferenceEquals(binding.Inventory, inventory))
		{
			if (binding.Inventory != null && binding.Callback != null)
				binding.Inventory.onSlotRemoveItemEndFunc -= binding.Callback;
			binding.Inventory = inventory;
			binding.Callback = (GameItem _, GameInventory box, SlotMarker _) =>
			{
				try
				{
					if (binding.Generation == _cleanupGeneration && binding.Session.IsCurrent
						&& ReferenceEquals(box, binding.Inventory) && box.childItems.Count == 0
						&& !PendingDestroy.Exists(entry => ReferenceEquals(entry.Item, crate)))
						PendingDestroy.Add((crate, binding.Session));
				}
				catch { }
			};
			inventory.onSlotRemoveItemEndFunc += binding.Callback;
		}
		binding.Session = session;
		binding.Generation = _cleanupGeneration;
	}

	// Once after native load: only items present in this store's owned inventory
	// graphs may acquire the fresh session. Includes the back counter and afterhours.
	public static void RebindLoaded()
	{
		StoreSession session = StoreSession.Capture();
		var emporium = EmporiumEntry.Instance;
		if (!session.IsCurrent || emporium == null) return;
		var items = emporium.GetAllOwnedItems();
		if (items == null) return;
		foreach (GameItem crate in items)
		{
			if (!session.IsCurrent || !ReferenceEquals(emporium, EmporiumEntry.Instance)) return;
			if (crate == null || crate.parentInventory == null || StorePlacement.Destroyed(crate)) continue;
			string key = Goods.IdOf(crate);
			if (key == null || !key.StartsWith("crate_", StringComparison.Ordinal)) continue;
			GameInventory inventory = InnerOf(crate);
			if (inventory == null || !session.IsCurrent) continue;
			Inner.AddOrUpdate(crate, inventory);
			BindCleanup(crate, inventory, session);
		}
	}

	public static GameInventory InnerOf(GameItem crate)
	{
		if (crate == null)
		{
			return null;
		}
		if (Inner.TryGetValue(crate, out var value))
		{
			return value;
		}
		try
		{
			using List<GameInventory>.Enumerator enumerator = GraphUtils.FindChildrenType<GameInventory>(Compat.Cast<GraphNodeStorage>(crate)).GetEnumerator();
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		catch
		{
		}
		return null;
	}

	private static int Count(GameItem crate)
	{
		try
		{
			return GraphUtils.FindAllChildrenType<GameItem>(Compat.Cast<GraphNodeStorage>(crate)).Count;
		}
		catch
		{
			return InnerOf(crate)?.childItems.Count ?? 0;
		}
	}

	private static bool InCrate(GameItem crate, GameItem item)
	{
		try
		{
			return GraphUtils.FindAllChildrenType<GameItem>(Compat.Cast<GraphNodeStorage>(crate)).Contains(item);
		}
		catch
		{
			return false;
		}
	}

	private static bool Absorbed(GameItem item)
	{
		try
		{
			return item is GameItemElement gameItemElement && gameItemElement.IsDestroyed();
		}
		catch
		{
			return false;
		}
	}

	public static bool Put(GameItem crate, GameItem item)
	{
		GameInventory gameInventory = InnerOf(crate);
		if (gameInventory == null)
		{
			ModLog.Warn("送货箱找不到箱内格子");
			return false;
		}
		if (item == null)
		{
			return false;
		}
		try
		{
			GeneralHelper.SetItemOwned(crate);
			GeneralHelper.SetItemOwned(item);
		}
		catch
		{
		}
		int num = Count(crate);
		object previousFill = CrateFillGate.Enter(gameInventory);
		try
		{
			GraphUtils.TryAcceptAll(gameInventory, item);
		}
		catch (Exception ex)
		{
			ModLog.Warn("送货箱装货失败：" + ex.Message);
		}
		finally
		{
			CrateFillGate.Restore(previousFill);
		}
		// The normal path does not need another two recursive inventory scans.
		if (item.parentInventory == gameInventory || Absorbed(item)) return true;
		int num2;
		if (Count(crate) <= num && !InCrate(crate, item))
		{
			num2 = (Absorbed(item) ? 1 : 0);
			if (num2 == 0)
			{
				ModLog.Warn($"送货箱装不进：{item.identifier}（箱里已有 {num} 件）");
			}
		}
		else
		{
			num2 = 1;
		}
		return (byte)num2 != 0;
	}

	public static GameItem MakeFilled(string crateKey, params string[] goods)
	{
		GameItem gameItem = Goods.Make(crateKey);
		if (gameItem == null)
		{
			return null;
		}
		for (int i = 0; i < goods.Length; i++)
		{
			GameItem gameItem2 = Goods.Make(goods[i]);
			if (gameItem2 != null && !Put(gameItem, gameItem2))
			{
				try
				{
					gameItem2.Destroy();
				}
				catch
				{
				}
			}
		}
		return gameItem;
	}

	public static void Reset()
	{
		PendingDestroy.Clear();
		unchecked { _cleanupGeneration++; }
	}

	public static void Tick()
	{
		if (_cleaning || PendingDestroy.Count == 0)
		{
			return;
		}
		try
		{
			if (ItemMouseDragHandler.current != null && ItemMouseDragHandler.current.IsDraggingItem)
			{
				return;
			}
		}
		catch
		{
			return;
		}
		long generation = _cleanupGeneration;
		_cleaning = true;
		try
		{
			int remaining = PendingDestroy.Count;
			while (remaining-- > 0 && generation == _cleanupGeneration && PendingDestroy.Count > 0)
			{
				var entry = PendingDestroy[0];
				PendingDestroy.RemoveAt(0);
				try
				{
					if (!entry.Session.IsCurrent || entry.Item == null || Absorbed(entry.Item)) continue;
					GameInventory inventory = InnerOf(entry.Item);
					if (inventory != null && inventory.childItems.Count == 0 && generation == _cleanupGeneration && entry.Session.IsCurrent)
						entry.Item.Destroy();
				}
				catch (Exception error)
				{
					try { ModLog.Warn("配送空箱清理失败，保留物品：" + error.Message); } catch { }
				}
			}
		}
		finally { _cleaning = false; }
	}
}
