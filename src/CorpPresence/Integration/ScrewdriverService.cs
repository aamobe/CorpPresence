using System;

namespace CorpPresence;

internal static class ScrewdriverService
{
    internal static void AfterCreate(GameItem __result)
    {
        if (__result == null) return;
        var mayTarget = __result.mayThisTargetItemFunc;
        var canTarget = __result.canThisTargetItemFunc;
        var use = __result.onThisTargetItemFunc;
        __result.mayThisTargetItemFunc = (tool, target) =>
            MachineService.CanOpen(tool, target) || mayTarget?.Invoke(tool, target) == true;
        __result.canThisTargetItemFunc = (tool, target) =>
            MachineService.CanOpen(tool, target) || canTarget?.Invoke(tool, target) == true;
        __result.onThisTargetItemFunc = (tool, target) =>
        {
            if (MachineService.CanOpen(tool, target))
                HookGuard.Run("螺丝刀打开公司设备检修盖", () => MachineService.TryOpen(tool, target));
            else
                use?.Invoke(tool, target);
        };
    }
}
