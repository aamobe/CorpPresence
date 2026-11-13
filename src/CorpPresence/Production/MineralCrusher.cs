using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class MineralCrusher
{
    internal const string Key = "mineral_crusher";
    private const string PreserveResidueTag = "CORP_CRUSHER_PRESERVE_RESIDUE";
    internal static readonly Goods.Def Definition = new()
    {
        Name = "矿粉破碎机", NameEn = "Mineral Crusher", Value = 200,
        Company = "mineral", Art = "mineral-crusher", Fallback = "furnace",
        W = 3, H = 3, Types = new[] { "MACHINE" }, Shell = Build
    };

    private static bool IsFeed(GameItem item) =>
        item != null && (item.identifier == "junk" || item.identifier == "scrap_metal")
        && item.unitCount > 0 && GeneralHelper.IsItemOwned(item);

    private static bool PreserveResidue(GameItem machine) => machine.GetTagReadonly(PreserveResidueTag).GetBool();

    private static int EffectiveQuality(GameItem machine) =>
        CrusherBatch.EffectiveQuality(machine.GetTagReadonly("TOTAL_PERCENTAGE_QUALITY_BONUS_INT").GetInt(), PreserveResidue(machine));

    private static GameItem Build()
    {
        var input = new GameGridInventory(8, 8) { mayInventoryAddItemFunc = (item, _) => IsFeed(item) };
        var receiver = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => PowderBox.IsOwnedBox(item) };
        var battery = new GameSlotInventory();
        var modules = MineralModuleBay.Create();
        var status = new MachineryText(uppercase: false);
        var qualitySwitch = new MachineryButton();
        var window = MachineHelper.CreateMachineryWindow(true,
            MachineHelper.Panel(L.T("垃圾／废金属", "Junk / scrap metal"), input, "input"),
            MachineHelper.Panel(L.T("接粉盒", "Powder box"), receiver, "receiver"),
            MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.T("模组", "Modules"), modules, "module"),
            MachineHelper.Panel(L.T("过夜加工", "Overnight processing"), MachineryStack.Vertical(status, qualitySwitch), "control"));
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, 12);
        machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(300));
        machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(300));

        void Refresh()
        {
            int limit = CrusherBatch.NightlyLimit(machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());
            int power = MachineryHelper.GetMachinePowerUsage(machine);
            int quality = EffectiveQuality(machine);
            qualitySwitch.SetText(PreserveResidue(machine)
                ? L.K("production.crusher.qualityOff", "高质量破碎 · 关", "High-quality crushing · Off")
                : L.K("production.crusher.qualityOn", "高质量破碎 · 开", "High-quality crushing · On"));
            status.SetText(L.T($"每夜最多 {limit} 件\n每夜耗电 {power}\n渣料减量 {quality}%",
                $"Up to {limit} items/night\n{power} charge/night\n{quality}% less residue"));
        }

        qualitySwitch.SetCallback(() => HookGuard.Run("切换破碎质量", () =>
        {
            if (!GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence")) return;
            bool preserveResidue = !PreserveResidue(machine);
            machine.ModifyTag(PreserveResidueTag, tag => tag.Enable().SetBool(preserveResidue));
            Refresh();
        }));
        MachineHelper.SetupModuleBay(modules, machine, Refresh);
        window.openCallback += Refresh;
        Refresh();
        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run("矿粉破碎", () =>
        {
            if (!GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence")) return;
            ModuleHelper.ComputeModuleEffect(modules, null, machine);
            ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
            Crush(machine, input, receiver.childItem, battery);
            Refresh();
        });
        MachineAppearance.WatchCycle(machine, () => CanStart(machine, input, receiver.childItem, battery));
        return machine;
    }

    private static bool CanStart(GameItem machine, GameGridInventory input, GameItem box, GameSlotInventory battery)
    {
        if (!GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence")
            || !machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
            || !PowderBox.IsOwnedBox(box) || !MachineHelper.CanPower(machine, battery)) return false;

        foreach (var item in input.childItems)
        {
            if (!IsFeed(item)) continue;
            Span<int> parts = stackalloc int[PowderBox.ComponentCount];
            long total = PowderBox.Read(box, parts);
            int quality = EffectiveQuality(machine);
            return CrusherBatch.CanFitFeed(total, item.identifier == "scrap_metal", quality);
        }
        return false;
    }

    private static void Crush(GameItem machine, GameGridInventory input, GameItem box, GameSlotInventory battery)
    {
        if (!CanStart(machine, input, box, battery)) return;

        Span<int> parts = stackalloc int[PowderBox.ComponentCount];
        long total = PowderBox.Read(box, parts);

        int limit = CrusherBatch.NightlyLimit(machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());
        int quality = EffectiveQuality(machine);
        var consumed = new List<(GameItem Item, int Count)>();
        int count = 0;
        foreach (var item in input.childItems)
        {
            if (!IsFeed(item)) continue;
            bool scrap = item.identifier == "scrap_metal";
            int used = 0;
            while (used < item.unitCount && count < limit)
            {
                if (!CrusherBatch.CanFitFeed(total, scrap, quality)) break;
                var batch = scrap ? CrusherBatch.Parts(true, 0) : CrusherBatch.Parts(false,
                    RNG.GetRandomIntExculsive(0, 100), RNG.GetRandomIntExculsive(-5, 6),
                    RNG.GetRandomIntExculsive(-1, 2), RNG.GetRandomIntExculsive(-CrusherBatch.MaxResidueShift, CrusherBatch.MaxResidueShift + 1));
                int residue = CrusherBatch.Residue(batch[4], quality);
                int size = 100 - batch[4] + residue;

                for (int i = 0; i < 4; i++) parts[i] += batch[i];
                parts[4] += residue;
                total += size;
                used++;
                count++;
            }
            if (used > 0) consumed.Add((item, used));
            if (used < item.unitCount || count == limit) break;
        }
        if (count == 0 || !MachineHelper.TryDrawCyclePower(machine, battery)) return;

        foreach (var (item, used) in consumed)
        {
            if (used == item.unitCount) item.Destroy();
            else item.SetUnitCount(item.unitCount - used);
        }
        PowderBox.WriteComposition(box, parts);
    }
}
