using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class ItemRegistry
{
	private static readonly Dictionary<string, Func<GameItem>> Factories = new Dictionary<string, Func<GameItem>>(StringComparer.Ordinal);

	private static readonly HashSet<string> Registered = new HashSet<string>(StringComparer.Ordinal);

	private static readonly HashSet<string> Reported = new HashSet<string>(StringComparer.Ordinal);

	private static ModItemDirectory _directory;
	private static ModItemDirectory _pendingDirectory;

	private static Action<ModItemDirectory> _onInit;

	private static int _reportedRegisteredCount;

	private static bool _hasReportedSummary;

	public static void Install()
	{
		if (_onInit == null)
		{
			_onInit = Register;
			ModHook.OnModItemDirectoryInit += _onInit;
		}
	}

	public static void Uninstall()
	{
		if (_onInit != null)
		{
			ModHook.OnModItemDirectoryInit -= _onInit;
			_onInit = null;
		}
		if (_directory != null)
		{
			foreach (string id in Registered)
				if (Factories.TryGetValue(id, out var factory) && _directory.factoryDictionary.TryGetValue(id, out var current) && ReferenceEquals(factory, current))
					_directory.Remove(id);
		}
		Registered.Clear();
		_directory = null;
		_pendingDirectory = null;
		Reported.Clear();
		_reportedRegisteredCount = 0;
		_hasReportedSummary = false;
	}

	public static void Activate()
	{
		if (!GameResources.Ready) throw new InvalidOperationException("公司物品资源尚未就绪");
		ModItemDirectory pending = _pendingDirectory;
		_pendingDirectory = null;
		if (pending != null) Register(pending);
	}

	public static void AfterInitDirectory(ModItemDirectory __instance)
	{
		Register(__instance);
	}

	private static void Register(ModItemDirectory directory)
	{
		if (directory == null)
		{
			return;
		}
		if (!GameResources.Ready) { _pendingDirectory = directory; return; }
		if ((object)_directory != directory)
		{
			_directory = directory;
			Registered.Clear();
		}
		foreach (ItemIdentity.Entry entry in ItemIdentity.All)
		{
			try
			{
				if (Registered.Contains(entry.Id) && directory.Has(entry.Id))
				{
					continue;
				}
				Registered.Remove(entry.Id);
				if (directory.Has(entry.Id) || DirectoryMaster.Has<GameItem>(entry.Id))
				{
					Warn("collision:" + entry.Id, "独立物品 ID 已被占用，保留现有工厂：" + entry.Id);
					continue;
				}
				if (!Factories.TryGetValue(entry.Id, out var value))
				{
					Func<GameItem> func = () => (entry.Goods == null) ? Cards.Build(entry) : Goods.Build(entry);
					Factories.Add(entry.Id, value = func);
				}
				if (directory.Add(entry.Id, value))
				{
					Registered.Add(entry.Id);
				}
				else
				{
					Warn("add:" + entry.Id, "注册独立物品失败：" + entry.Id);
				}
			}
			catch (Exception ex)
			{
				Warn("register:" + entry.Id, "注册 " + entry.Id + " 失败：" + ex.Message);
			}
		}
		if (!_hasReportedSummary || _reportedRegisteredCount != Registered.Count)
		{
			ModLog.Info($"公司独立物品目录：{Registered.Count}/{ItemIdentity.All.Count} 已注册");
			_reportedRegisteredCount = Registered.Count;
			_hasReportedSummary = true;
		}
	}

	public static bool IsAvailable(string id)
	{
		if (id == null)
		{
			return false;
		}
		try
		{
			return DirectoryMaster.Has<GameItem>(id);
		}
		catch (Exception ex)
		{
			Warn("available:" + id, "检查 " + id + " 失败：" + ex.Message);
			return false;
		}
	}

	public static GameItem Create(string id)
	{
		if (id == null)
		{
			return null;
		}
		if (!IsAvailable(id))
		{
			if (ItemIdentity.Find(id, null) != null)
			{
				Warn("missing:" + id, "物品目录未就绪，暂不生成：" + id);
			}
			return null;
		}
		GameItem gameItem = DirectoryMaster.Item(id, isOwned: false);
		if (gameItem != null && gameItem.identifier == id)
		{
			return gameItem;
		}
		if (gameItem != null && gameItem.parentInventory == null && !StorePlacement.Destroyed(gameItem))
		{
			try { gameItem.Destroy(); }
			catch (Exception ex) { Warn("discard:" + id, "回收错误身份的临时物品失败：" + id + "；" + ex.Message); }
		}
		Warn("create:" + id, "物品工厂返回了不匹配的身份：" + id);
		return null;
	}

	public static bool Migrate(GameItem item)
	{
		if (item == null)
		{
			return false;
		}
		try
		{
			string text = MigrationTarget(item.identifier, item.spritePath);
			if (text == null)
			{
				return false;
			}
			item.identifier = text;
			return true;
		}
		catch (Exception ex)
		{
			Warn("migrate-item", "迁移旧公司物品失败：" + ex.Message);
			return false;
		}
	}

	private static string MigrationTarget(string id, string sprite)
	{
		string text = ItemIdentity.ResolveId(id, sprite);
		if (text == null || !(text != id) || !IsAvailable(text))
		{
			return null;
		}
		return text;
	}

	public static void BeforeDecode(List<SaveItemNode> __0)
	{
		if (__0 == null)
		{
			return;
		}
		foreach (SaveItemNode item in __0)
		{
			if (item == null)
			{
				continue;
			}
			try
			{
				string text = MigrationTarget(item.identifier, item.spritePath);
				if (text != null)
				{
					item.identifier = text;
				}
			}
			catch (Exception ex)
			{
				Warn("migrate-node", "迁移旧公司存档节点失败：" + ex.Message);
			}
		}
	}

	public static void BeforeEncode(GameItem __0)
	{
		Migrate(__0);
	}

	private static void Warn(string key, string message)
	{
		if (Reported.Add(key))
		{
			ModLog.Warn("[CorpPresence] " + message);
		}
	}
}
