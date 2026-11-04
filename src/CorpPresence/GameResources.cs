using System;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CorpPresence;

internal static class GameResources
{
	private static AsyncOperationHandle<StringTable> _mechanic;

	private static bool _requested;

	private static long _generation;

	public static bool Ready { get; private set; }

	public static long Generation => _generation;

	public static void Begin()
	{
		_generation++;
		Reset();
	}

	public static void Reset()
	{
		Ready = false;
		_requested = false;
		_mechanic = default;
	}

	public static bool Poll()
	{
		if (Ready)
		{
			return true;
		}
		AsyncOperationHandle<LocalizationSettings> initializationOperation = LocalizationSettings.InitializationOperation;
		if (!initializationOperation.IsValid() || !initializationOperation.IsDone)
		{
			return false;
		}
		if (initializationOperation.Status != AsyncOperationStatus.Succeeded)
		{
			throw new InvalidOperationException("本地化系统初始化失败", initializationOperation.OperationException);
		}
		if (!_requested)
		{
			_mechanic = LocalizationSettings.StringDatabase.GetTableAsync("Mechanic");
			_requested = true;
		}
		if (!_mechanic.IsValid())
		{
			throw new InvalidOperationException("Mechanic 表句柄失效，停止公司初始化");
		}
		if (!_mechanic.IsDone)
		{
			return false;
		}
		if (_mechanic.Status != AsyncOperationStatus.Succeeded || _mechanic.Result == null)
		{
			throw new InvalidOperationException("Mechanic 表加载失败", _mechanic.OperationException);
		}
		if (_mechanic.Result.GetEntry("mech_condition_pure_water") == null)
		{
			throw new InvalidOperationException("Mechanic 表缺少 mech_condition_pure_water");
		}
		Ready = true;
		return true;
	}
}
