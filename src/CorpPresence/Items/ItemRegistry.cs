using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class ItemRegistry
{
    internal static bool IsInstalled => _onInit != null;
    internal static bool LastReleaseFailed { get; private set; }
    internal static event Action<string, string, Exception> CompanyDecodeWarning;
    internal static Func<string, string, Exception, bool> AllowLoadPlaceholder { get; set; }
    internal static void AppendDiagnostics(System.Collections.Generic.ICollection<MaintenanceCounter> target)
    {
        target.Add(new MaintenanceCounter("items.factories", Factories.Count));
        target.Add(new MaintenanceCounter("items.registered", Registered.Count));
        int resolvable = 0;
        foreach (string id in Registered)
            if (ResolveFailure(id, companyItem: true, out _) == null) resolvable++;
        target.Add(new MaintenanceCounter("items.resolvable", resolvable));
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
				ReportRuntime("release", "remove_failed", ex, id);
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
					string failure = ResolveFailure(entry.Id, companyItem: true, out var error);
					if (failure != null) ReportRuntime("resolve", failure, error, entry.Id);
					continue;
				}
				Registered.Remove(entry.Id);
				if (directory.Has(entry.Id) || DirectoryMaster.Has<GameItem>(entry.Id))
				{
					ReportRuntime("register", "collision", null, entry.Id);
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
					ReportRuntime("register", "add_refused", null, entry.Id);
					Warn("add:" + entry.Id, "注册独立物品失败：" + entry.Id);
				}
			}
			catch (Exception ex)
			{
				ReportRuntime("register", "add_failed", ex, entry.Id);
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

	public static bool IsAvailable(string id) => id != null
		&& ResolveFailure(id, ItemIdentity.Find(id, null) != null, out _) == null;

	private static string ResolveFailure(string id, bool companyItem, out Exception error)
	{
		error = null;
		try
		{
			if (!companyItem) return DirectoryMaster.Has<GameItem>(id) ? null : "global_entry_missing";
			if (!IsInstalled) return "registry_disabled";
			if (!GameResources.Ready) return "resources_not_ready";
			if (LastReleaseFailed) return "cleanup_incomplete";
			// The native global directory can retain this managed dictionary across scene changes.
			if (_directory is null) return "directory_not_initialized";
			if (!Registered.Contains(id) || !Factories.TryGetValue(id, out var owned)) return "not_registered";
			if (!_directory.factoryDictionary.TryGetValue(id, out var current)) return "local_entry_missing";
			if (!ReferenceEquals(owned, current)) return "local_factory_replaced";
			var global = DirectoryMaster.GetFactory<GameItem>(id);
			if (global == null) return "global_entry_missing";
			return ReferenceEquals(owned, global) ? null : "global_factory_replaced";
		}
		catch (Exception ex)
		{
			error = ex;
			return "lookup_exception";
		}
	}

	public static GameItem Create(string id)
	{
		if (id == null)
		{
			return null;
		}
		bool companyItem = ItemIdentity.Find(id, null) != null;
		string failure = ResolveFailure(id, companyItem, out var error);
		if (failure != null)
		{
			Report("resolve", failure, error, id);
			if (companyItem)
			{
				Warn("missing:" + id, "物品目录未就绪，暂不生成：" + id);
			}
			return null;
		}
		GameItem gameItem;
		try { gameItem = DirectoryMaster.Item(id, isOwned: false); }
		catch (FactoryCreationException) { return null; }
		catch (Exception ex) when (!companyItem)
		{
			Report("create", "native_creation_exception", ex, id);
			throw;
		}
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
		Report("create", gameItem == null ? "native_returned_null" : "wrong_identity", null, id);
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
		GameItem item;
		try { item = entry.Goods == null ? Cards.Build(entry) : Goods.Build(entry); }
		catch (Exception ex)
		{
			Report("create", "factory_exception", ex, entry.Id);
			if (TryUseLoadPlaceholder(entry.Id, "factory_exception", ex)) return null;
			throw;
		}
		if (item != null && item.identifier == entry.Id) return item;
		if (item != null && item.parentInventory == null && !StorePlacement.Destroyed(item))
		{
			try { item.Destroy(); }
			catch (Exception error) { Report("create", "discard_failed", error, entry.Id); }
		}
		string reason = item == null ? "factory_returned_null" : "wrong_identity";
		Report("create", reason, null, entry.Id);
		if (TryUseLoadPlaceholder(entry.Id, reason, null)) return null;
		// Only saved-item decoding can accept the native placeholder.
		throw new FactoryCreationException(entry.Id);
	}

	private static bool TryUseLoadPlaceholder(string id, string reason, Exception error)
	{
		try { return AllowLoadPlaceholder?.Invoke(id, reason, error) == true; }
		catch { return false; }
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
		// Report unavailable company factories and leave fallback handling to the native decoder.
		foreach (var node in __0)
		{
			string id = ItemIdentity.Find(node?.identifier, node?.spritePath)?.Id;
			if (id == null) continue;
			string reason = ResolveFailure(id, companyItem: true, out var error);
			if (reason == null) continue;
			Report("decode", reason, error, id);
			Warn("decode:" + id + ":" + reason, "读档时公司物品工厂不可用：" + id + " (" + reason + ")");
			try { CompanyDecodeWarning?.Invoke(id, reason, error); } catch { }
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

	private static void Report(string stage, string code, Exception error, string id)
	{
		try { MaintenanceDiagnostics.Record("items", stage, code, error, id); } catch { }
	}

	private static void ReportRuntime(string stage, string code, Exception error, string id)
	{
		try { MaintenanceDiagnostics.RecordRuntime("items", stage, code, error, id); } catch { }
	}

	private static void Warn(string key, string message)
	{
		if (Reported.Add(key))
		{
			try { ModLog.Warn("[CorpPresence] " + message); } catch { }
		}
	}
}
