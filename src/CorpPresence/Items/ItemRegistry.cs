using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class ItemRegistry
{
    internal static bool IsInstalled => _onInit != null;
    internal static bool LastReleaseFailed { get; private set; }
    internal static void AppendDiagnostics(System.Collections.Generic.ICollection<MaintenanceCounter> target)
    {
        target.Add(new MaintenanceCounter("items.factories", Factories.Count));
        target.Add(new MaintenanceCounter("items.registered", Registered.Count));
    }

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
		if (LastReleaseFailed)
		{
			Uninstall();
			if (LastReleaseFailed) throw new InvalidOperationException("物品工厂清理尚未完成，不能重新安装");
		}
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
		ReleaseFactories();
		_pendingDirectory = null;
		Reported.Clear();
		_reportedRegisteredCount = 0;
		_hasReportedSummary = false;
	}

	private static void ReleaseFactories()
	{
		LastReleaseFailed = false;
		foreach (string id in new List<string>(Registered))
		{
			try
			{
				if (_directory is not null && Factories.TryGetValue(id, out var factory)
					&& _directory.factoryDictionary.TryGetValue(id, out var current) && ReferenceEquals(factory, current))
				{
					_directory.Remove(id);
					if (_directory.factoryDictionary.TryGetValue(id, out current) && ReferenceEquals(factory, current))
						throw new InvalidOperationException("物品工厂移除未生效");
				}
				Registered.Remove(id);
			}
			catch (Exception ex)
			{
				LastReleaseFailed = true;
				Report("release", "remove_failed", ex);
				Warn("release:" + id, "清理独立物品失败，保留所有权供重试：" + id);
			}
		}
		if (Registered.Count == 0) _directory = null;
	}

	public static void Activate()
	{
		if (!GameResources.Ready) throw new InvalidOperationException("公司物品资源尚未就绪");
		ModItemDirectory pending = _pendingDirectory;
		_pendingDirectory = null;
		if (pending is not null) Register(pending);
	}

	private static void Register(ModItemDirectory directory)
	{
		if (directory is null)
		{
			return;
		}
		if (!GameResources.Ready) { _pendingDirectory = directory; return; }
		if (!ReferenceEquals(_directory, directory))
		{
			ReleaseFactories();
			if (LastReleaseFailed) return;
			_directory = directory;
			Registered.Clear();
		}
		foreach (ItemIdentity.Entry entry in ItemIdentity.All)
		{
			try
			{
				// Global availability can be shadowed while we still own the local entry.
				// Retain that ownership so replacement/disable can release it later.
				if (Registered.Contains(entry.Id) && Factories.TryGetValue(entry.Id, out var owned)
					&& directory.factoryDictionary.TryGetValue(entry.Id, out var current) && ReferenceEquals(owned, current))
				{
					if (!IsAvailable(entry.Id)) Report("register", "shadowed");
					continue;
				}
				Registered.Remove(entry.Id);
				if (directory.Has(entry.Id) || DirectoryMaster.Has<GameItem>(entry.Id))
				{
					Report("register", "collision");
					Warn("collision:" + entry.Id, "独立物品 ID 已被占用，保留现有工厂：" + entry.Id);
					continue;
				}
				if (!Factories.TryGetValue(entry.Id, out var value))
				{
					Func<GameItem> func = () => BuildRegistered(entry);
					Factories.Add(entry.Id, value = func);
				}
				if (directory.Add(entry.Id, value))
				{
					Registered.Add(entry.Id);
				}
				else
				{
					Report("register", "add_refused");
					Warn("add:" + entry.Id, "注册独立物品失败：" + entry.Id);
				}
			}
			catch (Exception ex)
			{
				Report("register", "add_failed", ex);
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
			// This shared creation entry also serves vanilla ingredients, templates and rewards.
			// Only our declared independent identities require registry ownership.
			if (ItemIdentity.Find(id, null) == null) return DirectoryMaster.Has<GameItem>(id);
			// DirectoryMaster retains the managed directory after its Unity scene object is destroyed.
			return IsInstalled && GameResources.Ready && !LastReleaseFailed
				&& Registered.Contains(id) && Factories.TryGetValue(id, out var owned)
				&& _directory is not null && _directory.factoryDictionary.TryGetValue(id, out var current)
				&& ReferenceEquals(owned, current) && ReferenceEquals(owned, DirectoryMaster.GetFactory<GameItem>(id));
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
			Report("create", "unavailable");
			if (ItemIdentity.Find(id, null) != null)
			{
				Warn("missing:" + id, "物品目录未就绪，暂不生成：" + id);
			}
			return null;
		}
		GameItem gameItem;
		try { gameItem = DirectoryMaster.Item(id, isOwned: false); }
		catch (FactoryCreationException) { return null; }
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
		Report("create", "wrong_identity");
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

	private sealed class FactoryCreationException : InvalidOperationException
	{
		internal FactoryCreationException(string id) : base("公司物品工厂创建失败：" + id) { }
	}

	private static GameItem BuildRegistered(ItemIdentity.Entry entry)
	{
		var item = entry.Goods == null ? Cards.Build(entry) : Goods.Build(entry);
		if (item != null && item.identifier == entry.Id) return item;
		if (item != null && item.parentInventory == null && !StorePlacement.Destroyed(item))
		{
			try { item.Destroy(); }
			catch (Exception error) { Report("create", "discard_failed", error); }
		}
		Report("create", item == null ? "factory_failed" : "wrong_identity");
		// Throw before DirectoryMaster can turn an empty result into a same-ID dummy.
		throw new FactoryCreationException(entry.Id);
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

	private static void Report(string stage, string code, Exception error = null)
	{
		try { MaintenanceDiagnostics.Record("items", stage, code, error); } catch { }
	}

	private static void Warn(string key, string message)
	{
		if (Reported.Add(key))
		{
			try { ModLog.Warn("[CorpPresence] " + message); } catch { }
		}
	}
}
