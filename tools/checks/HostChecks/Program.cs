using System.Reflection;
using CorpPresence;
using CorpPresence.UI;
using CorpPresence.Runtime;
using UnityEngine;
void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
var host=new Host();
void Invoke(string method)=>typeof(Host).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(host,null);
Event.current=new(){type=EventType.Layout};Invoke("OnGUI");
Check(CompanyWindow.Draws==0 && CargoSortWindow.Draws==1,"layout skips custom panel but reaches scrollable cargo window");
Event.current.type=EventType.MouseDown;Invoke("OnGUI");
Check(CompanyWindow.Draws==0 && CargoSortWindow.Draws==2,"input remains available to cargo controls without custom repaint");
Event.current.type=EventType.Repaint;Invoke("OnGUI");
Check(CompanyWindow.Draws==1 && CargoSortWindow.Draws==3,"repaint reaches both windows exactly once");
int ticks=0;var lease=RuntimeUpdates.Register("test",()=>ticks++);Invoke("Update");
Check(ticks==1,"host executes registered module updates");lease.Dispose();Invoke("Update");
Check(ticks==1,"host does not execute revoked module updates");

namespace UnityEngine {
    internal class MonoBehaviour { }
    internal static class Time { internal static float unscaledTime=0; }
    internal enum EventType { Layout, MouseDown, Repaint }
    internal sealed class Event { internal static Event current; internal EventType type; }
}
namespace CorpPresence.UI {
    internal static class CompanyWindow { internal static int Draws; internal static void Tick(){} internal static void Draw()=>Draws++; }
    internal static class CargoSortWindow { internal static int Draws; internal static void Draw()=>Draws++; }
    internal static class Notebook { internal static void Tick(){} }
}
namespace CorpPresence {
    internal static class LoadedItemRepair { internal static void Tick(){} }
    internal static class CjkFallback { internal static void Tick(){} }
    internal static class Crates { internal static void Tick(){} }
    internal static class MachineAppearance { internal static void Tick(){} }
    internal static class Portraits { internal static void Tick(){} }
    internal static class MaintenanceDiagnostics { internal static void Record(string source,string stage,string code,Exception error){} }
    internal static class ModLog { internal static void Warn(string text){} }
}
