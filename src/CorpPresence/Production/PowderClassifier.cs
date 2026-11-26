using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class PowderClassifier
{
    internal const string Key = "powder_classifier";
    private const string ModeTag = "CORP_CLASSIFIER_MODE", BatchTag = "CORP_CLASSIFIER_BATCH";
    private const string NightsTag = "CORP_CLASSIFIER_NIGHTS", LastTag = "CORP_CLASSIFIER_LAST";
    private const string EcoTag = "CORP_CLASSIFIER_ECO", PrecisionTag = "CORP_CLASSIFIER_PRECISION";
    private const string RigNightTag = "CORP_CLASSIFIER_RIG_NIGHT";

    internal static readonly Goods.Def Definition = new()
    {
        Name = "分级机", NameEn = "Powder Classifier", Value = 220, Company = "mineral", Art = "powder-classifier",
        Fallback = "furnace", W = 3, H = 3, Types = new[] { "MACHINE" }, Shell = Build,
        Desc = "单批通量取 60 点硅质，纯度至少 81%，耗 8 电。细筛或晶磨套放在模式件槽。筛垫另装，双份通量一夜取 120 点硅质、16 电，出 4 粗 2 细。",
        DescEn = "Single throughput uses 60 silica at 81% purity and 8 charge. Fit a fine screen or grinding sleeve in the attachment slot. A separate heavy pad enables four coarse and two fine bags in one night for 120 silica and 16 charge."
    };

    internal static bool HasActiveBatch(GameItem machine) => machine?.identifier == "corp_presence_" + Key
        && !string.IsNullOrEmpty(machine.GetTagReadonly(BatchTag).GetString());

    // An interrupted first input debit may already have charged the battery. Only credited batches have predictable output.
    internal static (bool Active, ClassifierBatch.Recipe Recipe, int Nights) ReadQueue(GameItem machine)
    {
        string id = machine.GetTagReadonly(BatchTag).GetString();
        if (string.IsNullOrEmpty(id)) return (false, null, 0);
        var recipe = FrozenRecipe(machine, id);
        int left = machine.GetTagReadonly(NightsTag).GetInt();
        return recipe == null || left < 0 || left >= recipe.Nights ? (true, null, -1) : (true, recipe, left);
    }

    private static ClassifierBatch.Recipe FrozenRecipe(GameItem machine, string id) => ClassifierBatch.WithService(
        ClassifierBatch.Restore(id), machine.GetTagReadonly(EcoTag).GetBool(), machine.GetTagReadonly(PrecisionTag).GetBool());

    private static bool IsSupport(GameItem item) => MachineService.IsStockPart(item, RodMaterial.ClassifierSupport)
        && (RodCondition.Read(item).Traits & RodTrait.DimensionStable) != 0;

    private static GameItem Build()
    {
        var power = new GameSlotInventory();
        var silica = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (i, _) => PowderBox.IsOwnedBox(i) };
        var feed = new GameGridInventory(9, 6) { mayInventoryAddItemFunc = (i, _) => Own(i, CartelReactor.Crystal) };
        var tool = new GameSlotInventory(1, 1) { mayInventoryAddItemFunc = (i, _) => i?.unitCount == 1
            && (Own(i, ClassifierGoods.Screen) || Own(i, ClassifierGoods.Sleeve)) };
        var output = new GameGridInventory(9, 6) { mayInventoryAddItemFunc = (_, _) => false };
        var pad = new GameSlotInventory(1, 1) { mayInventoryAddItemFunc = (i, _) => i?.unitCount == 1 && Own(i, IndustrialGoods.Pad) };
        var coil = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (i, _) => MachineService.Handbook
            && MachineService.IsStockPart(i, RodMaterial.IndustrialCoil) };
        var support = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (i, _) => MachineService.Handbook && IsSupport(i) };
        var coilPanel = MachineHelper.Panel(Goods.NameOf(RodMaterial.IndustrialCoil), coil, "service_coil").SetHidden(true);
        var supportPanel = MachineHelper.Panel(Goods.NameOf(RodMaterial.ClassifierSupport), support, "service_support").SetHidden(true);
        var status = new MachineryText(uppercase: false);
        var change = new MachineryButton(L.K("workshop.mill.select", "切换档位", "Change mode"));
        var report = new MachineryButton(L.K("workshop.mill.report", "上夜实绩", "Last night"));
        var closeCover = new MachineryButton(L.T("合上检修盖", "Close cover"));
        var window = MachineHelper.CreateMachineryWindow(true, new[] { 0, 3, 6, 7, 8, 1, 2, 5, 4 },
            MachineHelper.PowerPanel(power),
            MachineHelper.Panel(L.K("workshop.mill.silica", "硅粉盒", "Silica box"), silica, "silica"),
            MachineHelper.Panel(L.K("workshop.mill.feed", "光盐析晶", "Light salt precipitate"), feed, "feed"),
            MachineHelper.Panel(L.K("workshop.mill.tool", "模式件", "Attachment"), tool, "tool"),
            MachineHelper.Panel(L.K("workshop.mill.out", "成品", "Products"), output, "output"),
            MachineHelper.Panel(L.K("workshop.mill.process", "加工", "Processing"), MachineryStack.Vertical(status, change, report, closeCover), "control"),
            MachineHelper.Panel(L.K("workshop.mill.padSlot", "重载筛垫", "Heavy screen pad"), pad, "pad"),
            coilPanel, supportPanel);
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        MachineService.Register(machine, () => machine.identifier == "corp_presence_" + Key && MachineService.Handbook, visible =>
        {
            coilPanel.SetHidden(!visible);
            supportPanel.SetHidden(!visible);
        });
        closeCover.SetCallback(() => MachineService.Close(machine));
        machine.EnableTag("CONTAINER_TAG");
        machine.EnableTag("STANDARD_MACHINE_TAG");
        MachineHelper.SetupBatterySlot(power, machine);
        MachineryHelper.InitMachinery(machine, ClassifierBatch.Power);

        int Mode() => Math.Clamp(machine.GetTagReadonly(ModeTag).GetInt(), 0, 3);
        int Left() => machine.GetTagReadonly(NightsTag).GetInt();
        string Batch() => machine.GetTagReadonly(BatchTag).GetString();
        bool Locked() => !string.IsNullOrEmpty(Batch());
        bool OwesFeed() => Batch()?.StartsWith("charge:", StringComparison.Ordinal) == true;
        ClassifierBatch.Recipe Frozen() => FrozenRecipe(machine, OwesFeed() ? Batch().Substring(7) : Batch());
        GameItem Precipitate() => feed.childItems.Find(i => Own(i, CartelReactor.Crystal));
        bool HasPad() => pad.childItem?.unitCount == 1 && Own(pad.childItem, IndustrialGoods.Pad);
        bool HasSupport() => MachineService.Handbook && IsSupport(support.childItem);
        ClassifierBatch.Recipe Selected() => ClassifierBatch.WithService(
            ClassifierBatch.Plan(Mode(), Goods.IdOf(tool.childItem), ClassifierGoods.Charges(tool.childItem), HasPad()),
            MachineService.HasLowLossCoil(coil.childItem), HasSupport());
        long Night() => (long)StoreStation.GetDayCounter() + 1;
        bool WaitForNextNight() => Night() > 0 && machine.GetTagReadonly(RigNightTag).GetLong() == Night()
            && (!Locked() || !OwesFeed() && Left() > 0);
        tool.mayInventoryRemoveItemFunc = (_, _) => !Locked() || Frozen()?.Id is "flow.v1" or "double.v1";
        pad.mayInventoryRemoveItemFunc = (_, _) => !Locked() || Frozen()?.Id != "double.v1";
        coil.mayInventoryRemoveItemFunc = (_, _) => !Locked() || Frozen()?.LowLoss == false;
        support.mayInventoryRemoveItemFunc = (_, _) => !Locked() || Frozen()?.Precision == false;

        // Every product occupies one square. Reserve a separate footprint for every unit, even with stacking mods.
        bool Fits(ClassifierBatch.Recipe r) => MachineAppearance.CanFit(output,
            "corp_presence_" + (r.Crystal > 0 ? ClassifierGoods.Crystal : ClassifierGoods.Coarse),
            r.Crystal + r.Coarse + r.Fine, separateUnit: true)
            && (r.Fine == 0 || DirectoryMaster.Has<GameItem>("corp_presence_" + ClassifierGoods.Fine));

        int[] SilicaAfter(ClassifierBatch.Recipe recipe)
        {
            if (!PowderBox.IsOwnedBox(silica.childItem)) return null;
            var parts = new int[PowderBox.ComponentCount];
            PowderBox.Read(silica.childItem, parts);
            return ClassifierBatch.TakeSilica(parts, recipe.Silica, recipe.SilicaPurity) ? parts : null;
        }

        string SilicaShort(ClassifierBatch.Recipe r) => r.Precision ? "precisionSilica" : r.Silica == 120 ? "doubleSilica" : "silica";

        string State(ClassifierBatch.Recipe recipe, out int[] after)
        {
            after = null;
            if (WaitForNextNight()) return "settled";
            if (Locked())
            {
                if (recipe == null || Left() < 0 || Left() > recipe.Nights) return "unknown";
                if (recipe.Crystal > 0 && !Own(tool.childItem, ClassifierGoods.Sleeve)) return "tool";
                if (OwesFeed())
                {
                    if (recipe.Crystal > 0 && Precipitate() == null) return "feed";
                    if (recipe.Crystal == 0 && (after = SilicaAfter(recipe)) == null) return SilicaShort(recipe);
                    return "pay";
                }
                if (Left() == 0) return Fits(recipe) ? "deliver" : "full";
            }
            else
            {
                if (Mode() == 2 && !CartelShizuka.BlueprintDelivered(CorpState.Current)) return "blueprint";
                if (recipe == null) return "tool";
                if (recipe.Crystal > 0 && Precipitate() == null) return "feed";
                if (recipe.Crystal == 0 && (after = SilicaAfter(recipe)) == null) return SilicaShort(recipe);
                if (!Fits(recipe)) return "full";
            }
            return machine.GetTagReadonly(MachineryHelper.IS_MACHINERY_POWERED).GetBool()
                && WorkshopCycle.CanDrawPower(machine, power, recipe.NightPower) ? "ready" : "power";
        }

        void Refresh()
        {
            var r = Locked() ? Frozen() : Selected();
            string summary = ModeLabel(Mode()) + "\n" + Reason(State(r, out _));
            if (r != null)
            {
                int nights = Locked() ? Left() : r.Nights;
                summary += "\n" + Yield(r) + "\n" + (r.LowLoss
                    ? L.F("workshop.mill.serviceDuration", "剩 {0} 夜 · 每夜 {1} 电", "{0} nights left · {1} charge/night", nights, r.NightPower)
                    : r.NightPower == ClassifierBatch.Power
                        ? L.F("workshop.mill.duration", "剩 {0} 夜 · 每夜 8 电", "{0} nights left · 8 charge/night", nights)
                        : L.F("workshop.mill.doubleDuration", "剩 {0} 夜 · 每夜 16 电", "{0} nights left · 16 charge/night", nights));
                if (r.Precision) summary += "\n" + L.K("workshop.mill.precisionReady", "精密支承 · 硅质至少 70%", "Precision support · silica purity of at least 70%");
            }
            if (Own(tool.childItem, ClassifierGoods.Sleeve)) summary += "\n" + L.F("workshop.mill.charges",
                "剩余快磨 {0}/3 批", "Fast batches left {0}/3", ClassifierGoods.Charges(tool.childItem));
            status.SetText(summary);
        }
        change.SetCallback(() => WorkshopCycle.Run(machine, () =>
        {
            if (Locked()) return;
            int next = (Mode() + 1) % 4;
            if (next == 2 && !CartelShizuka.BlueprintDelivered(CorpState.Current)) next = 3;
            if (next == 3 && !HasPad()) next = 0;
            machine.ModifyTag(ModeTag, t => t.Enable().SetInt(next));
            SetModePower();
            Refresh();
        }));
        report.SetCallback(() =>
        {
            string[] fields = machine.GetTagReadonly(LastTag).GetString()?.Split('|');
            string message = L.K("workshop.machine.noRecord", "还没有上一夜的记录", "No previous night recorded");
            if (fields?.Length == 3)
            {
                message = Reason(fields[0]) + "\n" + L.F("workshop.machine.usedPower", "实际耗电 {0}", "Charge used {0}", fields[1]);
                var recipe = ClassifierBatch.Restore(fields[2]);
                if (fields[0] == "done" && recipe != null) message += "\n" + Yield(recipe);
            }
            var ui = GenericConfirmationUIManager.Instance;
            ui?.OpenPanelSingleButton(message, L.K("production.canning.buildCanner.oK", "确定", "OK"), () => ui.ClosePanel());
        });

        void Settle()
        {
            var r = Locked() ? Frozen() : Selected();
            string outcome = State(r, out var after);
            if (outcome == "settled") return;
            int used = 0;
            GameItem changedBox = null;
            if (outcome == "ready")
            {
                if (WorkshopCycle.DrawPower(machine, power, r.NightPower))
                {
                    used = r.NightPower;
                    bool starting = !Locked();
                    int left = (starting ? r.Nights : Left()) - 1;
                    machine.ModifyTag(NightsTag, t => t.Enable().SetInt(left), skipValidate: true);
                    if (starting)
                    {
                        machine.ModifyTag(EcoTag, t => t.Enable().SetBool(r.LowLoss), skipValidate: true);
                        machine.ModifyTag(PrecisionTag, t => t.Enable().SetBool(r.Precision), skipValidate: true);
                        machine.ModifyTag(BatchTag, t => t.Enable().SetString("charge:" + r.Id), skipValidate: true);
                    }
                    if (r.Retrofitted && Night() > 0)
                        machine.ModifyTag(RigNightTag, t => t.Enable().SetLong(Night()), skipValidate: true);
                    outcome = starting ? "pay" : left == 0 ? "deliver" : "working";
                }
                else outcome = "power";
            }
            if (outcome == "pay")
            {
                bool paid;
                if (r.Crystal == 0)
                {
                    after = SilicaAfter(r);
                    changedBox = silica.childItem;
                    paid = after != null;
                    if (paid) PowderBox.CommitComposition(changedBox, after);
                    else changedBox = null;
                }
                else paid = TryConsume(Precipitate());
                if (paid)
                {
                    machine.ModifyTag(BatchTag, t => t.Enable().SetString(r.Id), skipValidate: true);
                    outcome = Left() == 0 ? "deliver" : "working";
                }
                else outcome = "pay";
            }
            if (outcome == "deliver")
            {
                if (Fits(r) && TryDeliver(output, r))
                {
                    if (r == ClassifierBatch.Fast)
                        ClassifierGoods.SetCharges(tool.childItem, ClassifierGoods.Charges(tool.childItem) - 1);
                    machine.ModifyTag(BatchTag, t => t.Enable().SetString(""), skipValidate: true);
                    machine.DisableTag(EcoTag, skipValidate: true);
                    machine.DisableTag(PrecisionTag, skipValidate: true);
                    outcome = "done";
                }
                else outcome = "full";
            }
            machine.ModifyTag(LastTag, t => t.Enable().SetString(outcome + "|" + used + "|" + r?.Id), skipValidate: true);
            if (changedBox != null) PowderBox.RefreshComposition(changedBox, after);
            Refresh();
        }

        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run("分级机过夜", () => WorkshopCycle.Run(machine, Settle));
        foreach (var inventory in new GameInventory[] { power, silica, feed, tool, output, pad, coil, support })
        {
            inventory.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        }
        window.openCallback += () => { MachineService.OpenWindow(machine); Refresh(); };
        window.closeCallback += () => MachineService.Close(machine);
        void SetModePower()
        {
            int cost = Mode() == 3 ? 16 : ClassifierBatch.Power;
            machine.ModifyTag(MachineryHelper.MACHINERY_DRAWN_POWER_BASE, t => t.Enable().SetInt(cost), skipValidate: true);
            machine.ModifyTag(MachineryHelper.MACHINERY_DRAWN_POWER, t => t.Enable().SetInt(cost), skipValidate: true);
        }
        machine.onLoaded += _ => { SetModePower(); Refresh(); };
        return machine;
    }

    private static bool Own(GameItem item, string key) => item?.unitCount > 0
        && (item is not GameItemElement element || !element.IsDestroyed())
        && GeneralHelper.IsItemOwned(item) && Goods.IdOf(item) == key;
    private static void Consume(GameItem item)
    {
        if (item.unitCount == 1) item.Destroy();
        else item.SetUnitCount(item.unitCount - 1);
    }

    private static bool TryConsume(GameItem item)
    {
        if (item == null) return false;
        int before = item.unitCount;
        var source = item.parentInventory;
        try { Consume(item); }
        catch (Exception error) { ModLog.Warn("分级机按原料实扣核账：" + error.Message); }
        return before == 1 ? item.parentInventory != source || item.unitCount == 0
            || item is GameItemElement element && element.IsDestroyed() : item.unitCount == before - 1;
    }

    private static bool TryDeliver(GameGridInventory output, ClassifierBatch.Recipe recipe)
    {
        var batch = new List<GameItem>();
        bool committed = false;
        bool Add(string key, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = DirectoryMaster.Item("corp_presence_" + key, isOwned: true);
                if (item == null) return false;
                batch.Add(item);
            }
            return true;
        }
        try
        {
            return committed = Add(ClassifierGoods.Crystal, recipe.Crystal) && Add(ClassifierGoods.Coarse, recipe.Coarse)
                && Add(ClassifierGoods.Fine, recipe.Fine) && ProductionOutput.TryAddSeparateBatch(output, batch);
        }
        finally { if (!committed) ProductionOutput.DiscardBatch(batch); }
    }

    private static string Yield(ClassifierBatch.Recipe r) => r.Crystal > 0
        ? L.K("workshop.mill.crystalYield", "1 析晶 → 2 包晶粉", "1 precipitate → 2 crystal bags")
        : r.Silica == 120 ? L.K("workshop.mill.doubleYield", "120 硅 → 4 粗 + 2 细", "120 silica → 4 coarse + 2 fine")
        : L.F("workshop.mill.sandYield", "60 硅 → {0} 粗 + {1} 细", "60 silica → {0} coarse + {1} fine", r.Coarse, r.Fine);

    private static string ModeLabel(int mode) => mode switch
    {
        1 => L.K("workshop.mill.fineMode", "细筛档", "Fine mode"),
        2 => L.K("workshop.mill.crystalMode", "晶磨档", "Crystal milling"),
        3 => L.K("workshop.mill.doubleMode", "双份通量档", "Double throughput"),
        _ => L.K("workshop.mill.flowMode", "通量档", "Throughput mode")
    };

    private static string Reason(string reason) => reason switch
    {
        "tool" => L.K("workshop.mill.needTool", "装入对应模式件", "Fit the matching attachment"),
        "blueprint" => L.K("workshop.mill.needBlueprint", "等待黑羽静送光盐蓝图", "Await Shizuka’s light salt blueprint"),
        "feed" => L.K("workshop.mill.needCrystal", "放入 1 份光盐析晶", "Insert one light salt precipitate"),
        "silica" => L.K("workshop.mill.needSilica", "需 60 点硅质，纯度至少 81%", "Needs 60 silica points at 81% purity"),
        "doubleSilica" => L.K("workshop.mill.needDoubleSilica", "需 120 点硅质，纯度至少 81%", "Needs 120 silica points at 81% purity"),
        "precisionSilica" => L.K("workshop.mill.needPrecisionSilica", "需 60 点硅质，纯度至少 70%", "Needs 60 silica points at 70% purity"),
        "power" => MineralNightReport.Reason(MineralStop.NoPower),
        "full" => MineralNightReport.Reason(MineralStop.Full),
        "deliver" => L.K("workshop.mill.deliver", "已磨好，下次结算免费出料", "Finished. Unloads next cycle at no charge"),
        "working" => L.K("workshop.mill.working", "已加工一夜", "One night of work completed"),
        "pay" => L.K("workshop.mill.prepaid", "本夜电已付，待投料确认", "Night prepaid, awaiting feed confirmation"),
        "done" => L.K("workshop.mill.done", "整批已出料", "Entire batch delivered"),
        "ready" => L.K("workshop.mill.ready", "今晚可加工", "Ready for tonight"),
        "settled" => L.K("workshop.mill.serviceSettled", "本夜电力已结算", "This night's power has been settled"),
        _ => L.K("workshop.mill.unknown", "保留未知批次", "Unrecognized batch retained")
    };
}
