using System.Collections.Generic;

namespace CorpPresence;

internal static class RodCladding
{
    internal const string Thin = RodMaterial.CladRod;
    internal const string Thick = "clad_alloy_rod_thick";
    internal const string Recoat = "recoat_wet_pump_contact";

    private sealed record Work(GameSlotInventory CoreSlot, GameSlotInventory WrapSlot, GameItem Core,
        GameItem Wrap, RodCondition CoreStock, RodCondition WrapStock, RodCondition Rod, RodCondition Offcut);

    internal static bool IsRecipe(string key) => key is Thin or Thick or Recoat || CrusherToothRoller.IsRecipe(key);
    internal static bool Learned => MachineService.Handbook && LowerMachinist.MineralizerUnlocked(CorpState.Current);

    private static string Product(string key) => key switch
    {
        Recoat => RodMaterial.WetContact,
        RodMaterial.ToothRoller or CrusherToothRoller.Reface => RodMaterial.ToothRoller,
        _ => RodMaterial.CladRod
    };

    private static bool IsCore(string key, GameItem item) => key switch
    {
        Recoat => item.identifier == "corp_presence_" + RodMaterial.WetContact,
        CrusherToothRoller.Reface => item.identifier == "corp_presence_" + RodMaterial.ToothRoller,
        RodMaterial.ToothRoller => RodMaterial.IsShortRod(item.identifier) && CrusherToothRoller.SturdyHub(RodCondition.Read(item)),
        _ => RodMaterial.IsRod(item.identifier) && item.identifier != "corp_presence_" + RodMaterial.CladRod
    };

    internal static bool Accept(string key, GameItem item)
    {
        if (item?.unitCount != 1 || !GeneralHelper.IsItemOwned(item) || StorePlacement.Destroyed(item)) return false;
        bool core = IsCore(key, item);
        if (RodCondition.Read(item) is not { Jacket: null } stock) return false;
        if (CrusherToothRoller.IsRecipe(key))
            return stock.Material.PowderPoints is >= 4 and <= PowderColor.Capacity && (core
                ? CrusherToothRoller.SturdyHub(stock)
                : RodMaterial.IsShortRod(item.identifier) && CrusherToothRoller.HardFace(stock));
        if (!core && item.identifier != "corp_presence_" + RodMaterial.Strip) return false;
        int minimum = core ? 18 : 4;
        if (key == Recoat) minimum = core ? 2 : 5;
        return stock.Material.PowderPoints >= minimum && stock.Material.PowderPoints <= PowderColor.Capacity;
    }

    private static Work Plan(string key, GameSlotInventory[] slots)
    {
        GameSlotInventory coreSlot = null, wrapSlot = null;
        foreach (var slot in slots)
        {
            var item = slot.childItem;
            if (!Accept(key, item)) continue;
            if (IsCore(key, item)) coreSlot ??= slot;
            else wrapSlot ??= slot;
        }
        if (coreSlot == null || wrapSlot == null) return null;
        var core = coreSlot.childItem;
        var wrap = wrapSlot.childItem;
        var coreStock = RodCondition.Read(core);
        var wrapStock = RodCondition.Read(wrap);
        var jacketMass = AlloyForms.TakePercent(wrapStock.Material, key switch
        {
            Thick or RodMaterial.ToothRoller or CrusherToothRoller.Reface => 50,
            Recoat => 10,
            _ => 25
        });
        if (coreStock.Material.PowderPoints + jacketMass.PowderPoints > PowderColor.Capacity) return null;
        var rod = RodCondition.WithJacket(coreStock, new(jacketMass, wrapStock.Assay, wrapStock.Process));
        if (key == Recoat && !WetPumpContact.Qualified(rod)) return null;
        var offcut = RodCondition.Evaluate(wrapStock.Material.TakeAway(jacketMass), wrapStock.Process, wrapStock.Assay);
        return new(coreSlot, wrapSlot, core, wrap, coreStock, wrapStock, rod, offcut);
    }

    private static bool Fits(string key, GameGridInventory output) =>
        MachineAppearance.CanFit(output, "corp_presence_" + Product(key), separateUnit: true)
        && MachineAppearance.CanFit(output, "corp_presence_" + RodMaterial.Offcut, key == Recoat ? 2 : 3, separateUnit: true);

    internal static string Status(string key, GameSlotInventory[] slots, GameGridInventory output)
    {
        string title = key switch
        {
            Thick => L.K("workshop.clad.thick", "厚套复合棒 · 用半条轧制带", "Thick-jacket rod · half a rolled strip"),
            Recoat => L.K("workshop.clad.recoat", "触点换套 · 用一成轧制带", "Rejacket contact · one tenth of a rolled strip"),
            RodMaterial.ToothRoller => L.K("workshop.crusher.makeRoller", "装组合齿辊 · 回火抗冲击短棒和半根硬化齿料",
                "Assemble roller · one impact-rated segment and half a case-hardened segment"),
            CrusherToothRoller.Reface => L.K("workshop.crusher.refaceRoller", "齿辊换面 · 裸辊和半根硬化齿料",
                "Reface roller · worn hub and half a case-hardened segment"),
            _ => L.K("workshop.clad.thin", "薄套复合棒 · 用四分之一条轧制带", "Thin-jacket rod · one quarter of a rolled strip")
        };
        var plan = Plan(key, slots);
        if (plan == null) return title + "\n" + (key switch
        {
            Recoat => L.K("workshop.clad.recoatInputs", "须有磨穿的触点与耐蚀 ≥45、孔隙 ≤3 的板带。", "Load a worn-through contact and a strip rated ≥45 for corrosion and ≤3 for porosity."),
            RodMaterial.ToothRoller or CrusherToothRoller.Reface => L.K("workshop.crusher.rollerInputs",
                "齿料须经表面硬化，耐磨 ≥70、硬度 ≥80。剩余半根退作边料。",
                "The toothed face requires case hardening, wear resistance ≥70 and hardness ≥80. The rest returns as an offcut."),
            _ => L.K("workshop.clad.inputs", "放入整棒与轧制合金带。", "Load a whole rod and a rolled alloy strip.")
        });
        return title + "\n" + L.F("workshop.clad.preview", "芯 {0:0.#} 点 · 套 {1:0.#} 点 · 边料 {2:0.#} 点",
            "Core {0:0.#} points · jacket {1:0.#} points · offcut {2:0.#} points",
            plan.CoreStock.Material.PowderPoints, plan.Rod.Jacket.Material.PowderPoints, plan.Offcut.Material.PowderPoints)
            + (Fits(key, output) ? "" : "\n" + MineralNightReport.Reason(MineralStop.Full));
    }

    internal static void Craft(string key, GameSlotInventory[] slots, GameGridInventory output)
    {
        var plan = Plan(key, slots);
        if (plan == null || !Fits(key, output)) return;
        var session = StoreSession.Capture();
        var products = new List<GameItem>(2);
        bool committed = false;
        try
        {
            if (!Stage(Product(key), plan.Rod) || !Stage(RodMaterial.Offcut, plan.Offcut)
                || !ProductionOutput.TryAddSeparateBatch(output, products)) return;
            if (!session.IsCurrent || !IndustrialWorkbench.Learned(key) || plan.CoreSlot.childItem != plan.Core || plan.WrapSlot.childItem != plan.Wrap
                || !Accept(key, plan.Core) || !Accept(key, plan.Wrap)
                || RodCondition.Read(plan.Core) != plan.CoreStock || RodCondition.Read(plan.Wrap) != plan.WrapStock
                || products.Exists(item => item.parentInventory != output || !output.childItems.Contains(item)
                    || item.unitCount != 1 || StorePlacement.Destroyed(item))
                || RodCondition.Read(products[0]) != plan.Rod || RodCondition.Read(products[1]) != plan.Offcut) return;
            plan.Core.unitCount = 0;
            plan.Wrap.unitCount = 0;
            committed = true;
            HookGuard.Run("复合棒芯棒出库", () => IndustrialWorkbench.Discard(plan.Core));
            HookGuard.Run("复合棒外套出库", () => IndustrialWorkbench.Discard(plan.Wrap));
        }
        finally
        {
            if (!committed)
                foreach (var item in products) HookGuard.Run("撤回复合棒批次", () => IndustrialWorkbench.Discard(item));
        }

        bool Stage(string id, RodCondition condition)
        {
            var item = DirectoryMaster.Item("corp_presence_" + id, isOwned: true);
            if (item == null) return false;
            products.Add(item);
            condition.WriteFormed(item);
            return true;
        }
    }
}
