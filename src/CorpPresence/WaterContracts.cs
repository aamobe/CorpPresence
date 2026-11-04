using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
namespace CorpPresence;

// Metadata only: no native methods, resources or class initializers run here.
internal static class WaterContracts
{
    private static readonly Dictionary<short,OpCode> Opcodes=BuildOpcodes();
    private static Dictionary<short,OpCode> BuildOpcodes()
    {
        var result=new Dictionary<short,OpCode>();
        foreach(var field in typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static))
            if(field.FieldType==typeof(OpCode)) { var op=(OpCode)field.GetValue(null); result[op.Value]=op; }
        return result;
    }
    internal static void Visit(MethodInfo method,Action<OpCode,MemberInfo> consume)
    {
        var bytes=method.GetMethodBody()?.GetILAsByteArray() ?? throw new InvalidOperationException("缺少方法体："+method);
        for(int i=0;i<bytes.Length;)
        {
            short value=bytes[i++]; if(value==0xfe)value=(short)(0xfe00|bytes[i++]);
            if(!Opcodes.TryGetValue(value,out var op))throw new InvalidOperationException("未知原版 IL 操作码");
            int size; MemberInfo member=null;
            switch(op.OperandType)
            {
                case OperandType.InlineNone:size=0;break;
                case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:size=1;break;
                case OperandType.InlineVar:size=2;break;
                case OperandType.InlineI8:case OperandType.InlineR:size=8;break;
                case OperandType.InlineSwitch:size=checked(4+4*BitConverter.ToInt32(bytes,i));break;
                default:size=4;break;
            }
            if(i+size>bytes.Length)throw new InvalidOperationException("原版 IL 被截断");
            if(op.OperandType==OperandType.InlineField)member=method.Module.ResolveField(BitConverter.ToInt32(bytes,i));
            else if(op.OperandType==OperandType.InlineMethod)member=method.Module.ResolveMethod(BitConverter.ToInt32(bytes,i));
            consume(op,member); i+=size;
        }
    }
    public static void Preflight()
    {
        foreach(var type in new[]{typeof(WaterScannerUIManager),typeof(ChemScannerUIManager)})
        {
            int names=0;Visit(type.GetMethod("RefreshUI",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic),(op,member)=>
            {if(op==OpCodes.Ldfld&&member is FieldInfo field&&field.DeclaringType==typeof(Liquid)&&field.Name=="displayName")names++;});
            if(names!=1)throw new InvalidOperationException("原版扫描仪结构不受支持："+type.Name);
        }
    }
}
