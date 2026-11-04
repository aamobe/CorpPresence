using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
namespace CorpPresence;

internal static partial class WaterCompatibility
{
    internal sealed class Binding
    {
        internal readonly string TypeName, Method;
        internal readonly int Fields, Reads;
        internal Binding(string type, string method, int fields, int reads) { TypeName=type; Method=method; Fields=fields; Reads=reads; }
        internal MethodInfo Resolve() => typeof(WaterHelper).Assembly.GetType(TypeName, true).GetMethod(Method, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly)
            ?? throw new MissingMethodException(TypeName, Method);
    }
    public static void Install(Harmony owner)
    {
        var transpiler = new HarmonyMethod(typeof(WaterCompatibility), nameof(Transpile));
        foreach (var binding in Bindings) owner.Patch(binding.Resolve(), transpiler: transpiler);
        owner.Patch(AccessTools.Method(typeof(WaterHelper), "WillAcceptWater"), transpiler: new HarmonyMethod(typeof(WaterCompatibility), nameof(AcceptanceTranspile)));
        Patch(owner, typeof(ItemFeature), "IsFeatureMatch", typeof(WaterIdentity), nameof(WaterIdentity.BeforeFeatureMatch), true);
        Patch(owner, typeof(ItemFeature), "GetBaseRealDisplay", typeof(WaterNames), nameof(WaterNames.AfterRealDisplay));
        Patch(owner, typeof(ItemFeature), "GetBaseFakeDisplay", typeof(WaterNames), nameof(WaterNames.AfterFakeDisplay));
        Patch(owner, typeof(WaterStripUIManager), "Open", typeof(WaterNames), nameof(WaterNames.AfterStripOpen));
        Patch(owner, typeof(WaterScannerUIManager), "GetLabeledString", typeof(WaterNames), nameof(WaterNames.AfterLabeledString));
        foreach (var type in new[]{typeof(WaterScannerUIManager),typeof(ChemScannerUIManager)})
            owner.Patch(AccessTools.Method(type,"RefreshUI") ?? throw new MissingMethodException(type.FullName,"RefreshUI"),
                transpiler:new HarmonyMethod(typeof(WaterCompatibility),nameof(LiquidDisplayTranspile)));
        ModLog.Info("[水质身份兼容] 已将 " + Bindings.Length + " 处原版判断改为固定水质键；试纸按纯度着色；存档字段保持原样。");
    }
    private static void Patch(Harmony owner,Type target,string method,Type callbackOwner,string callback,bool prefix=false)
    {
        var targetMethod=AccessTools.Method(target,method) ?? throw new MissingMethodException(target.FullName,method);
        var hook=new HarmonyMethod(callbackOwner,callback);
        owner.Patch(targetMethod,prefix:prefix?hook:null,postfix:prefix?null:hook);
    }
    internal static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        Binding binding=null;
        foreach(var item in Bindings)
            if(item.TypeName==__originalMethod.DeclaringType.FullName && item.Method==__originalMethod.Name) { binding=item; break; }
        if(binding==null) throw new InvalidOperationException("未验证的水质补丁目标："+__originalMethod);
        return Rewrite(instructions,binding.Fields,binding.Reads);
    }
    // Materialize and validate the entire edit before yielding anything to Harmony.
    internal static List<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions,int expectedFields,int expectedReads)
    {
        int fields=0,reads=0;
        var output=new List<CodeInstruction>();
        foreach(var original in instructions)
        {
            var code=new CodeInstruction(original);
            if(code.opcode==OpCodes.Ldsfld && code.operand is FieldInfo field && field.DeclaringType==typeof(WaterFeatureHelper))
            {
                var key=WaterGrades.FromField(field.Name);
                if(key!=null) { code.opcode=OpCodes.Ldstr; code.operand=key; fields++; }
            }
            else if((code.opcode==OpCodes.Call || code.opcode==OpCodes.Callvirt) && code.operand is MethodInfo method)
            {
                string replacement=null;
                if(method.DeclaringType==typeof(WaterFeatureHelper) && method.Name=="GetCurrentWaterDisplay") replacement=nameof(WaterIdentity.CurrentKey);
                if(method.DeclaringType==typeof(ItemFeature) && method.Name=="GetBaseRealDisplay") replacement=nameof(WaterIdentity.RealKey);
                if(method.DeclaringType==typeof(ItemFeature) && method.Name=="GetBaseFakeDisplay") replacement=nameof(WaterIdentity.FakeKey);

                if(replacement!=null) { code.opcode=OpCodes.Call; code.operand=AccessTools.Method(typeof(WaterIdentity),replacement); reads++; }
            }
            output.Add(code);
        }
        if(fields!=expectedFields || reads!=expectedReads)
            throw new InvalidOperationException($"原版水质判断代码发生变化，停止安装：常量 {fields}/{expectedFields}，读取 {reads}/{expectedReads}");
        return output;
    }
    internal static IEnumerable<CodeInstruction> AcceptanceTranspile(IEnumerable<CodeInstruction> instructions)
    {
        var output=new List<CodeInstruction>(); int reads=0,contains=0;
        foreach(var original in instructions)
        {
            var code=new CodeInstruction(original);
            if((code.opcode==OpCodes.Call || code.opcode==OpCodes.Callvirt) && code.operand is MethodInfo method)
            {
                if(method.DeclaringType==typeof(WaterFeatureHelper) && method.Name=="GetCurrentWaterDisplay")
                { code.opcode=OpCodes.Call; code.operand=AccessTools.Method(typeof(WaterIdentity),nameof(WaterIdentity.AcceptanceKey)); reads++; }
                else if(method.DeclaringType==typeof(List<string>) && method.Name=="Contains")
                { code.opcode=OpCodes.Call; code.operand=AccessTools.Method(typeof(WaterIdentity),nameof(WaterIdentity.AcceptContains)); contains++; }
            }
            output.Add(code);
        }
        if(reads!=1 || contains!=1) throw new InvalidOperationException("原版水质收购函数结构变化，停止安装");
        return output;
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

