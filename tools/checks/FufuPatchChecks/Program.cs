using CorpPresence;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.Loader;
AssemblyLoadContext.Default.Resolving += (_,name)=> {
 var path=Path.Combine(args[0],name.Name+".dll");
 return File.Exists(path)?AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path)):null;
};
Run();
void Run(){
 var original=PatchProcessor.GetOriginalInstructions(typeof(PlayerStore).GetMethod("HandleInsurance")).ToList();
 var vanillaRoll=AccessTools.Method(typeof(RNG),"Roll",new[]{typeof(int)});
 var before=original.Select(i=>new CodeInstruction(i)).ToList();
 var result=FufuTheft.Rewrite(original).ToList();
 var replacement=AccessTools.Method(typeof(FufuTheft),"RollTheft");
 if(result.Count(i=>i.Calls(replacement))!=2||result.Count(i=>i.Calls(vanillaRoll))!=1)throw new Exception("Wrong roll replacement");
 var changes=Enumerable.Range(0,before.Count).Where(i=>!Equals(before[i].operand,result[i].operand)).ToArray();
 if(changes.Length!=2||before.Count!=result.Count)throw new Exception("Unrelated instructions changed");
 if(Enumerable.Range(0,before.Count).Any(i=>before[i].opcode!=result[i].opcode||!before[i].labels.SequenceEqual(result[i].labels)||!before[i].blocks.SequenceEqual(result[i].blocks)))throw new Exception("Lost control flow metadata");
 Console.WriteLine("PASS current game IL: exactly two theft rolls replaced; alarm and all control flow preserved");
 var unknown=new[]{new CodeInstruction(OpCodes.Ret)};
 var unchanged=FufuTheft.Rewrite(unknown).ToArray();
 if(unchanged.Length!=1||!ReferenceEquals(unchanged[0],unknown[0])||CorpPresence.ModLog.Warnings!=1)throw new Exception("Unknown IL not preserved with warning");
 Console.WriteLine("PASS unknown game IL preserved with warning; optional effect disabled");
}
