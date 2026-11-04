using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CorpPresence.Malice;

namespace CorpPresence;

internal static class Crates
{
	public const int InnerW = 12;

	public const int InnerH = 10;

	private static readonly List<GameItem> PendingDestroy = new List<GameItem>();

	private static readonly ConditionalWeakTable<GameItem, GameInventory> Inner = new ConditionalWeakTable<GameItem, GameInventory>();

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
		item2.onSlotRemoveItemEndFunc = (Action<GameItem, GameInventory, SlotMarker>)Delegate.Combine(item2.onSlotRemoveItemEndFunc, (Action<GameItem, GameInventory, SlotMarker>)((GameItem _, GameInventory box, SlotMarker _) =>
		{
			try
			{
				if (box.childItems.Count == 0 && !PendingDestroy.Contains(item3))
				{
					PendingDestroy.Add(item3);
				}
			}
			catch
			{
			}
		}));
		return item3;
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
		object previous = CrateFillGate.Enter(gameInventory);
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
			CrateFillGate.Restore(previous);
		}
		if (item.parentInventory == gameInventory || Absorbed(item))
		{
			return true;
		}
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

	private static bool _cleaning;
	private static int _cleanupGeneration;

	public static void Reset()
	{
		PendingDestroy.Clear();
		unchecked { _cleanupGeneration++; }
	}

	public static void Tick()
	{
		if (_cleaning || PendingDestroy.Count == 0) return;
		// If drag state cannot be read, defer instead of destroying a potentially dragged box.
		try
		{
			if (ItemMouseDragHandler.current != null && ItemMouseDragHandler.current.IsDraggingItem) return;
		}
		catch { return; }
		int generation = _cleanupGeneration;
		_cleaning = true;
		try
		{
			// Remove before invoking callbacks. Appended work remains for the next tick.
			for (int i = PendingDestroy.Count - 1; i >= 0 && generation == _cleanupGeneration; i--)
			{
				GameItem crate = PendingDestroy[i];
				PendingDestroy.RemoveAt(i);
				try
				{
					if (crate == null || Absorbed(crate)) continue;
					GameInventory inventory = InnerOf(crate);
					// Missing inventory is unknown, not empty. Never delete contents on lookup failure.
					if (inventory != null && inventory.childItems.Count == 0) crate.Destroy();
				}
				catch (Exception ex)
				{
					try { ModLog.Warn("配送空箱清理失败，保留物品：" + ex.Message); } catch { }
				}
			}
		}
		finally { _cleaning = false; }
	}
}
