namespace CorpPresence.UI { internal class Placeholder { } }
namespace CorpPresence
{
    internal sealed class TestTag
    {
        internal int Number;
        internal long Long;
        internal string Text;
        internal TestTag Enable() => this;
        internal TestTag SetInt(int n) { Number = n; return this; }
        internal TestTag SetString(string s) { Text = s; return this; }
        internal TestTag SetBool(bool value) => SetInt(value ? 1 : 0);
        internal int GetInt() => Number;
        internal long GetLong() => Long;
        internal TestTag SetLong(long value) { Long=value; return this; }
        internal bool GetBool() => Number != 0;
        internal string GetString() => Text;
    }
    internal class GraphNodeStorage { }
    internal class GameItem
    {
        internal string identifier;
        internal int uniqueId { get; set; }
        internal (int minX, int minY) modifiedShape { get; set; }
        internal void DisableTag(string key, bool skipValidate = false) => Tags.Remove(key);
        internal int unitCount = 1;
        internal GameInventory parentInventory;
        internal PixelWindow contentWindow;
        internal int Charge
        {
            get => GetTagReadonly("power_source_item_energy").GetInt();
            set { EnableTag("power_source_item"); ModifyTag("power_source_item_energy", tag=>tag.SetInt(value)); }
        }
        internal bool Owned = true;
        internal int[] Parts = new int[5];
        internal Dictionary<string, TestTag> Tags = new();
        internal Func<GameItem, GameItem, bool> mayStackItemFunc;
        internal Func<GameItem, GameItem, bool> mayThisTargetItemFunc, canThisTargetItemFunc;
        internal Action<GameItem, GameItem> onThisTargetItemFunc;
        internal Func<GameItem, GameInventory, object, int> maxSlotAmountItemFunc;
        internal Action<GameItem, GameInventory, SlotMarker> onSlotRemoveItemFunc;
        internal Action<GameItem, GameInventory, object> onCycleEndSlotItemFunc;
        internal Action<GameItem> onLoaded;
        internal GameItem(string id = null) { identifier = id; }
        internal GameItem SetContentWindow(PixelWindow w) { contentWindow = w; return this; }
        internal void EnableTag(string key) => ModifyTag(key, t => t.SetInt(1));
        internal bool IsTag(string key) => GetTagReadonly(key).GetBool();
        internal TestTag GetTagReadonly(string key) => Tags.GetValueOrDefault(key) ?? new();
        internal void ModifyTag(string key, Action<TestTag> update, bool skipValidate=false)
        { if (!Tags.TryGetValue(key, out var tag)) Tags.Add(key, tag = new()); update(tag); }
        internal bool FailCountRedraw;
        internal void SetUnitCount(int n) { unitCount=n; if(FailCountRedraw)throw new InvalidOperationException("count redraw"); }
        internal Action BeforeDestroy, AfterDestroy;
        private bool destroyed;
        internal void Destroy()
        {
            if (destroyed) return;
            BeforeDestroy?.Invoke();
            destroyed = true;
            parentInventory?.Expel(this);
            unitCount=0;
            AfterDestroy?.Invoke();
        }
        internal void Night() => onCycleEndSlotItemFunc(this, null, null);
        internal GameInventory Slot(string key) => contentWindow.childElement.GetPanel(key).Inventory;
    }
    internal class GameItemElement : GameItem { internal bool IsDestroyed() => unitCount == 0; }
    internal class GameInventory : GraphNodeStorage
    {
        internal GameItem ParentItem;
        internal Func<GameItem, GameInventory, bool> mayInventoryAddItemFunc;
        internal Func<GameItem, GameInventory, bool> mayInventoryRemoveItemFunc;
        internal Action<GameItem, GameInventory, SlotMarker> onSlotRemoveItemFunc;
        internal Action<GameItem, GameInventory, SlotMarker> onSlotAddItemEndFunc, onSlotRemoveItemEndFunc, onSlotChangeAmountItemFunc;
        internal List<GameItem> childItems = new();
        internal int Capacity = 54;
        internal GameItem Put(GameItem i)
        {
            if (mayInventoryAddItemFunc?.Invoke(i, this) == false || childItems.Count >= Capacity) throw new Exception("Rejected "+i.identifier);
            childItems.Add(i); i.parentInventory=this; return i;
        }
    }
    internal class GameGridInventory : GameInventory { internal GameGridInventory(int w,int h){Capacity=w*h;} }
    internal class GameSlotInventory : GameInventory
    {
        internal GameSlotInventory(int w=1,int h=1){ Capacity=1; }
        internal GameItem childItem => childItems.FirstOrDefault();
    }
    internal class GraphUtils
    {
        internal static bool CanAccept(GameInventory inv,GameItem item) => inv.childItems.Count < inv.Capacity && inv.mayInventoryAddItemFunc?.Invoke(item,inv)!=false;
        internal static Action<GameInventory,GameItem> AfterAccept;
        internal static void TryAcceptAll(GameInventory inv, GameItem item)
        {
            if (item.parentInventory is { } existing && existing != inv) existing.Expel(item);
            inv.Put(item);
            AfterAccept?.Invoke(inv, item);
        }
    }
    internal class PixelWindow
    {
        internal MachineryPanelRow childElement;
        internal PixelWindow(bool unused=true,object arg=null,WindowStyle type=WindowStyle.Machinery) {}
        internal void Attach(MachineryPanelRow row) { childElement=row; }
        internal Action openCallback;
        internal Action closeCallback;
        internal PixelWindow Show() { openCallback?.Invoke(); return this; }
    }
    internal enum WindowStyle { Machinery }
    internal class Panel
    {
        internal string Key;
        internal GameInventory Inventory;
        internal object Content;
        internal bool Hidden;
        internal Panel SetHidden(bool b) { Hidden = b; return this; }
        internal void CollectStorage(List<GraphNodeStorage> nodes) { if(Inventory!=null)nodes.Add(Inventory); }
    }
    internal class MachineryPanelRow
    {
        private readonly Panel[] _panels;
        internal MachineryPanelRow(params Panel[] panels){_panels=panels;}
        internal MachineryPanelRow SetColumns(int n)=>this;
        internal MachineryPanelRow SetDisplayOrder(params int[] order)=>this;
        internal Panel GetPanel(string id)=>_panels.First(p=>p.Key==id);
    }
    internal class MachineryText { internal MachineryText(bool uppercase){} internal string Text; internal void SetText(string s){Text=s;} }
    internal class MachineryButton { internal MachineryButton(string s=""){} internal string Text; internal void SetText(string s){Text=s;} internal Action Click; internal MachineryButton SetCallback(Action a){Click=a;return this;} }
    internal sealed class MachineryStack
    {
        internal object[] Items;
        internal static MachineryStack Horizontal(params object[] content) => new(){Items=content};
        internal static MachineryStack Vertical(params object[] content) => new(){Items=content};
    }
    internal static partial class MachineHelper
    {
        internal static Panel Panel(string title,object content,string id) => new(){Key=id,Content=content is MachineryStack stack ? stack.Items : content,Inventory=content as GameInventory};
        internal static Panel PowerPanel(GameSlotInventory slot)=>Panel("",slot,"power");
        internal static PixelWindow CreateMachineryWindow(bool value,int[] order,params Panel[] panels)
        {var window=new PixelWindow();window.Attach(new(panels));return window;}
        internal static void SetupBatterySlot(GameSlotInventory slot,GameItem machine){}
        internal static void SetupModuleBay(GameGridInventory slot,GameItem machine,Action refresh){}
        internal static bool CanPower(GameItem machine,GameSlotInventory slot)=>slot.childItem!=null && slot.childItem.Charge>=MachineryHelper.GetMachinePowerUsage(machine);
        internal static bool ThrowAfterDraw;
        internal static bool TryDrawCyclePower(GameItem machine,GameSlotInventory slot)
        { if(!CanPower(machine,slot))return false;slot.childItem.Charge-=MachineryHelper.GetMachinePowerUsage(machine);if(ThrowAfterDraw)throw new Exception("injected battery redraw");return true;}
        internal static void OnMachineActioned(GameItem item,GameInventory modules){}
    }
    internal static class MachineryHelper
    {
        internal const string IS_MACHINERY_POWERED="on";
        internal const string MACHINERY_DRAWN_POWER="cost", MACHINERY_DRAWN_POWER_BASE="base";
        internal static void InitMachinery(GameItem machine,int power){machine.ModifyTag("cost",t=>t.SetInt(power));machine.EnableTag(IS_MACHINERY_POWERED);}
        internal static int GetMachinePowerUsage(GameItem i)=>i.GetTagReadonly("cost").GetInt();
    }
    internal static partial class ModuleHelper
    {
        internal const string TOTAL_PERCENTAGE_QUALITY_BONUS_INT="quality", TOTAL_PERCENTAGE_POWER_BONUS_INT="efficiency";
        internal static void ComputeModuleEffect(GameInventory m,object unused,GameItem i){}
        internal static void ApplyBasicModuleEffect(GameInventory m,object unused,GameItem i){}
    }
    internal static class MineralModuleBay {internal static GameGridInventory Create()=>new(7,5);}
    internal static partial class MachineAppearance
    {
        internal static void Refresh(GameItem item){}
        internal static void WatchOutput(GameItem item,GameInventory slot){}
        internal static bool CanFit(GameInventory slot,string product,int count=1,bool separateUnit=false)=>slot.Capacity-slot.childItems.Count>=count;
    }
    internal static class ItemDirectory { internal static GameItem CreateEmptyItem()=>new(); }
    internal static class DirectoryMaster
    {
        internal static bool Has<T>(string id)=>true;
        internal static GameItem Item(string id,bool isOwned)
        {
            var item = id switch
            {
                "corp_presence_" + MineralCrusher.FeederKey => MineralCrusher.FeederDefinition.Shell(),
                "corp_presence_" + IndustrialPump.Key => IndustrialPump.Definition.Shell(),
                "corp_presence_" + CoreCalibration.Key => CoreCalibration.Definition.Shell(),
                "corp_presence_" + FoldedFactory.Key => FoldedFactory.Definition.Shell(),
                "corp_presence_" + PhaseRecoveryStation.Key => PhaseRecoveryStation.Definitions[PhaseRecoveryStation.Key].Shell(),
                _ => new GameItem(id)
            };
            item.identifier = id;
            item.Owned = isOwned;
            return item;
        }
    }
    internal static class Goods
    {
        internal enum ArtKind { Embedded, NativeSprite, TintedNativeSprite }
        internal static void Redraw(GameItem item){}
        internal static void RefreshRodSprite(GameItem item){}
        internal static void Relabel(GameItem item, string key){}
        internal sealed class Def
        {
            internal ArtKind ArtSource;
            public string Name,NameEn,Company,Art,Fallback,Shape,Desc,DescEn; public int Value,W,H; public string[] Types;
            public Func<GameItem> Shell; public Action<GameItem> Setup;
        }
        internal static string NameOf(string key) => key;
        internal static string IdOf(GameItem item)=>item?.identifier?.Replace("corp_presence_","");
        internal static GameItem Make(string key)=>new("corp_presence_"+key);
    }
    internal static partial class PowderBox
    {
        internal const int ComponentCount=5;
        internal static bool IsOwnedBox(GameItem i)=>i?.identifier=="corp_presence_powder_box" && i.Owned;
        internal static long Read(GameItem i,Span<int> dest){i.Parts.AsSpan().CopyTo(dest);return i.Parts.Sum();}
        internal static void WriteComposition(GameItem i,int[] p)=>CommitComposition(i,p);
        internal static void CommitComposition(GameItem i,ReadOnlySpan<int> p)=>i.Parts=p.ToArray();
        internal static bool FailPowderRedraw;
        internal static void RefreshComposition(GameItem i,ReadOnlySpan<int> p)
        { if(FailPowderRedraw) throw new InvalidOperationException("injected powder redraw"); }
        internal static string ComponentName(PowderComponent p)=>p.ToString();
    }
    internal static class PowderColor {internal const int Capacity=500;}
    internal sealed class Liquid
    {
        internal readonly string identifier;
        internal string currentPart => identifier;
        private Liquid(string id) { identifier=id; }
        internal static readonly List<Liquid> Liquids = new() { new("water"), new("alcohol"), new("physical_contaminant") };
        internal static string GetCurrentPartFromId(string id)=>id;
    }
    internal static partial class WaterHelper
    {
        internal static int[] GetLiquidParts(GameItem item) => Liquid.Liquids.Select(l=>item.GetTagReadonly(l.currentPart).GetInt()).ToArray();
        internal static int GetFreeCapacity(GameItem item) => item.GetTagReadonly("capacity").GetInt() - GetLiquidParts(item).Sum();
    }
    internal static class MachineBlender { internal static void SetupChemModuleGrid(GameGridInventory grid, GameItem machine) {} }
    internal static class MachineChemFinisher { internal static int GetLoss(GameItem machine) => 0; }
    internal class WaterFeature {internal bool IsExposed()=>false;}
    internal static partial class WaterFeatureHelper
    {
        internal static WaterFeature GetWaterFeature(GameItem i)=>i?.IsTag("LIQUID_CONTAINER_TAG")==true ? new() : null;
        internal static bool FailRedraw;
        internal static void OnLiquidChanged(GameItem i,bool b){if(FailRedraw)throw new InvalidOperationException("water redraw");}
    }
    internal static class CartelChemistry {internal static void Initialize(GameItem i,string art){}}
    internal static class GeneralHelper {internal static bool IsItemOwned(GameItem i)=>i?.Owned==true;internal static void SetItemOwned(GameItem i)=>i.Owned=true;}
    internal static class SleepMoss {internal const string BlockId="corp_presence_sleep_moss_block";}
    internal static class L
    {
        internal static string K(string key,string zh,string en)=>en;
        internal static string T(string zh,string en)=>en;
        internal static string F(string key,string zh,string en,params object[] args)=>string.Format(en,args);
    }
    internal static class LocHelper {internal static string GetLocalizedItem(string key)=>key; internal static string GetLocalizedMechanic(string key)=>key;}
    internal static class ModLog {internal static void Warn(string msg)=>Console.WriteLine(msg);}
    internal static class MaintenanceDiagnostics { internal static void Record(string area,string what,string flag,Exception error){} }
    internal static class SaveSlot {internal static PlayerStore CurrentStore {get;}=new();internal static bool CanWrite(string key)=>true;}
    internal class StoreSession {internal static bool CompanyProgressPaused {get;set;} internal static StoreSession Capture()=>new();internal bool IsCurrent=>true;}
}
