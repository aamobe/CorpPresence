using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static partial class ItemRegistry
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
			foreach (string item in Registered)
			{
				if (Factories.TryGetValue(item, out var value) && _directory.factoryDictionary.TryGetValue(item, out var value2) && (object)value == value2)
				{
					_directory.Remove(item);
				}
			}
		}
		Registered.Clear();
		_directory = null;
		_pendingDirectory = null;
		Reported.Clear();
		_hasReportedSummary = false;
	}

	public static void Activate()
	{
		if (!GameResources.Ready)
		{
			throw new InvalidOperationException("公司物品资源尚未就绪");
		}
		ModItemDirectory pendingDirectory = _pendingDirectory;
		_pendingDirectory = null;
		if (pendingDirectory != null)
		{
			Register(pendingDirectory);
		}
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
		if (!GameResources.Ready)
		{
			_pendingDirectory = directory;
			return;
		}
		if ((object)_directory != directory)
		{
			_directory = directory;
			Registered.Clear();
			_hasReportedSummary = false;
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
					Func<GameItem> func = () => (entry.Goods != null) ? Goods.Build(entry) : Cards.Build(entry);
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

	private static void Warn(string key, string message)
	{
		if (Reported.Add(key))
		{
			ModLog.Warn("[CorpPresence] " + message);
		}
	}
}


