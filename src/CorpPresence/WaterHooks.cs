using System;
using HarmonyLib;

namespace CorpPresence;

// The bridge target has no dependency on WaterFeatureHelper's static initializer.
// Its postfix runs after vanilla InitWaterFeature has established native conditions.
internal static class WaterHooks
{
    private static Harmony _owner;
    private static bool _installed;
    private static bool _installing;
    private static Action<Exception> _abort;

    public static void Install(string parentId, Action<Exception> abort)
    {
        Uninstall();
        _abort = abort ?? throw new ArgumentNullException(nameof(abort));
        _owner = new Harmony(parentId + ".native-water-ready");
        try
        {
            var method = AccessTools.Method(typeof(WaterHelper), "InitLiquidContainerItem",
                new[] { typeof(GameItem), typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(int) });
            if (method == null) throw new MissingMethodException("WaterHelper.InitLiquidContainerItem");
            _owner.Patch(method, postfix: new HarmonyMethod(typeof(WaterHooks), nameof(AfterNativeContainer)));
        }
        catch { Uninstall(); throw; }
    }

    public static void Uninstall()
    {
        var owner = _owner;
        _installed = false;
        _installing = false;
        _abort = null;
        WaterNames.Reset();
        // Retain the owner if Harmony cleanup fails, so the main runtime can retry.
        owner?.UnpatchAll(owner.Id);
        if (ReferenceEquals(_owner, owner)) _owner = null;
    }

    public static void AfterNativeContainer(GameItem item, bool __runOriginal)
    {
        if (!__runOriginal || _owner == null || _installed || _installing) return;
        // No changing global names, existing features, purity, or buyer requirements.
        _installing = true;
        try
        {
            WaterCompatibility.Install(_owner);
            Patch("UpdateWaterFeature", nameof(SilverBlood.AfterUpdateWaterFeature), typeof(GameItem), typeof(bool));
            Patch("OnLiquidChanged", nameof(SilverBlood.AfterLiquidChanged), typeof(GameItem), typeof(bool));
            Patch("OnLiquidContainerPourInto", nameof(SilverBlood.AfterPourInto), typeof(GameItem), typeof(GameItem), typeof(bool));
            _installed = true;
            ModLog.Info("[水质初始化验证] 原版容器初始化完成后安装公司液体补丁和固定水质键兼容层");
        }
        catch (Exception error)
        {
            // Never turn optional compatibility installation into a failed native
            // bottle constructor during save loading. Report the full failure and
            // roll back the company runtime through its owner.
            var abort = _abort;
            Exception failure = error;
            try { Uninstall(); }
            catch (Exception cleanup) { failure = new AggregateException("水质安装和补丁撤销均失败", error, cleanup); }
            try { abort?.Invoke(failure); }
            catch (Exception reporting)
            {
                // A logging subscriber must not turn this into a load failure.
                try { UnityEngine.Debug.LogException(new AggregateException("水质回滚或上报失败", failure, reporting)); }
                catch { /* No remaining reporting sink; preserve the native return. */ }
            }
        }
        finally { _installing = false; }
    }

    private static void Patch(string method, string callback, params Type[] args)
    {
        var target = AccessTools.Method(typeof(WaterFeatureHelper), method, args);
        if (target == null) throw new MissingMethodException("WaterFeatureHelper." + method);
        _owner.Patch(target, postfix: new HarmonyMethod(typeof(SilverBlood), callback));
    }
}


