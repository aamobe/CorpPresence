using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
namespace CorpPresence;

internal static class WaterCompatibility
{
    public static void Install(Harmony owner)
    {
        Patch(owner, typeof(WaterStripUIManager), "Open", typeof(WaterNames), nameof(WaterNames.AfterStripOpen));
        Patch(owner, typeof(WaterScannerUIManager), "GetLabeledString", typeof(WaterNames), nameof(WaterNames.AfterLabeledString));
        foreach (var type in new[]{typeof(WaterScannerUIManager),typeof(ChemScannerUIManager)})
            owner.Patch(AccessTools.Method(type,"RefreshUI") ?? throw new MissingMethodException(type.FullName,"RefreshUI"),
                transpiler:new HarmonyMethod(typeof(WaterCompatibility),nameof(LiquidDisplayTranspile)));
    }
    private static void Patch(Harmony owner,Type target,string method,Type callbackOwner,string callback)
    {
        var targetMethod=AccessTools.Method(target,method) ?? throw new MissingMethodException(target.FullName,method);
        var hook=new HarmonyMethod(callbackOwner,callback);
        owner.Patch(targetMethod,postfix:hook);
    }
    internal static IEnumerable<CodeInstruction> LiquidDisplayTranspile(IEnumerable<CodeInstruction> instructions)
    {
        var output=new List<CodeInstruction>(); int count=0;
        foreach(var original in instructions)
        {
            var code=new CodeInstruction(original);
            if(code.opcode==OpCodes.Ldfld && code.operand is FieldInfo field && field.DeclaringType==typeof(Liquid) && field.Name=="displayName")
            { code.opcode=OpCodes.Call; code.operand=AccessTools.Method(typeof(WaterNames),nameof(WaterNames.LiquidDisplay)); count++; }
            output.Add(code);
        }
        if(count!=1) throw new InvalidOperationException("原版扫描仪液体显示代码发生变化，停止安装："+count);
        return output;
    }
}

