using System;
using System.Globalization;
using System.Linq;

namespace CorpPresence;

internal static partial class Perfume
{
    private const string DrawSaveKey = "CorpPresence.PerfumeBloodDraw";

    private static bool ReadDraws(out int draws)
    {
        draws = 0;
        if (SaveSlot.CurrentStore == null || !SaveSlot.CanWrite(DrawSaveKey)) return false;
        string saved = SaveSlot.Read(DrawSaveKey);
        if (saved == null) return true;
        string[] parts = saved.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int day) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) || count < 0 || count > 3 || day < 0)
        {
            SaveSlot.ProtectUnreadable(DrawSaveKey);
            return false;
        }
        int today = StoreStation.GetDayCounter();
        if (day > today) return false; // Do not reset a future record on an earlier-day save.
        draws = day == today ? count : 0;
        return true;
    }

    private static GameItem BuildCollector()
    {
        var battery = new GameSlotInventory();
        var output = new GameGridInventory(4, 3);
        var note = new GameSlotInventory();
        var modules = new GameGridInventory("10000010001000001110000010001000001", 7);
        var button = new MachineryButton(L.K("production.perfume.blood.buildCollector.drawBlood", "采血", "Draw Blood"));
        var check = new MachineryButton(L.K("production.perfume.blood.buildCollector.checkConditions", "检查采血条件", "Check Conditions"));
        var window = MachineHelper.CreateMachineryWindow(true, MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.K("production.perfume.blood.buildCollector.control", "操作", "Control"), MachineryStack.Vertical(button, check), "control"),
            MachineHelper.Panel(L.K("production.perfume.blood.buildCollector.bloodBags", "血袋", "Blood Bags"), output, "output"), MachineHelper.NotePanel(note),
            MachineHelper.Panel(L.K("production.perfume.blood.buildCollector.modules", "模组", "Modules"), modules, "module"));
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("STANDARD_MACHINE_TAG");
        machine.onLoaded += _ => machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineHelper.SetupManualSlot(note);
        MachineryHelper.InitMachinery(machine, 2);
        MachineHelper.SetupModuleBay(modules, machine);
        MachineUpgrade.Attach(machine, 2, output);
        output.mayInventoryAddItemFunc = (_, _) => false;
        check.SetCallback(() => Tell(DrawProblem(machine, battery) ?? L.K("production.perfume.blood.buildCollector.readyFirstDailyDrawNo", "可以采血。每日第一次无伤，第二次轻伤，第三次重伤。", "Ready. First daily draw: no injury; second: minor wound; third: major wound.")));
        button.SetCallback(() => DrawBlood(machine, battery, output, modules));
        // Perfume artwork ships in Release too; do not depend on PREVIEW_ART's
        // conditional WatchOutput/SpriteOverride hooks for this machine.
        void RefreshAppearance()
        {
            string sprite = "corp_med_" + CollectorKey +
                (output.childItems.Any(item => item != null && item.unitCount > 0) ? "_running" : "");
            if (machine.spritePath == sprite) return;
            machine.SetSprite(machine.spriteAtlasPath, sprite);
            Goods.Redraw(machine);
        }
        output.onSlotAddItemEndFunc += (_, _, _) => RefreshAppearance();
        output.onSlotRemoveItemEndFunc += (_, _, _) => RefreshAppearance();
        machine.onLoaded += _ => RefreshAppearance();
        return machine;
    }

    private static string DrawProblem(GameItem machine, GameSlotInventory battery)
    {
        if (!GameResources.Ready || SaveSlot.CurrentStore?.healthData == null || !ReadDraws(out int count))
            return L.K("production.perfume.blood.drawProblem.bloodDrawRecordUnavailable", "采血记录暂不可用。", "Blood draw record is unavailable.");
        if (!GeneralHelper.IsItemOwned(machine)) return L.K("production.perfume.blood.drawProblem.useOwnedCollector", "只能使用自己的采血机。", "Use an owned collector.");
        if (HealthData.IsSeriouslyWounded()) return L.K("production.perfume.blood.drawProblem.cannotDrawBloodWhileSeriously", "重伤时不能采血。", "Cannot draw blood while seriously wounded.");
        if (count >= 3) return L.K("production.perfume.blood.drawProblem.alreadyDrewBloodTimesToday", "今天已经采血3次。", "Already drew blood 3 times today.");
        int power = MachineryHelper.GetMachinePowerUsage(machine);
        if (!PowerHelper.CanDrawPowerSource(battery.childItem, power))
            return L.K("production.perfume.blood.drawProblem.drawingBloodRequiresCharge", $"采血需要{power}电。", (FormattableString)$"Drawing blood requires {power} charge.");
        return null;
    }

    private static void DrawBlood(GameItem machine, GameSlotInventory battery, GameInventory output, GameInventory modules)
    {
        ModuleHelper.ComputeModuleEffect(modules.childItems.ToList(), machine);
        ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
        string reason = DrawProblem(machine, battery);
        if (reason != null) { Tell(reason); return; }
        if (!ReadDraws(out int count)) return;
        var store = SaveSlot.CurrentStore;
        var blood = ItemRegistry.Create("blood_bag");
        if (blood == null) return;
        bool committed = false;
        try
        {
            GeneralHelper.SetItemOwned(blood);
            if (!ProductionOutput.TryAdd(output, blood)) { Tell(L.K("production.perfume.blood.drawBlood.bloodBagOutputFull", "血袋出料仓空间不足。", "Blood bag output is full.")); return; }
            if (store != SaveSlot.CurrentStore || !ReadDraws(out int current) || current != count || HealthData.IsSeriouslyWounded()) return;
            if (!MachineHelper.TryDrawCyclePower(machine, battery)) return;
            // Persist globally, not on the individual machine. Loading/other machines see it.
            SaveSlot.Write(DrawSaveKey, StoreStation.GetDayCounter().ToString(CultureInfo.InvariantCulture) + ":" + (count + 1).ToString(CultureInfo.InvariantCulture));
            committed = true;
            if (count == 1) HealthData.ReceiveMinorWound();
            if (count == 2)
            {
                HealthData.ReceiveMajorWound();
                // A previously treated second draw must not make the third draw merely minor.
                store.healthData.woundState = Math.Max(6, store.healthData.woundState);
            }
            MachineHelper.OnMachineActioned(machine, modules);
            Tell(L.K("production.perfume.blood.drawBlood.bloodDrawnReceivedOrdinaryBlood", "采血完成，获得1袋普通血。", "Blood drawn: received 1 ordinary blood bag."));
        }
        finally { if (!committed) blood.Destroy(); }
    }
}
