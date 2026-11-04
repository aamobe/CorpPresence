using System;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CorpPresence;

internal static class GameResources
{
    private static AsyncOperationHandle<StringTable> _mechanic;
    private static string _requestedLocale;
    private static long _generation;
    public static bool Ready { get; private set; }
    public static long Generation => _generation;

    public static void Begin() { unchecked { _generation++; } Reset(); }
    public static void Reset() { Ready = false; _requestedLocale = null; _mechanic = default; }

    public static bool Poll()
    {
        if (Ready) return true;
        var initialization = LocalizationSettings.InitializationOperation;
        if (!initialization.IsValid() || !initialization.IsDone) return false;
        if (initialization.Status != AsyncOperationStatus.Succeeded)
            throw new InvalidOperationException("本地化系统初始化失败", initialization.OperationException);
        var locale = LocalizationSettings.SelectedLocale;
        if (locale == null) return false;
        // The main menu can save and apply a new language while this table is loading.
        string startupLocale = ResolveMenuStartupLocale();
        if (locale.Identifier.Code != startupLocale) return false;

        if (_requestedLocale != startupLocale)
        {
            // StringDatabase owns its cached handle. Never release a borrowed handle.
            _mechanic = LocalizationSettings.StringDatabase.GetTableAsync("Mechanic", locale);
            _requestedLocale = startupLocale;
        }
        if (!_mechanic.IsValid()) throw new InvalidOperationException("Mechanic 表句柄失效，停止公司初始化");
        if (!_mechanic.IsDone) return false;
        if (_mechanic.Status != AsyncOperationStatus.Succeeded || _mechanic.Result == null)
            throw new InvalidOperationException("Mechanic 表加载失败", _mechanic.OperationException);
        if (_mechanic.Result.LocaleIdentifier.Code != startupLocale)
            throw new InvalidOperationException("Mechanic 表语言与游戏启动语言不符");
        if (_mechanic.Result.GetEntry("mech_condition_pure_water") == null)
            throw new InvalidOperationException("Mechanic 表缺少 mech_condition_pure_water");
        Ready = true;
        return true;
    }

    private static string ResolveMenuStartupLocale()
    {
        // Match the vanilla startup dropdown, which falls back to English for unknown codes.
        return LocHelper.ResolveStartupLocale() switch
        {
            "zh" => "zh", "ru" => "ru", "de" => "de", "ja" => "ja",
            "fr" => "fr", "es" => "es", "pl" => "pl",
            _ => "en",
        };
    }
}
