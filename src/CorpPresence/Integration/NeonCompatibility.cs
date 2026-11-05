using System;
using HarmonyLib;

namespace CorpPresence;

// Optional integration: keep our registered species out of Neon's generic fish
// extensions. No third-party assembly dependency, water hooks or saved-state edits.
internal static class NeonCompatibility
{
    public static void Install(Harmony harmony)
    {
        try
        {
            var target = AccessTools.Method("WaterSellerPack.NeonFish:Fish", new[] { typeof(GameItem) });
            if (target == null) return; // Not installed/loaded: leave the native flow alone.
            if (!target.IsStatic || target.ReturnType != typeof(bool))
            {
                ModLog.Warn("Neon 鱼类接口已变化，跳过三叶蜥兼容钩子");
                return;
            }
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(NeonCompatibility), nameof(BeforeFish))
            {
                priority = Priority.First
            });
            ModLog.Info("Neon 兼容已启用：通用鱼类扩展跳过银血三叶蜥，其余鱼类规则保留");
        }
        catch (Exception error)
        {
            // An optional companion must not prevent company content from loading.
            ModLog.Warn("Neon 可选兼容未启用：" + error.Message);
        }
    }

    private static bool BeforeFish(GameItem __0, ref bool __result)
    {
        if (!SilverBlood.IsCreature(__0)) return true;
        __result = false;
        return false;
    }
}
