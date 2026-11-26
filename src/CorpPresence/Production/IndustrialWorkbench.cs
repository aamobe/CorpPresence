using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static class IndustrialWorkbench
{
    internal const string Dismantle = "dismantle_repair_filter";
    internal const int WashAlcoholUl = 15_000;
    private sealed record Recipe(string Key, string[] Solids, string[] Products, params SinteringIngredient[] Powders);
    private static readonly SinteringIngredient[] BlendPowders = {
        new(PowderComponent.Conductive, IndustrialRecipes.BlendConductive), new(PowderComponent.RareMetal, IndustrialRecipes.BlendRare)
    };
    private static readonly Recipe[] Recipes = {
        new(IndustrialGoods.Blend, new[] { ClassifierGoods.Fine }, new[] { IndustrialGoods.Blend }, BlendPowders),
        new(IndustrialGoods.Contact, new[] { IndustrialGoods.Blend }, Enumerable.Repeat(IndustrialGoods.Contact, IndustrialRecipes.ContactYield).ToArray()),
        new(IndustrialGoods.Liner, new[] { ClassifierGoods.Crystal }, new[] { IndustrialGoods.Liner }),
        new(IndustrialGoods.Unit, new[] { IndustrialGoods.Frame, IndustrialGoods.Blank, IndustrialGoods.Liner, CartelReactor.Gel },
            new[] { IndustrialGoods.Unit }),
        new(IndustrialGoods.Pad, new[] { ClassifierGoods.Coarse, IndustrialGoods.Alloy }, new[] { IndustrialGoods.Pad }),
        new(IndustrialGoods.CleanShell, new[] { IndustrialGoods.OldShell }, new[] { IndustrialGoods.CleanShell }),
        new(Dismantle, new[] { IndustrialGoods.Unit }, new[] { IndustrialGoods.CleanShell, IndustrialGoods.Blank, IndustrialGoods.Liner, CartelReactor.Gel }),
        new(IndustrialPump.Key, new[] { IndustrialGoods.Frame, IndustrialGoods.Contact }, new[] { IndustrialPump.Key }),
        new(CoreCalibration.Key, new[] { IndustrialGoods.Frame, CraftingStation.Substrate, CraftingStation.SiliconPlate }, new[] { CoreCalibration.Key }),
        new(FoldedFactory.Key, new[] { RodMaterial.PressureShell, RodMaterial.IndustrialCoil, RodMaterial.FeedShaft, MeridianCoreIntake.TimingCore }, new[] { FoldedFactory.Key }),
        new(PhaseRecoveryStation.Winch, new[] { IndustrialGoods.Frame, RodMaterial.FeedShaft }, new[] { PhaseRecoveryStation.Winch }),
        new(PhaseRecoveryStation.Drive, new[] { RodMaterial.AxialLead, RodMaterial.IndustrialCoil }, new[] { PhaseRecoveryStation.Drive }),
        new(PhaseRecoveryStation.Key, new[] { PhaseRecoveryStation.Winch, PhaseRecoveryStation.Drive, RodMaterial.PressureShell,
            MeridianCoreIntake.PhaseCore }, new[] { PhaseRecoveryStation.Key }),
        new(JointWaterWork.Kit, new[] { CraftingStation.SiliconPlate, IndustrialGoods.Contact, SleeveOiling.Lubricant }, new[] { JointWaterWork.Kit })
    };
    internal static readonly string[] BasicCycle = { ClassifierGoods.Screen, ClassifierGoods.Sleeve, IndustrialGoods.Blend,
        IndustrialGoods.Contact, IndustrialGoods.Liner, IndustrialGoods.Unit, IndustrialGoods.Pad, IndustrialGoods.CleanShell, Dismantle };
    internal static readonly string[] FormingCycle = { RodMaterial.ShortRod, RodMaterial.Strip, RodMaterial.WireBlank,
        RodMaterial.PorousBlank, RodCladding.Thin, RodCladding.Thick };
    internal static readonly string[] PartCycle = { RodMaterial.FeedShaft, RodMaterial.FeedSleeve, SleeveOiling.Recipe,
        RodMaterial.IndustrialCoil, RodMaterial.AxialLead, RodMaterial.ReturnGuide, RodMaterial.FilterBasket,
        RodMaterial.PressureShell, IndustrialPump.Key, CoreCalibration.Key, FoldedFactory.Key,
        PhaseRecoveryStation.Winch, PhaseRecoveryStation.Drive, PhaseRecoveryStation.Key, JointWaterWork.Kit,
        RodMaterial.WetContact, RodCladding.Recoat, RodMaterial.ToothRoller, CrusherToothRoller.Reface,
        RodMaterial.CastingDie, RodMaterial.ClassifierSupport, RodMaterial.GrowthGuide };

    internal static string[] CycleFor(string recipe) => Array.IndexOf(FormingCycle, recipe) >= 0 ? FormingCycle :
        Array.IndexOf(PartCycle, recipe) >= 0 ? PartCycle : BasicCycle;

    internal static bool Learned(string key) => key is IndustrialPump.Key or RodMaterial.PressureShell ? IndustrialPump.Learned()
        : CrusherToothRoller.IsRecipe(key) ? CrusherToothRoller.Learned
        : key is RodMaterial.CastingDie or RodMaterial.ClassifierSupport or RodMaterial.GrowthGuide ? RodCladding.Learned
        : RodCladding.IsRecipe(key) || key == RodMaterial.WetContact ? RodCladding.Learned
        : AlloyForms.IsRecipe(key) || key == SleeveOiling.Recipe
        ? LowerMachinist.MineralizerUnlocked(CorpState.Current) : key switch
    {
        IndustrialGoods.Blend or IndustrialGoods.Contact or ClassifierGoods.Screen or IndustrialGoods.Pad => LowerMachinist.MineralizerUnlocked(CorpState.Current),
        IndustrialGoods.Liner or IndustrialGoods.Unit or ClassifierGoods.Sleeve => CartelShizuka.BlueprintDelivered(CorpState.Current),
        IndustrialGoods.CleanShell => WorkshopSaleOffer.GelOrderSettled(CorpState.Current),
        CoreCalibration.Key or FoldedFactory.Key => WaterStationRepair.EngineeringCleared(CorpState.Current),
        PhaseRecoveryStation.Winch or PhaseRecoveryStation.Drive or PhaseRecoveryStation.Key or JointWaterWork.Kit =>
            WaterStationRepair.EngineeringCleared(CorpState.Current),
        Dismantle => CorpState.Current?.Workshop?.Rotations?.Any(c => c?.Kind == RotationKind.Repair) == true,
        _ => false
    };

    internal static bool IsIndustrial(string key) => key == SleeveOiling.Recipe || RodCladding.IsRecipe(key)
        || AlloyForms.IsRecipe(key) || Array.Exists(Recipes, r => r.Key == key);
    internal static bool Accept(string key, GameItem item)
    {
        if (key == SleeveOiling.Recipe) return SleeveOiling.Accept(item);
        if (RodCladding.IsRecipe(key)) return RodCladding.Accept(key, item);
        if (AlloyForms.IsRecipe(key)) return AlloyForms.Accept(key, item);
        var recipe = Array.Find(Recipes, r => r.Key == key);
        return recipe != null && item?.unitCount > 0 && GeneralHelper.IsItemOwned(item)
            && (SolidIndex(recipe, item) >= 0 || recipe.Powders.Length > 0 && PowderBox.IsOwnedBox(item));
    }

    private static int SolidIndex(Recipe r, GameItem item)
    {
        string key = Goods.IdOf(item);
        if (r.Key == FoldedFactory.Key && !FoldedFactory.AcceptMaterial(item, key)) return -1;
        if (r.Key == JointWaterWork.Kit && key == CraftingStation.SiliconPlate && !FoldedFactory.IsFoldedPlate(item)) return -1;
        if (!PhaseRecoveryStation.Qualifies(r.Key, key, item)) return -1;
        if (key is IndustrialGoods.OldShell or IndustrialGoods.CleanShell || r.Key == Dismantle)
        {
            if (!RepairReturns.Outstanding(item)) return -1;
            if (r.Key == IndustrialGoods.Unit && key == IndustrialGoods.CleanShell) key = IndustrialGoods.Frame;
        }
        return Array.IndexOf(r.Solids, key);
    }

    private static string Description(string key) => key switch
    {
        IndustrialGoods.Blend => L.K("workshop.industrial.makeBlend", "1 细砂＋20 导电＋4 稀有（粉盒各 ≥81%）→ 1 复合粉",
            "1 fine grit + 20 conductive + 4 rare (each box ≥81%) → 1 conductive blend"),
        IndustrialGoods.Contact => L.K("workshop.industrial.makeContacts", "1 复合粉 → 2 触片", "1 conductive blend → 2 pump contacts"),
        IndustrialGoods.Liner => L.K("workshop.industrial.makeLiner", "1 光盐晶粉 → 1 工业衬层", "1 crystal powder → 1 filter liner"),
        IndustrialGoods.Pad => L.K("workshop.industrial.makePad", "1 粗砂＋1 耐磨合金粉 → 1 重载筛垫", "1 coarse grit + 1 wear alloy powder → 1 heavy screen pad"),
        IndustrialGoods.CleanShell => L.K("workshop.industrial.washHousing", "1 旧滤壳＋15 mL 酒精 → 1 同单净滤壳", "1 returned housing + 15 mL alcohol → 1 clean housing for the same repair"),
        IndustrialPump.Key => L.K("workshop.pump.craft", "1 工业框架＋1 泵用触片 → 1 工业试水泵。用螺丝刀打开机身装配。", "1 industrial frame + 1 pump contact → 1 industrial test pump. Open the body with a screwdriver to fit the parts."),
        CoreCalibration.Key => L.K("workshop.core.craft", "1 工业框架＋1 电子基材＋1 硅板 → 1 核心校准台", "1 industrial frame + 1 electronic substrate + 1 silicon plate → 1 core calibration bench"),
        FoldedFactory.Key => L.K("workshop.fold.craft", "承压壳、工业线圈、耐疲劳轴及已校时序核心各 1 件", "Use one pressure shell, industrial coil, fatigue-rated shaft and verified timing core"),
        PhaseRecoveryStation.Winch => L.K("workshop.phase.winchRecipe", "1 工业框架＋1 耐疲劳轴 → 1 重载绞盘", "1 industrial frame + 1 fatigue-rated shaft → 1 heavy winch"),
        PhaseRecoveryStation.Drive => L.K("workshop.phase.driveRecipe", "1 定向引线＋1 工业线圈 → 1 相位导电总成", "1 axial lead + 1 industrial coil → 1 phase conductor assembly"),
        PhaseRecoveryStation.Key => L.K("workshop.phase.recipe", "重载绞盘、导电总成、耐蚀 ≥45 的承压壳、已校相位核心各 1 件", "Use a heavy winch, conductor assembly, pressure shell with corrosion resistance ≥45, and verified phase core"),
        JointWaterWork.Kit => L.K("workshop.joint.recipe", "1 折叠硅板＋1 泵用触片＋1 润滑料 → 1 水站复工备件", "1 folded silicon plate + 1 pump contact + 1 lubricant → 1 water station service kit"),
        Dismantle => L.K("workshop.industrial.dismantleFilter", "拆回本单净壳、滤芯坯、衬层、眠苔胶各一件", "Recover the tagged housing and one filter blank, liner and gel"),
        IndustrialGoods.Unit when Learned(Dismantle) => L.K("workshop.industrial.makeRepairUnit", "1 新框架或本单净壳＋1 滤芯坯＋1 衬层＋1 眠苔胶 → 1 滤芯总成", "1 new frame or tagged clean housing + 1 blank + 1 liner + 1 gel → 1 filter assembly"),
        _ => L.K("workshop.industrial.makeUnit", "1 新框架＋1 滤芯坯＋1 衬层＋1 眠苔胶 → 1 全新滤芯总成",
            "1 new frame + 1 blank + 1 liner + 1 gel → 1 new filter assembly")
    };

    private static string Plan(Recipe r, GameSlotInventory[] slots, GameItem liquid, GameGridInventory output, out GameItem[] solids, out SinteringBatch powder, bool checkOutput = true)
    {
        powder = null;
        solids = new GameItem[r.Solids.Length];
        var boxes = new int[slots.Length][];
        for (int i = 0; i < slots.Length; i++)
        {
            boxes[i] = new int[PowderBox.ComponentCount];
            var item = slots[i].childItem;
            if (item == null || item.unitCount < 1 || !GeneralHelper.IsItemOwned(item)) continue;
            int index = SolidIndex(r, item);
            if (index >= 0) solids[index] ??= item;
            else if (r.Powders.Length > 0 && PowderBox.IsOwnedBox(item) && PowderBox.Read(item, boxes[i]) > PowderColor.Capacity) return "feed";
        }
        if (Array.Exists(solids, item => item == null)) return "feed";
        powder = SinteringBatch.Plan(boxes, r.Powders, null, exact: true);
        if (powder == null) return "feed";
        if (r.Key == IndustrialGoods.CleanShell && AlcoholPreparationStation.AlcoholUlAvailable(liquid) < WashAlcoholUl) return "alcohol";
        // Mixed-output recipes contain one-cell products; single-product recipes reserve the product's shape.
        return !checkOutput || MachineAppearance.CanFit(output, "corp_presence_" + r.Products[0], r.Products.Length, separateUnit: true) ? "ready" : "full";
    }

    internal static string Status(string key, GameSlotInventory[] slots, GameItem liquid, GameGridInventory output)
    {
        if (key == SleeveOiling.Recipe) return SleeveOiling.Status(slots);
        if (RodCladding.IsRecipe(key)) return RodCladding.Status(key, slots, output);
        if (AlloyForms.IsRecipe(key)) return AlloyForms.Status(key, slots, output);
        var recipe = Array.Find(Recipes, r => r.Key == key);
        string state = Plan(recipe, slots, liquid, output, out var solids, out _);
        string origin = IndustrialGoods.TryReadOrigin(solids[0], out _, out int serial, out int slot)
            ? "\n" + L.F("workshop.rotation.origin","返修合同 #{0} · 壳位 {1}","Repair contract #{0} · shell {1}",serial,slot+1)
            : "";
        return Description(key) + origin + "\n" + (state switch
        {
            "ready" => L.K("workshop.mill.workReady", "可以装配", "Ready to assemble"),
            "full" => MineralNightReport.Reason(MineralStop.Full),
            "alcohol" => L.K("workshop.industrial.needAlcohol", "在清洗液槽放入含至少 15 mL 酒精的普通水容器", "Insert a regular water container with at least 15 mL alcohol in the wash slot"),
            _ => L.K("workshop.industrial.needMaterials", "按配方放入自己的材料与合格粉盒", "Insert the required owned materials and qualifying powder boxes")
        });
    }

    internal static void Craft(string key, GameSlotInventory[] slots, GameSlotInventory liquidSlot, GameGridInventory output)
    {
        if (RodCladding.IsRecipe(key))
        {
            if (Learned(key)) RodCladding.Craft(key, slots, output);
            return;
        }
        if (key == SleeveOiling.Recipe)
        {
            if (Learned(key)) SleeveOiling.Apply(slots);
            return;
        }
        if (AlloyForms.IsRecipe(key))
        {
            if (Learned(key)) AlloyForms.Craft(key, slots, output);
            return;
        }
        var r = Array.Find(Recipes, recipe => recipe.Key == key);
        var liquid = liquidSlot.childItem;
        if (!Learned(key) || r == null || Plan(r, slots, liquid, output, out var solids, out var powder) != "ready") return;
        var session = StoreSession.Capture();
        var sources = slots.Select(slot => slot.childItem).ToArray();
        var products = new List<GameItem>(r.Products.Length);
        bool tagged = IndustrialGoods.TryReadOrigin(solids[0], out var run, out int serial, out int slot);
        bool committed = false;
        try
        {
            foreach (string id in r.Products)
            {
                var product = DirectoryMaster.Item("corp_presence_" + id, isOwned: true);
                if (product == null) return;
                products.Add(product);
                if (tagged && id is IndustrialGoods.Unit or IndustrialGoods.CleanShell)
                    IndustrialGoods.MarkOrigin(product, run, serial, slot);
                else if (id == IndustrialGoods.Unit) IndustrialGoods.MarkFresh(product);
            }
            if (!ProductionOutput.TryAddSeparateBatch(output, products)) return;
            // Native output callbacks may move a feed or change a powder box during placement.
            if (!session.IsCurrent || !Learned(key) || products.Any(item => item.parentInventory != output
                || item.unitCount != 1 || !output.childItems.Contains(item) || StorePlacement.Destroyed(item))
                || !sources.SequenceEqual(slots.Select(slot => slot.childItem))
                || (key == IndustrialGoods.CleanShell && liquid != liquidSlot.childItem)
                || Plan(r, slots, liquid, output, out solids, out powder, checkOutput: false) != "ready") return;
            if (tagged && (!IndustrialGoods.TryReadOrigin(solids[0], out var nowRun, out int nowSerial, out int nowSlot)
                || run != nowRun || serial != nowSerial || slot != nowSlot)) return;
            if (key == IndustrialGoods.CleanShell)
                AlcoholPreparationStation.CommitAlcohol(liquid, AlcoholPreparationStation.AlcoholUlAvailable(liquid) - WashAlcoholUl);
            foreach (var item in solids) item.unitCount--;
            for (int i = 0; i < slots.Length; i++)
                if (powder.Used[i]) PowderBox.CommitComposition(slots[i].childItem, powder.Remaining[i]);
            committed = true;
            if (key == IndustrialGoods.CleanShell) HookGuard.Run("工艺台清洗液刷新", () => AlcoholPreparationStation.RefreshLiquid(liquid));
            foreach (var item in solids) HookGuard.Run("工业工艺材料刷新", () =>
            {
                if (item.unitCount == 0) Discard(item);
                else item.SetUnitCount(item.unitCount);
            });
            for (int i = 0; i < slots.Length; i++)
                if (powder.Used[i]) HookGuard.Run("工业工艺粉盒刷新", () => PowderBox.RefreshComposition(slots[i].childItem, powder.Remaining[i]));
        }
        finally
        {
            if (!committed)
                foreach (var item in products) HookGuard.Run("撤回工艺台成品", () => Discard(item));
        }
    }

    // GameItemElement.Destroy marks the item destroyed before Expel. Detach a spent/staged
    // item first: native save enumerates the inventory graph regardless of the destroyed flag.
    internal static void Discard(GameItem item)
    {
        var source = item.parentInventory;
        if (source != null)
        {
            HookGuard.Run("工艺台移出实物", () => source.Expel(item));
            var attached = item.parentInventory;
            if (attached != null)
            {
                // Slot and grid Expel invoke these two notifications before changing either graph edge.
                // If interrupted there, finish removal of this item with the native graph/render cleanup.
                var itemCallback = item.onSlotRemoveItemFunc;
                var slotCallback = attached.onSlotRemoveItemFunc;
                try
                {
                    item.onSlotRemoveItemFunc = null;
                    attached.onSlotRemoveItemFunc = null;
                    HookGuard.Run("工艺台恢复移出", () => attached.Expel(item));
                }
                finally
                {
                    item.onSlotRemoveItemFunc = itemCallback;
                    attached.onSlotRemoveItemFunc = slotCallback;
                }
            }
            if (item.parentInventory != null || source.childItems.Contains(item) || attached?.childItems.Contains(item) == true)
                throw new InvalidOperationException("工艺台实物仍挂在库存图中");
        }
        item.Destroy();
    }
}
