using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static partial class MineralCrusher
{
    internal const string Key = "mineral_crusher";
    internal const string FeederKey = "cen_feeder";
    private const string PreserveResidueTag = "CORP_CRUSHER_PRESERVE_RESIDUE";
    internal static readonly Goods.Def Definition = new()
    {
        Name = "矿粉破碎机", NameEn = "Mineral Crusher", Value = 200,
        Company = "mineral", Art = "mineral-crusher", Fallback = "furnace",
        W = 3, H = 3, Types = new[] { "MACHINE" }, Shell = () => Build(feeder: false)
    };

    internal static readonly Goods.Def FeederDefinition = new()
    {
        Name = "阿岑的旧给料机", NameEn = "Cen's Old Feeder", Value = 220,
        Company = "mineral", Art = "cen-feeder", Fallback = "furnace",
        W = 3, H = 3, Types = new[] { "MACHINE" }, Shell = () => Build(feeder: true)
    };

    private static bool PreserveResidue(GameItem machine) => machine.GetTagReadonly(PreserveResidueTag).GetBool();

    private static int EffectiveQuality(GameItem machine) =>
        CrusherBatch.EffectiveQuality(machine.GetTagReadonly("TOTAL_PERCENTAGE_QUALITY_BONUS_INT").GetInt(), PreserveResidue(machine));

    private static GameItem Build(bool feeder)
    {
        var input = new GameGridInventory(8, 8) { mayInventoryAddItemFunc = (item, _) =>
            IsFeed(item) && (!feeder || CrusherBatch.Classify(item.identifier) != CrusherFeed.AlloyBillet) };
        input.onSlotAddItemEndFunc += QueueFeed;
        input.onSlotRemoveItemEndFunc += DequeueFeed;
        var receiver = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => PowderBox.IsOwnedBox(item) };
        var battery = new GameSlotInventory();
        var modules = MineralModuleBay.Create();
        var output = new GameGridInventory(9, 6) { mayInventoryAddItemFunc = (_, _) => false };
        var hard = feeder ? null : new HardDuty();
        var status = new MachineryText(uppercase: false);
        GameItem machine = null;
        var qualitySwitch = new MachineryButton();
        var close = new MachineryButton(L.T("合上检修盖", "Close cover"));
        var forecast = new MachineryButton(L.K("workshop.crusher.forecast", "查看来料签", "Feed forecast"));
        var reportButton = MineralNightReport.Button(() => machine, MineralNightReport.Crusher);
        var control = feeder ? MachineryStack.Vertical(status, qualitySwitch, forecast, reportButton, close)
            : MachineryStack.Vertical(status, qualitySwitch, forecast, reportButton, hard.Toggle, close);
        var panels = new[]
        {
            MachineHelper.Panel(L.K("workshop.crusher.input", "破碎来料", "Crusher feed"), input, "input"),
            MachineHelper.Panel(L.T("接粉盒", "Powder box"), receiver, "receiver"),
            MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.T("模组", "Modules"), modules, "module"),
            MachineHelper.Panel(L.T("过夜加工", "Overnight processing"), control, "control"),
            MachineHelper.Panel(L.K("production.sintering.output", "出料", "Output"), output, "output")
        };
        if (feeder)
        {
            var shaft = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (item, _) => MachineService.IsStockPart(item, RodMaterial.FeedShaft) };
            var sleeve = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (item, _) => MachineService.IsStockPart(item, RodMaterial.FeedSleeve) };
            var shaftPanel = MachineHelper.Panel(L.T("传动 · 给料轴", "Drive · Feed shaft"), shaft, "shaft").SetHidden(true);
            var sleevePanel = MachineHelper.Panel(L.T("支承 · 轴套", "Support · Sleeve"), sleeve, "sleeve").SetHidden(true);
            Array.Resize(ref panels, panels.Length + 2);
            panels[panels.Length - 2] = shaftPanel;
            panels[panels.Length - 1] = sleevePanel;
            machine = ItemDirectory.CreateEmptyItem().SetContentWindow(MachineHelper.CreateMachineryWindow(true, panels));
            FeederAssembly.Register(machine, shaft, sleeve, visible =>
            {
                shaftPanel.SetHidden(!visible);
                sleevePanel.SetHidden(!visible);
            }, () => input.childItems.Count == 0 && receiver.childItem == null && battery.childItem == null
                && modules.childItems.Count == 0 && output.childItems.Count == 0 && shaft.childItem == null && sleeve.childItem == null);
            close.SetCallback(() => MachineService.Close(machine));
            shaft.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            shaft.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
            sleeve.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            sleeve.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        }
        else
        {
            var roller = MachineHelper.Panel(Goods.NameOf(RodMaterial.ToothRoller), hard.Slot, "hard_roller").SetHidden(true);
            Array.Resize(ref panels, panels.Length + 1);
            panels[panels.Length - 1] = roller;
            machine = ItemDirectory.CreateEmptyItem().SetContentWindow(MachineHelper.CreateMachineryWindow(true, panels));
            hard.Register(machine, visible => roller.SetHidden(!visible), Refresh);
            close.SetCallback(() => MachineService.Close(machine));
        }
        var window = machine.contentWindow;
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, feeder ? FeederAssembly.BaseCharge : 12);
        machine.ModifyTag("BASE_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(300));
        machine.ModifyTag("CURRENT_PROCESSING_SPEED_TAG", tag => tag.Enable().SetInt(300));

        void Refresh()
        {
            int limit = feeder ? FeederAssembly.BatchSize : CrusherBatch.NightlyLimit(machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());
            int power = MachineryHelper.GetMachinePowerUsage(machine);
            int quality = EffectiveQuality(machine);
            qualitySwitch.SetText(PreserveResidue(machine)
                ? L.K("production.crusher.qualityOff", "高质量破碎 · 关", "High-quality crushing · Off")
                : L.K("production.crusher.qualityOn", "高质量破碎 · 开", "High-quality crushing · On"));
            string description = feeder
                ? L.T($"每夜基础批 {limit} 件\n耗电 {power}\n渣料减量 {quality}%",
                    $"Base throughput {limit} items/night\n{power} charge\n{quality}% less residue")
                : L.T($"每夜最多 {limit} 件\n每夜耗电 {power}\n渣料减量 {quality}%",
                    $"Up to {limit} items/night\n{power} charge/night\n{quality}% less residue");
            if (feeder)
            {
                var installed = FeederAssembly.SleeveForSecondBatch(machine);
                if (installed == null) description += "\n" + L.T("加批需要耐疲劳轴和含油套", "A second batch needs a fatigue-rated shaft and an oiled sleeve");
                else if (SecondBatch(machine, input, receiver.childItem, battery, power, quality) == null)
                    description += "\n" + L.T("装配合格 · 检查来料、接粉盒容量和电量", "Parts qualified · check feed, powder box space and charge");
                else description += "\n" + L.T($"可跑第二批 · 合计 {power + FeederAssembly.ExtraCharge} 电及 1 份套内油",
                    $"Second batch ready · {power + FeederAssembly.ExtraCharge} charge total and one sleeve oil portion");
            }
            else if (hard.Status(power) is { Length: > 0 } hardStatus)
            {
                description += "\n" + hardStatus;
                if (hard.Selected && hard.Capacity > 0)
                    description += "\n" + MineralNightReport.Reason(Condition(machine, input, receiver.childItem, battery, output, hard));
            }
            status.SetText(description);
        }

        forecast.SetCallback(() =>
        {
            var ui = GenericConfirmationUIManager.Instance;
            int quality = EffectiveQuality(machine);
            string Label(CrusherFeed feed) => feed switch
            {
                CrusherFeed.Junk => LocHelper.GetLocalizedItem("item_junk_name"),
                CrusherFeed.Scrap => LocHelper.GetLocalizedItem("item_scrap_metal_name"),
                CrusherFeed.Glass => LocHelper.GetLocalizedItem("item_glass_shard_name"),
                CrusherFeed.SalvageElectronics => Goods.NameOf("salvage_electronics"),
                CrusherFeed.SpentContact => Goods.NameOf(IndustrialRecipes.SpentContact),
                _ => Goods.NameOf("spent_lining")
            };
            string report = MineralNightReport.Order;
            foreach (var feed in new[] { CrusherFeed.Junk, CrusherFeed.Scrap, CrusherFeed.Glass, CrusherFeed.SalvageElectronics, CrusherFeed.SpentLining, CrusherFeed.SpentContact })
            {
                var range = CrusherBatch.Range(feed, quality);
                var values = new string[range.Min.Length];
                for (int i = 0; i < values.Length; i++)
                    values[i] = range.Min[i] == range.Max[i] ? range.Min[i].ToString() : $"{range.Min[i]}–{range.Max[i]}";
                report += "\n" + Label(feed) + "  " + string.Join("/", values);
            }
            if (hard?.Capacity > 0)
            {
                var parts = CrusherBatch.HardLiningParts.ToArray();
                parts[4] = CrusherBatch.Residue(parts[4], quality);
                report += "\n" + L.F("workshop.crusher.hardForecast", "硬料档旧炉衬 {0}", "Lining in hard duty {0}", MineralNightReport.Vector(parts));
            }
            if (!feeder && LowerMachinist.MineralizerUnlocked(CorpState.Current))
                report += "\n" + Goods.NameOf(IndustrialRecipes.Billet) + " → " + Goods.NameOf(IndustrialRecipes.Alloy) + " × " + IndustrialRecipes.AlloyYield;
            report += "\n" + L.K("workshop.crusher.offcutForecast",
                "合金边料：每种成分按 80% 回粉并向下取整。无整点产出的边料不能投放。",
                "Alloy offcuts: Recover 80% of each component, rounded down. Pieces yielding no whole points cannot be loaded.");
            ui?.OpenPanelSingleButton(report, L.K("production.canning.buildCanner.oK", "确定", "OK"), () => ui.ClosePanel());
        });
        qualitySwitch.SetCallback(() => HookGuard.Run("切换破碎质量", () =>
        {
            if (!GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence")) return;
            bool preserveResidue = !PreserveResidue(machine);
            machine.ModifyTag(PreserveResidueTag, tag => tag.Enable().SetBool(preserveResidue));
            Refresh();
        }));
        MachineHelper.SetupModuleBay(modules, machine, Refresh);
        window.openCallback += () => MachineService.OpenWindow(machine);
        window.closeCallback += () => MachineService.Close(machine);
        foreach (var inventory in new GameInventory[] { input, receiver, battery })
        {
            inventory.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        }
        window.openCallback += Refresh;
        Refresh();
        FoldedFactory.RegisterCrusher(machine, receiver, () =>
        {
            if (!GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence")) return;
            ModuleHelper.ComputeModuleEffect(modules, null, machine);
            ModuleHelper.ApplyBasicModuleEffect(modules, null, machine);
            Crush(machine, input, receiver.childItem, battery, output, feeder, hard);
            Refresh();
        });
        MachineAppearance.WatchCycle(machine, () => Condition(machine, input, receiver.childItem, battery, output, hard) == MineralStop.Ready);
        return machine;
    }

    private static MineralStop Condition(GameItem machine, GameGridInventory input, GameItem box, GameSlotInventory battery,
        GameGridInventory output, HardDuty hard)
    {
        if (!GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence")) return MineralStop.Locked;
        if (hard?.Settled == true) return MineralStop.CrusherSettled;
        bool grinding = hard?.Selected == true;
        if (grinding && hard.Capacity == 0) return MineralStop.CrusherTeeth;
        int charge = MachineryHelper.GetMachinePowerUsage(machine);
        if (!machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
            || !(grinding ? WorkshopCycle.CanDrawPower(battery, charge + HardDuty.ExtraCharge)
                : MachineHelper.CanPower(machine, battery))) return MineralStop.NoPower;

        foreach (var item in NightFeed(input, grinding))
        {
            var feed = CrusherBatch.Classify(item.identifier);
            if (feed == CrusherFeed.AlloyBillet)
                return !LowerMachinist.MineralizerUnlocked(CorpState.Current) ? MineralStop.Locked :
                    MachineAppearance.CanFit(output, "corp_presence_" + IndustrialRecipes.Alloy, IndustrialRecipes.AlloyYield, true)
                        ? MineralStop.Ready : MineralStop.Full;
            if (!PowderBox.IsOwnedBox(box)) return MineralStop.NoBox;
            Span<int> parts = stackalloc int[PowderBox.ComponentCount];
            long total = PowderBox.Read(box, parts);
            int quality = EffectiveQuality(machine);
            int maximum = grinding ? CrusherBatch.HardLiningMaximum(quality) : MaximumPowder(item, feed, quality);
            return maximum > 0 && CrusherBatch.Fits(total, maximum) ? MineralStop.Ready : MineralStop.Full;
        }
        return grinding ? MineralStop.CrusherLining : MineralStop.NoFeed;
    }

    private static GameItem SecondBatch(GameItem machine, GameGridInventory input, GameItem box, GameSlotInventory battery,
        int normalCharge, int quality)
    {
        var oilySleeve = FeederAssembly.SleeveForSecondBatch(machine);
        if (oilySleeve == null || !PowderBox.IsOwnedBox(box)
            || !WorkshopCycle.CanDrawPower(battery, normalCharge + FeederAssembly.ExtraCharge)) return null;
        Span<int> stock = stackalloc int[PowderBox.ComponentCount];
        long total = PowderBox.Read(box, stock);
        int remaining = 2 * FeederAssembly.BatchSize;
        foreach (var item in FeedInOrder(input))
        {
            int size = MaximumPowder(item, CrusherBatch.Classify(item.identifier), quality);
            if (size <= 0) return null;
            int count = Math.Min(remaining, item.unitCount);
            total += size * count;
            if (total > PowderColor.Capacity) return null;
            remaining -= count;
            if (remaining == 0) return oilySleeve;
        }
        return null;
    }

    private static void Crush(GameItem machine, GameGridInventory input, GameItem box, GameSlotInventory battery,
        GameGridInventory output, bool feeder, HardDuty hard)
    {
        var condition = Condition(machine, input, box, battery, output, hard);
        if (condition == MineralStop.CrusherSettled) return;
        var roller = hard?.Selected == true ? hard.Fitted : null;
        bool grinding = roller != null;
        if (condition != MineralStop.Ready)
        {
            MineralNightReport.Save(machine, MineralNightReport.Crusher, condition, recipe: grinding ? HardLiningReceipt : "");
            return;
        }

        Span<int> parts = stackalloc int[PowderBox.ComponentCount];
        long total = PowderBox.IsOwnedBox(box) ? PowderBox.Read(box, parts) : 0;

        int baseLimit = feeder ? FeederAssembly.BatchSize : CrusherBatch.NightlyLimit(machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG").GetInt());
        int quality = EffectiveQuality(machine);
        int normalCharge = MachineryHelper.GetMachinePowerUsage(machine);
        var oilySleeve = feeder ? SecondBatch(machine, input, box, battery, normalCharge, quality) : null;
        int limit = grinding ? Math.Min(baseLimit, roller.Capacity) : oilySleeve == null ? baseLimit : 2 * baseLimit;
        var consumed = new List<(GameItem Item, int Count)>();
        int count = 0;
        var counts = new int[(int)CrusherFeed.AlloyOffcut];
        var products = new List<GameItem>();
        var produced = new int[PowderBox.ComponentCount];
        bool committed = false;
        try
        {
            foreach (var item in NightFeed(input, grinding))
            {
                var feed = CrusherBatch.Classify(item.identifier);
                var offcut = feed == CrusherFeed.AlloyOffcut ? OffcutParts(item) : null;
                int maximum = grinding ? CrusherBatch.HardLiningMaximum(quality) : offcut == null ? CrusherBatch.MaximumPowder(feed, quality)
                    : PowderCount(offcut, CrusherBatch.Residue(offcut[4], quality));
                int used = 0;
                while (used < item.unitCount && count < limit)
                {
                    if (feed == CrusherFeed.AlloyBillet)
                    {
                        if (!LowerMachinist.MineralizerUnlocked(CorpState.Current)) { condition = MineralStop.Locked; break; }
                        if (!StageAlloy(output, products)) { condition = MineralStop.Full; break; }
                        counts[(int)feed - 1]++;
                        used++;
                        count++;
                        continue;
                    }
                    if (!PowderBox.IsOwnedBox(box)) { condition = MineralStop.NoBox; break; }
                    if (maximum <= 0 || !CrusherBatch.Fits(total, maximum))
                    {
                        condition = MineralStop.Full;
                        break;
                    }
                    ReadOnlySpan<int> batch = grinding ? CrusherBatch.HardLiningParts : offcut;
                    if (!grinding && offcut == null)
                        batch = feed != CrusherFeed.Junk ? CrusherBatch.Parts(feed, 0) : CrusherBatch.Parts(feed,
                            RNG.GetRandomIntExculsive(0, 100), RNG.GetRandomIntExculsive(-5, 6),
                            RNG.GetRandomIntExculsive(-1, 2), RNG.GetRandomIntExculsive(-CrusherBatch.MaxResidueShift, CrusherBatch.MaxResidueShift + 1));
                    int residue = CrusherBatch.Residue(batch[4], quality);
                    int size = PowderCount(batch, residue);

                    for (int i = 0; i < 4; i++) produced[i] += batch[i];
                    produced[4] += residue;
                    counts[(int)feed - 1]++;
                    total += size;
                    used++;
                    count++;
                }
                if (used > 0) consumed.Add((item, used));
                if (used < item.unitCount || count == limit) break;
            }
            bool second = oilySleeve != null && count > baseLimit;
            int usedCharge = normalCharge + (grinding ? HardDuty.ExtraCharge : second ? FeederAssembly.ExtraCharge : 0);
            if (count == 0 || !(second || grinding ? WorkshopCycle.DrawPower(battery, usedCharge) : WorkshopCycle.DrawPower(machine, battery)))
            {
                MineralNightReport.Save(machine, MineralNightReport.Crusher, count == 0 ? condition : MineralStop.NoPower);
                return;
            }

            foreach (var (item, used) in consumed) item.unitCount -= used;
            if (second) SleeveOiling.Consume(oilySleeve);
            if (grinding) hard.Finish(roller, count);
            bool powderChanged = Array.Exists(produced, value => value != 0);
            if (powderChanged)
            {
                for (int i = 0; i < produced.Length; i++) parts[i] += produced[i];
                PowderBox.CommitComposition(box, parts);
            }
            committed = true;
            if (feeder) CenFeederTrial.Record(machine, count);
            foreach (var (item, _) in consumed)
                HookGuard.Run("破碎进料刷新", () =>
                {
                    if (item.unitCount == 0) item.Destroy();
                    else item.SetUnitCount(item.unitCount);
                });
            if (powderChanged) PowderBox.RefreshComposition(box, parts);
            MineralNightReport.Save(machine, MineralNightReport.Crusher, condition, count,
                usedCharge, counts, produced, recipe: grinding ? HardLiningReceipt : products.Count.ToString());
        }
        finally { if (!committed) ProductionOutput.DiscardBatch(products); }
    }

    private static bool StageAlloy(GameGridInventory output, List<GameItem> products)
    {
        string id = "corp_presence_" + IndustrialRecipes.Alloy;
        if (!MachineAppearance.CanFit(output, id, IndustrialRecipes.AlloyYield, separateUnit: true)) return false;
        var batch = new List<GameItem>(IndustrialRecipes.AlloyYield);
        bool staged = false;
        try
        {
            for (int i = 0; i < IndustrialRecipes.AlloyYield; i++)
            {
                var product = DirectoryMaster.Item(id, isOwned: true);
                if (product == null) return false;
                batch.Add(product);
            }
            if (!ProductionOutput.TryAddSeparateBatch(output, batch)) return false;
            products.AddRange(batch);
            return staged = true;
        }
        finally { if (!staged) ProductionOutput.DiscardBatch(batch); }
    }
}
