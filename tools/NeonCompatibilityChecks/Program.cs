using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
var compatibility=Assembly.GetExecutingAssembly().GetType("CorpPresence.NeonCompatibility");
if(compatibility==null){Console.WriteLine("FAIL optional Neon compatibility is missing");return 1;}
#if NEON_ABSENT
compatibility.GetMethod("Install",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{new Harmony("test.corp.neon.absent")});
Console.WriteLine("PASS Neon absent: optional integration returns without loading a dependency");return 0;
#else
var h=new Harmony("test.corp.neon");
try {
 compatibility.GetMethod("Install",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{h});
 void Assert(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS "+message);}
 var own=new GameItem{Species="FISH_CORP_SILVERBLOOD"};
 WaterSellerPack.NeonFish.Calls=0;
 Assert(!WaterSellerPack.NeonFish.Fish(own)&&WaterSellerPack.NeonFish.Calls==0,"own species skips Neon predicate entirely");
 Assert(WaterSellerPack.NeonFish.Fish(new GameItem{Species="TILAPIA"}),"native fish retains Neon behavior");
 Assert(WaterSellerPack.NeonFish.Fish(new GameItem{Species="OTHER_MOD_SPECIES"}),"other mod species retains Neon behavior");
 Assert(!WaterSellerPack.NeonFish.Fish(null),"null retains original behavior");
 h.UnpatchAll(h.Id);
 Assert(WaterSellerPack.NeonFish.Fish(own),"unload restores original predicate");
 Console.WriteLine("5 passed, 0 failed");return 0;
} catch(Exception e){Console.WriteLine("FAIL "+e);return 1;} finally {h.UnpatchAll(h.Id);}
#endif
public class GameItem {public string Species;}
namespace CorpPresence {
 static class SilverBlood {public static bool IsCreature(GameItem item)=>item?.Species=="FISH_CORP_SILVERBLOOD";}
 static class ModLog {public static void Info(string text)=>Console.WriteLine(text);public static void Warn(string text)=>Console.WriteLine(text);}
}
#if !NEON_ABSENT
namespace WaterSellerPack {
 public static class NeonFish {
 public static int Calls;
 [MethodImpl(MethodImplOptions.NoInlining)]public static bool Fish(GameItem item){Calls++;return item?.Species!=null;}
 }
}



#endif
