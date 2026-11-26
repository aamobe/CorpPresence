using System;

namespace CorpPresence;

internal static class CoreCalibration
{
    internal const string Key = "core_calibration_bench";
    private const string PretestTag = "CORP_CORE_PRETEST";
    private const string VerifiedFromTag = "CORP_CORE_VERIFIED_FROM";

    private sealed record Job(IndustrialCoreKind Kind, GameItem Feed, GameItem Part, GameItem Fixture)
    {
        internal int PretestCost => Kind == IndustrialCoreKind.Timing ? 8 : 10;
        internal int VerificationCost => Kind == IndustrialCoreKind.Timing ? 12 : 18;
        internal string Stamp => Part.uniqueId <= 0 || Fixture is { uniqueId: <= 0 } ? null
            : FormattableString.Invariant($"1;{(int)Kind};{Part.uniqueId};{Fixture?.uniqueId ?? 0}");
        internal bool Pretested => Stamp is string stamp && Feed.GetTagReadonly(PretestTag).GetString() == stamp;
    }

    internal static readonly Goods.Def Definition = new()
    {
        Name = "核心校准台", NameEn = "Core Calibration Bench", Value = 180,
        Company = "mineral", Art = "core-calibration-bench", Fallback = "furnace",
        W = 3, H = 3, Types = new[] { "MACHINE" }, Shell = Build
    };

    internal static bool IsVerified(GameItem item, IndustrialCoreKind kind)
    {
        var root = CorpState.Current;
        if (item?.identifier != "corp_presence_" + MeridianCoreIntake.Product(kind) || item.unitCount != 1
            || !GeneralHelper.IsItemOwned(item) || StorePlacement.Destroyed(item) || string.IsNullOrEmpty(root?.RunId)) return false;
        int source = item.GetTagReadonly(VerifiedFromTag).GetInt();
        return source > 0 && root.Workshop?.CoreIntake?.Id(kind) == source
            && item.GetTagReadonly(MeridianCoreIntake.OriginTag).GetString() == root.RunId;
    }

    private static IndustrialCoreKind? KindOf(GameItem item) => item?.identifier switch
    {
        "corp_presence_" + MeridianCoreIntake.TimingFeed => IndustrialCoreKind.Timing,
        "corp_presence_" + MeridianCoreIntake.PhaseFeed => IndustrialCoreKind.Phase,
        _ => null
    };

    private static Job Plan(CorpState root, GameItem feed, GameItem part, GameItem fixture)
    {
        if (KindOf(feed) is not IndustrialCoreKind kind || !MeridianCoreIntake.IsIssued(root, feed, kind)) return null;
        if (kind == IndustrialCoreKind.Timing)
        {
            if (!MachineService.IsStockPart(part, RodMaterial.IndustrialCoil)
                || (RodCondition.Read(part).Traits & RodTrait.LowLoss) == 0) return null;
            return new Job(kind, feed, part, null);
        }
        if (!MachineService.IsStockPart(part, RodMaterial.AxialLead) || (RodCondition.Read(part).Traits & RodTrait.AxialConductor) == 0
            || !MachineService.IsStockPart(fixture, RodMaterial.PressureShell) || !IndustrialPump.PressureStock(RodCondition.Read(fixture)))
            return null;
        return new Job(kind, feed, part, fixture);
    }

    private static void Finish(GameItem machine, GameSlotInventory battery, GameGridInventory output, Func<Job> current, Job job)
    {
        string productId = "corp_presence_" + MeridianCoreIntake.Product(job.Kind);
        if (!job.Pretested || !WorkshopCycle.CanDrawPower(battery, job.VerificationCost)
            || !MachineAppearance.CanFit(output, productId, separateUnit: true)) return;
        var session = StoreSession.Capture();
        var root = CorpState.Current;
        var result = DirectoryMaster.Item(productId, isOwned: true);
        if (result == null) return;
        bool committed = false;
        try
        {
            result.ModifyTag(MeridianCoreIntake.OriginTag, tag => tag.Enable().SetString(root.RunId), skipValidate: true);
            result.ModifyTag(VerifiedFromTag, tag => tag.Enable().SetInt(job.Feed.uniqueId), skipValidate: true);
            if (!ProductionOutput.TryAddSeparate(output, result) || !session.IsCurrent || !ReferenceEquals(root, CorpState.Current)
                || !ShopInventory.Contains(machine) || current() != job || !job.Pretested
                || !ReferenceEquals(output, result.parentInventory) || !output.childItems.Contains(result)
                || result.unitCount != 1 || StorePlacement.Destroyed(result)
                || !WorkshopCycle.DrawPower(battery, job.VerificationCost)) return;

            job.Feed.unitCount = 0;
            job.Part.unitCount = 0;
            committed = true;
            HookGuard.Run("清理已校准原件", () => IndustrialWorkbench.Discard(job.Feed));
            HookGuard.Run("清理已装入核心的零件", () => IndustrialWorkbench.Discard(job.Part));
        }
        finally
        {
            if (!committed) HookGuard.Run("撤回未完成校准的成品", () => IndustrialWorkbench.Discard(result));
        }
    }

    private static GameItem Build()
    {
        var battery = new GameSlotInventory();
        var feed = new GameSlotInventory(3, 2) { mayInventoryAddItemFunc = (item, _) => KindOf(item).HasValue };
        var part = new GameSlotInventory(3, 2) { mayInventoryAddItemFunc = (item, _) =>
            MachineService.IsStockPart(item, RodMaterial.IndustrialCoil) || MachineService.IsStockPart(item, RodMaterial.AxialLead) };
        var fixture = new GameSlotInventory(3, 2) { mayInventoryAddItemFunc = (item, _) => MachineService.IsStockPart(item, RodMaterial.PressureShell) };
        var output = new GameGridInventory(6, 4) { mayInventoryAddItemFunc = (_, _) => false };
        var text = new MachineryText(uppercase: false);
        var pretest = new MachineryButton(L.K("workshop.core.pretest", "预校", "Pretest"));
        var verify = new MachineryButton(L.K("workshop.core.verify", "复测并装配", "Verify and assemble"));
        var window = new PixelWindow(true, null, WindowStyle.Machinery);
        window.Attach(new MachineryPanelRow(MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.K("workshop.core.feed", "待校原件", "Original module"), feed, "input"),
            MachineHelper.Panel(L.K("workshop.core.part", "替换件", "Replacement"), part, "part"),
            MachineHelper.Panel(L.K("workshop.core.fixture", "定位壳", "Reference shell"), fixture, "fixture"),
            MachineHelper.Panel(L.K("production.crafting.output", "出料", "Output"), output, "output"),
            MachineHelper.Panel(L.K("workshop.core.control", "校准", "Calibration"), MachineryStack.Vertical(text, pretest, verify), "control"))
            .SetColumns(2).SetDisplayOrder(0, 5, 1, 2, 3, 4));
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, 8);
        Job CurrentJob() => Plan(CorpState.Current, feed.childItem, part.childItem, fixture.childItem);
        pretest.SetCallback(() => Operate(final: false));
        verify.SetCallback(() => Operate(final: true));
        foreach (var storage in new GameInventory[] { battery, feed, part, fixture, output })
        {
            storage.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            storage.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
            storage.onSlotChangeAmountItemFunc += (_, _, _) => Refresh();
        }
        window.openCallback += Refresh;
        return machine;

        void Operate(bool final)
        {
            WorkshopCycle.Run(machine, () =>
            {
                if (!ShopInventory.Contains(machine) || CurrentJob() is not { } job) return;
                if (final) { Finish(machine, battery, output, CurrentJob, job); return; }
                if (job.Pretested || !WorkshopCycle.CanDrawPower(battery, job.PretestCost)) return;
                job.Part.GetUniqueID();
                job.Fixture?.GetUniqueID();
                if (job.Stamp is not string stamp || !WorkshopCycle.DrawPower(battery, job.PretestCost)) return;
                job.Feed.ModifyTag(PretestTag, tag => tag.Enable().SetString(stamp), skipValidate: true);
            });
            Refresh();
        }

        void Refresh()
        {
            var job = CurrentJob();
            string message = job == null
                ? L.K("workshop.core.needParts", "时序需低损线圈。相位需定向引线和承压壳。", "Timing needs a low-loss coil. Phase needs an axial lead and a pressure shell.")
                : job.Pretested
                    ? L.F("workshop.core.readyVerify", "预校有效 · 复测耗电 {0}", "Pretest valid · Verification uses {0} charge", job.VerificationCost)
                    : L.F("workshop.core.readyPretest", "预校耗电 {0} · 复测耗电 {1}", "Pretest uses {0} charge · Verification uses {1}", job.PretestCost, job.VerificationCost);
            if (job != null && !WorkshopCycle.CanDrawPower(battery, job.Pretested ? job.VerificationCost : job.PretestCost))
                message += "\n" + L.K("workshop.pump.battery", "电量不足", "Insufficient charge");
            if (job?.Pretested == true && !MachineAppearance.CanFit(output, "corp_presence_" + MeridianCoreIntake.Product(job.Kind), separateUnit: true))
                message += "\n" + MineralNightReport.Reason(MineralStop.Full);
            text.SetText(message);
        }
    }
}
