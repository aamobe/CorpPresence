namespace CorpPresence;

internal static partial class Canning
{
    private const int SpeedScale = 100;
    public const string PasteId = "corp_presence_fruit_paste";
    private static GameItem Empty() => new("");
}
internal class GameItem(string id)
{
    public string identifier = id;
    public GameInventory parentInventory;
    public bool owned = true;
    public int unitCount = 1;
    public PixelWindow Window; public PixelWindow contentWindow => Window;
    public Action<GameItem> onLoaded;
    public Action<GameItem,GameInventory,object> onCycleEndSlotItemFunc;
    public readonly Dictionary<string,TagState> Tags = new();
    public GameItem SetContentWindow(PixelWindow window) { Window = window; return this; }
    public bool IsTag(string key) => Tags.TryGetValue(key, out var tag) && tag.Enabled;
    public void EnableTag(string key) => GetTagReadonly(key).Enable();
    public void DisableTag(string key) => GetTagReadonly(key).Enabled = false;
    public TagState GetTagReadonly(string key) => Tags.TryGetValue(key, out var tag) ? tag : Tags[key] = new();
    public void ModifyTag(string key, Action<TagState> edit) => edit(GetTagReadonly(key));
    public void Destroy() { parentInventory?.childItems.Remove(this); parentInventory = null; unitCount = 0; }
    public void SetUnitCount(int count) => unitCount = count;
}
internal class TagState
{
    public bool Enabled;
    int number;
    public TagState Enable() { Enabled = true; return this; }
    public void SetInt(int value) => number = value;
    public int GetInt() => number;
}
internal class GraphNodeStorage {}
internal class GameInventory : GraphNodeStorage
{
    public List<GameItem> childItems = new();
    public Func<GameItem,GameInventory,bool> mayInventoryAddItemFunc;
    public int Capacity = int.MaxValue;
    public void Add(GameItem item) { childItems.Add(item); item.parentInventory = this; }
}
internal class GameGridInventory : GameInventory
{
    public GameGridInventory(string mask,int width) {}
    public GameGridInventory(int width,int height) {}
}
internal class GameSlotInventory : GameInventory
{
    public GameSlotInventory(int width=1,int height=1,bool a=false,bool b=false) {}
    public GameItem childItem => childItems.FirstOrDefault();
}
internal class PixelWindow { public object childElement = null; public Panel[] Panels; public Action openCallback; }
internal record Panel(object Body);
internal class MachineryRadioButton(string name,bool initial,Action<bool> callback)
{
    public string Name = name;
    public bool IsOn = initial;
    public MachineryRadioGroup Group;
    public MachineryRadioButton SetDescription(string description) => this;
    public void SetOn(bool on,bool notify=false)
    {
        if (IsOn == on) return;
        IsOn = on;
        if (notify) callback(on);
        if (on) Group.Notify(this,notify);
    }
}
internal class MachineryRadioGroup
{
    readonly List<MachineryRadioButton> modes = new();
    public MachineryRadioGroup Add(MachineryRadioButton mode) { modes.Add(mode); mode.Group = this; return this; }
    public void Notify(MachineryRadioButton current,bool notify) { foreach (var mode in modes) if (mode != current) mode.SetOn(false,notify); }
}
internal class MachineryButton(string name)
{
    Action action;
    public string Name = name;
    public void SetCallback(Action callback) => action = callback;
    public void Click() => action();
}
internal class MachineryStack(object[] controls)
{
    public enum Align { Start, Center, End }
    public Align CrossAlignment = Align.Center;
    public object[] Controls = controls;
    public static MachineryStack Vertical(params object[] controls) => new(controls);
    public MachineryStack SetAlign(Align align) { CrossAlignment = align; return this; }
}
internal static class MachineHelper
{
    public static Panel Panel(string title,object content,string type) => new(content);
    public static Panel PowerPanel(GameSlotInventory battery) => new(battery);
    public static PixelWindow CreateMachineryWindow(bool a,int[] order,params Panel[] panels) => new() { Panels = panels };
    public static void SetupBatterySlot(GameSlotInventory slot, GameItem item) {}
    public static void SetupModuleBay(GameInventory slot, GameItem item) {}
    public static void OnMachineActioned(GameItem item, GameInventory modules) {}
}
internal static class MachineryHelper
{
    public static void InitMachinery(GameItem machine,int power) => machine.GetTagReadonly("power").SetInt(power);
    public static int GetMachinePowerUsage(GameItem machine) => machine.GetTagReadonly("power").GetInt();
}
internal static class MachineUpgrade { public static void Attach(GameItem item,int level,params GameInventory[] inv) {} }
internal static class GeneralHelper
{
    public static bool IsItemOwned(GameItem item) => item.owned;
    public static void SetItemOwned(GameItem item) => item.owned = true;
}
internal static class ModuleHelper
{
    public static void ComputeModuleEffect(List<GameItem> items,GameItem machine) {}
    public static void ApplyBasicModuleEffect(GameInventory inv,object unused,GameItem machine) {}
}
internal static class Goods
{
    public class Def
    {
        public string Flavor,FlavorEn,Name,NameEn,Company,Manufacturer,Art,Fallback,Desc,DescEn;
        public int Value,W,H;
        public string[] Types;
        public Func<GameItem> Shell;
        public Action<GameItem> Setup;
    }
    public static string IdOf(GameItem item) => item?.identifier; public static GameItem Make(string id) => id is "fish_can" or "crab_stick" ? new(id) { owned = false } : null;
}
internal static class ItemRegistry { public static GameItem Create(string id) => id == "galaxy_blend" ? new(id) { owned = false } : null; }
internal static class AudioHelper { public static void InitCannedFood(GameItem item) {} public static void InitFoodAudio(GameItem item) {} }
internal static class FoodItemHelper { public static void InitFoodItem(GameItem item,int kcal,bool isCanned,bool isMeat) {} }
internal static class ModLog { public static void Info(string text) {} public static void Warn(string text) => Console.WriteLine(text); }
internal static class L { public static string T(string zh,string en) => en; public static string K(string key,string zh,string en) => en; public static string K(string key,string zh,FormattableString en) => en.ToString(); }
internal class StoreUIManager
{
    public static StoreUIManager Instance = new();
    public string Message;
    public void Notify(string message) => Message = message;
}
internal enum FishSpecies { Tilapia }
internal enum FishStage { Adolescent }
internal static class AquacultureHelper
{
    public static bool IsFish(GameItem item) => item.identifier == "tilapia";
    public static bool IsGroup(GameItem item) => false;
    public static FishSpecies GetSpecies(GameItem item) => FishSpecies.Tilapia;
    public static bool IsDead(GameItem item) => true;
    public static FishStage GetStage(GameItem item) => FishStage.Adolescent;
}
internal class Liquid { public string currentPart = "water_ml"; public static Liquid[] Liquids = { new() }; }
internal static class WaterHelper
{
    public static int GetTotalVolume(GameItem item) => item.GetTagReadonly("water_ml").GetInt();
    public static int GetWaterPurity(GameItem item) => 2;
    public static void RemoveMl(GameItem item,int ml) => item.GetTagReadonly("water_ml").SetInt(GetTotalVolume(item)-1000*ml);
}
internal static class WaterFeatureHelper
{
    public static int GetPurityArrayIndex(int purity) => purity;
    public static void OnLiquidContainerPourOutOf(GameItem item) {}
}
internal static class PowerHelper
{
    public static int FailureMode; public static bool CanDrawPowerSource(GameItem item,int amount) => item.GetTagReadonly("power_source_item_energy").GetInt() >= amount;
    public static bool DrawPowerSource(GameItem item,int amount)
    {
        if (!CanDrawPowerSource(item,amount)) return false;
        if (FailureMode == 1) return false; if (FailureMode == 2) throw new InvalidOperationException("injected before debit"); var charge = item.GetTagReadonly("power_source_item_energy"); charge.SetInt(charge.GetInt()-amount); if (FailureMode == 3) return false; if (FailureMode == 4) throw new InvalidOperationException("injected after debit"); return true;
    }
    public static void UpdateEnergyCreditSprite(GameItem item) {}
}
internal static class CurrentConverterHelper { public static bool IsCurrentConverter(GameItem item) => false; }
internal static class HypertechBatteryHelper { public static bool IsHypertechBattery(GameItem item) => false; }
internal static class GraphUtils
{
    public static bool CanAccept(GameInventory inv,GameItem item) => inv.childItems.Count < inv.Capacity && (inv.mayInventoryAddItemFunc?.Invoke(item,inv) ?? true);
    public static void TryAcceptAll(GameInventory inv,GameItem item) { if (CanAccept(inv,item)) inv.Add(item); }
}




internal class MachineryPanelRow { public Panel GetPanel(string role) => null; }
internal static class PanelExtensions { public static void CollectStorage(this Panel panel,List<GraphNodeStorage> nodes) {} }
internal static class Perfume { public const string CollectorKey = "perfume_collector"; }


internal static class MachineAppearance { public static void Rearm(GameItem item) {} public static void WatchCycle(GameItem item,GameGridInventory output,Func<string> ready) {} public static bool CanPowerCycle(GameItem item,GameSlotInventory battery,string carry) => true; }
