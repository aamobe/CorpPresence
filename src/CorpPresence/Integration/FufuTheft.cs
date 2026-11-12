using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace CorpPresence;

internal static class FufuTheft
{
    internal static void Install(Harmony harmony)
    {
        PatchInstallation.Run("PlayerStore.HandleInsurance/Fufu", () =>
        {
            var target = AccessTools.Method(typeof(PlayerStore), nameof(PlayerStore.HandleInsurance), Type.EmptyTypes);
            if (target == null) return false;
            harmony.Patch(target, transpiler: new HarmonyMethod(typeof(FufuTheft), nameof(Rewrite)));
            return true;
        }, required: false);
    }

    // Only the two theft rolls in this method are replaced; the alarm roll stays native.
    internal static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var roll = AccessTools.Method(typeof(RNG), nameof(RNG.Roll), new[] { typeof(int) });
        var calls = Enumerable.Range(0, code.Count).Where(i => code[i].Calls(roll)).ToArray();
        if (calls.Length != 3 || calls[0] == 0 || calls[2] == 0
            || code[calls[0] - 1].opcode != OpCodes.Add
            || code[calls[2] - 1].opcode != OpCodes.Ldc_I4_6
            || !code.Skip(calls[0] + 1).Take(calls[1] - calls[0] - 1)
                .Any(i => i.operand is System.Reflection.MethodInfo method
                    && method.DeclaringType == typeof(MachineAlarm) && method.Name == "GetAlarmeStopRate"))
        {
            ModLog.Warn("Fufu偷盗率效果已停用：保险判定结构不匹配，保留原版结算。");
            return code;
        }
        var replacement = AccessTools.Method(typeof(FufuTheft), nameof(RollTheft));
        code[calls[0]].operand = replacement;
        code[calls[2]].operand = replacement;
        return code;
    }

    internal static bool RollTheft(int chance)
    {
        bool owned = !Settings.CleanupForUninstall && SaveSlot.CurrentStore != null
            && EmporiumEntry.Instance != null
            && EmporiumEntry.Instance.GetAllOwnedItems().Any(item => item?.identifier == FufuDoll.ItemId);
        return owned ? RNG.Roll(FufuTheftChance.Apply(chance, true)) : RNG.Roll(chance);
    }
}
