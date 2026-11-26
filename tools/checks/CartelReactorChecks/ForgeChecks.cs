using System.Text.Json;

namespace CorpPresence;

internal static class ForgeChecks
{
    private static void Check(bool ok, string detail) { if (!ok) throw new Exception(detail); }
    private static GameItem Ingot(string grade, int units = 1)
    {
        var item = new GameItem("metal_ingot") { unitCount = units };
        IngotPurityHelper.SetPurity(item, grade);
        return item;
    }

    internal static GameItem[] Frames(GameItem[] alloy)
    {
        Check(alloy.Length == 4 && alloy.All(i => i.identifier == MeridianIndustrial.AlloyId), "four actual alloy packets");
        var root = CorpState.Current;
        var state = root.Companies["meridian"];
        var company = new MeridianCo();
        var rosen = root.Characters["rosen"];
        state.ForgeOwned = false;
        rosen.SideDone.Clear();
        Check(!MeridianIndustrial.Eligible(root), "before Rosen's offer");
        state.ForgeOwned = true;
        Check(MeridianIndustrial.Eligible(root), "older save with forge kit");
        state.ForgeOwned = false;
        rosen.SideDone.Add(5);
        Check(MeridianIndustrial.Eligible(root), "refused forge purchase still unlocks work");

        var ingots = new GameInventory();
        ingots.Put(Ingot("low"));
        var qualified = ingots.Put(Ingot(IngotPurityHelper.INGOT_PURITY_HIGH, 2));
        var billets = new GameInventory();
        var furnace = new GameItem(Forge.FurnaceId);
        var furnacePower = new GameSlotInventory();
        var furnaceCell = furnacePower.Put(new GameItem("energy_credit") { Charge = 15 });
        MachineryHelper.InitMachinery(furnace, 15);
        furnace.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", t => t.SetInt(200));
        int savedIngots = -1, savedBillets = -1, savedPowerDraws = -1;
        GraphUtils.AfterAccept = (_, _) =>
        {
            void Save() { savedIngots = qualified.unitCount; savedBillets = billets.childItems.Count; savedPowerDraws = furnaceCell.Charge; }
            if (!CompanyTransaction.DeferSave(SaveSlot.CurrentStore, Save)) Save();
        };
        try { Forge.FinishFurnaceNight(furnace, furnacePower, new GameGridInventory(7, 5), ingots, billets); }
        finally { GraphUtils.AfterAccept = null; }
        Check(billets.childItems.Count == 2 && furnaceCell.Charge == 0 && savedIngots == 0 && savedBillets == 2 && savedPowerDraws == 0
            && qualified.unitCount == 0 && ingots.childItems.Single().unitCount == 1, "native save during placement observes one paid furnace night and consumed ingots");
        var holder = new GameSlotInventory();
        var quench = new GameSlotInventory();
        var vessel = quench.Put(new GameItem("water_vessel"));
        vessel.EnableTag("LIQUID_CONTAINER_TAG");
        vessel.ModifyTag("water", t => t.SetInt(200000));
        var power = new GameSlotInventory();
        var cell = power.Put(new GameItem("energy_credit") { Charge = 12 });
        var output = new GameInventory { Capacity = 0 };
        void Move(GameInventory into, GameItem item) { item.parentInventory?.Expel(item); into.Put(item); }
        Move(holder, alloy[0]);
        Check(Forge.StrikeFrame(billets, holder, quench, power, output) != null && cell.Charge == 12
            && vessel.GetTagReadonly("water").GetInt() == 200000 && billets.childItems.Count == 2 && holder.childItem == alloy[0],
            "full output rejects without spending");
        output.Capacity = 2;
        vessel.ModifyTag("water", t => t.SetInt(0));
        vessel.ModifyTag("alcohol", t => t.SetInt(100000));
        Check(Forge.StrikeFrame(billets, holder, quench, power, output) != null && output.childItems.Count == 0
            && cell.Charge == 12 && vessel.GetTagReadonly("alcohol").GetInt() == 100000, "pure alcohol cannot supply quench water");
        vessel.ModifyTag("water", t => t.SetInt(99000));
        vessel.ModifyTag("alcohol", t => t.SetInt(0));
        vessel.ModifyTag("physical_contaminant", t => t.SetInt(1000));
        Check(Forge.StrikeFrame(billets, holder, quench, power, output) != null && output.childItems.Count == 0
            && cell.Charge == 12 && vessel.GetTagReadonly("water").GetInt() == 99000 && holder.childItem == alloy[0],
            "99 ml water and 1 ml contaminant cannot satisfy a 100 ml water cost");
        vessel.ModifyTag("water", t => t.SetInt(100000));
        Check(Forge.StrikeFrame(billets, holder, quench, power, output) == null && vessel.GetTagReadonly("water").GetInt() == 0
            && vessel.GetTagReadonly("physical_contaminant").GetInt() == 1000, "100 ml actual water consumed and contaminants retained");
        vessel.ModifyTag("water", t => t.SetInt(100000));
        Move(holder, alloy[1]);
        Check(Forge.StrikeFrame(billets, holder, quench, power, output) == null && cell.Charge == 0
            && vessel.GetTagReadonly("water").GetInt() == 0 && vessel.GetTagReadonly("physical_contaminant").GetInt() == 1000 && billets.childItems.Count == 0 && holder.childItems.Count == 0,
            "two frames consume two exact billets, two packets, 200 ml and 12 charge");

        var shop = EmporiumEntry.Instance;
        var rack = shop.frontInvinvElement;
        var original = shop.ListedOutput;
        shop.ListedOutput = rack;
        var gradeLow = rack.Put(Ingot("low"));
        var unpaid = rack.Put(Ingot(IngotPurityHelper.INGOT_PURITY_PERFECT));
        unpaid.Owned = false;
        var loose = rack.Put(Ingot(IngotPurityHelper.INGOT_PURITY_VERY_HIGH, 2));
        Move(rack, alloy[2]);
        Move(rack, alloy[3]);
        var wallet = SaveSlot.CurrentStore;
        wallet.ModCash(70, false);
        int cash = wallet.GetCash(), day = StoreStation.Day;
        var quote = new MeridianCommission(company, 2);
        Check(quote.Check() == null && new MeridianCommission(company, 0).Check() != null
            && new MeridianCommission(company, 5).Check() != null, "1-4 quantity gate");
        alloy[2].Owned = false;
        Check(quote.Submit() != null && wallet.GetCash() == cash && loose.unitCount == 2, "changed physical offer must reject");
        alloy[2].Owned = true;
        loose.AfterDestroy = () => alloy[2].Owned = false;
        Check(new MeridianCommission(company, 2).Submit() != null && root.Workshop.Commission?.Ingots == 2
            && root.Workshop.Commission.Alloy == 0 && state.LastOrderDay != day && state.Orders.Count == 0
            && cash == wallet.GetCash(), "interrupted material receipt");
        root.Workshop = JsonSerializer.Deserialize<WorkshopState>(JsonSerializer.Serialize(root.Workshop));
        alloy[2].Owned = true;
        Services.ThrowAfterPayment = true;
        var resume = new MeridianCommission(company, 2);
        Check(resume.Submit() == null && wallet.GetCash() == cash - 70 && state.LastOrderDay == day
            && unpaid.unitCount == 1 && gradeLow.unitCount == 1 && root.Workshop.Commission == null, "resume only unpaid fee and missing alloy");
        Check(resume.Submit() != null && state.Orders.Count == 1 && wallet.GetCash() == cash - 70, "duplicate confirmation");
        var order = state.Orders.Single();
        Check(order.Day == day + 2 && order.Items.Count == 2, "due in two days");
        state.Orders = JsonSerializer.Deserialize<List<Order>>(JsonSerializer.Serialize(state.Orders));
        order = state.Orders.Single();
        var counter = shop.backInvinvElement;
        int capacity = counter.Capacity;
        counter.Capacity = 0;
        MeridianFrameDelivery.Deliver(state, order, day + 1, () => true);
        Check(order.Day == day + 2 && order.Items.Count == 2, "no early delivery");
        MeridianFrameDelivery.Deliver(state, order, day + 2, () => true);
        Check(order.Day == day + 3 && order.Items.Count == 2, "full warehouse retains paid order");
        counter.Capacity = capacity;
        StoreStation.Day = day + 3;
        MeridianFrameDelivery.Deliver(state, order, day + 3, () => true);
        MeridianFrameDelivery.Deliver(state, order, day + 3, () => true);
        Check(state.Orders.Count == 0 && counter.childItems.Count(c => c.identifier == "corp_presence_crate_meridian") == 1,
            "one physical Meridian crate, no repeated delivery");
        var crate = counter.childItems.Single(c => c.identifier == "corp_presence_crate_meridian");
        var frames = output.childItems.Concat(Crates.Contents[crate].childItems).ToArray();
        Check(frames.Length == 4 && frames.Distinct().Count() == 4 && frames.All(i => i.identifier == MeridianFrameDelivery.FrameId && i.Owned), "four distinct physical frames");
        shop.ListedOutput = original;
        Console.WriteLine("PASS two self-forged and two commissioned frames, interrupted payment and reload, full-counter retry and daily quota.");
        return frames;
    }
}

internal partial class CorpState
{
    internal readonly Dictionary<string, CompanyState> Companies = new() { ["meridian"] = new() };
    internal readonly Dictionary<string, CharacterState> Characters = new() { ["rosen"] = new() };
}
internal sealed class CharacterState { internal readonly HashSet<int> SideDone = new(); }
internal sealed class CompanyState { internal bool ForgeOwned; internal int LastOrderDay = -1; internal List<Order> Orders = new(); }
internal class Company { }
internal sealed class MeridianCo : Company { }
internal static class Companies { internal static CompanyState StateOf(Company c) => CorpState.Current.Companies["meridian"]; }
internal static class Settings { internal static bool MaliceMode => false; }
internal static class Services
{
    internal static bool ThrowAfterPayment;
    internal static int CashDue(Company c, int amount) => amount;
    internal static bool CanAfford(int amount) => SaveSlot.CurrentStore.GetCash() >= amount;
    internal static string OrderBlocked(Company c) => null;
    internal static string Pay(Company c, int amount, string what)
    {
        SaveSlot.CurrentStore.ModCash(-amount, false);
        if (ThrowAfterPayment) { ThrowAfterPayment = false; throw new InvalidOperationException("register redraw"); }
        return null;
    }
}
internal static partial class Forge
{
    internal const string BilletId = "corp_presence_forged_billet", FurnaceId = "corp_presence_forge_furnace";
    internal const int WaterPerBillet = 100, BilletPower = 6, FurnacePower = 15, SpeedScale = 100;
    internal const string CarryTag = "CORP_FORGE_CARRY_INT";
}
internal static partial class ModuleHelper
{
    internal static void ComputeModuleEffect(List<GameItem> modules, GameItem machine) { }
}
internal static partial class IngotPurityHelper
{
    internal static string GetPurity(GameItem i) => i.GetTagReadonly("purity").GetString();
}
internal static class PowerHelper
{
    internal static Action<GameItem> AfterDraw;
    internal static bool CanDrawPowerSource(GameItem i, int power) => i.Charge >= power;
    internal static void SetPowerSourceAt(GameItem i, int power) => i.Charge = power;
    internal static bool DrawPowerSource(GameItem i, int power)
    {
        if (i.Charge < power) return false;
        i.Charge -= power;
        AfterDraw?.Invoke(i);
        return true;
    }
}
internal static class CurrentConverterHelper { internal static bool IsCurrentConverter(GameItem _) => false; }
internal static class HypertechBatteryHelper { internal static bool IsHypertechBattery(GameItem _) => false; }
internal static class CompanyVisitors
{
    internal static void DiscardUnshipped(GameItem i) { if (i != null && i.parentInventory == null && i.unitCount > 0) i.Destroy(); }
}
internal static class Crates
{
    internal static readonly Dictionary<GameItem, GameInventory> Contents = new();
    internal static bool Put(GameItem crate, GameItem item)
    {
        if (!Contents.TryGetValue(crate, out var inventory)) Contents[crate] = inventory = new() { ParentItem = crate };
        inventory.Put(item);
        return true;
    }
}
internal static class CounterBoundary
{
    internal sealed class Slot
    {
        internal GameInventory inventory;
        internal GameItem item;
        internal int numTransfer => item.unitCount;
        internal GameItem targetItem => null;
        internal void TryAcceptOnce() => inventory.Put(item);
    }
    internal static bool CanTakeAnything(this GameInventory inv) => inv.childItems.Count < inv.Capacity;
    internal static Slot TryFindOneValidInventorySlot(this GameInventory inv, GameItem item) =>
        inv.CanTakeAnything() && inv.mayInventoryAddItemFunc?.Invoke(item, inv) != false
            ? new Slot { inventory = inv, item = item } : null;
}
