using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CorpPresence;

internal static class PhaseRecoveryStation
{
    internal const string Key = "phase_recovery_station", Winch = "phase_heavy_winch", Drive = "phase_conductor";
    private const string WholeTag = "CORP_PHASE_WHOLE", PeakTag = "CORP_PHASE_LIFT";
    private sealed record Bays(GameSlotInventory Power, GameSlotInventory Pump, GameSlotInventory Kit);
    private static readonly ConditionalWeakTable<GameItem, Bays> Stations = new();

    internal static readonly Dictionary<string, Goods.Def> Definitions = new()
    {
        [Key] = new()
        {
            Name = "相位回收站", NameEn = "Phase Recovery Station", Value = 520,
            Company = "mineral", Art = "phase-recovery-station", Fallback = "furnace",
            W = 3, H = 3, Types = new[] { "MACHINE" }, Shell = Build
        },
        [Winch] = Stock("重载绞盘", "Heavy Winch", 95, "metal_ingot", "phase-heavy-winch"),
        [Drive] = Stock("相位导电总成", "Phase Conductor Assembly", 105, "wire", "phase-conductor")
    };

    private static Goods.Def Stock(string name, string english, int value, string fallback, string art) => new()
    {
        Name = name, NameEn = english, Value = value, Company = "mineral", Art = art, Fallback = fallback,
        W = 2, H = 1, Shape = "##", Types = new[] { "MATERIAL" },
        Shell = () => ItemDirectory.CreateEmptyItem(), Setup = item => item.mayStackItemFunc = null
    };

    internal static bool Qualifies(string recipe, string key, GameItem item) => (recipe, key) switch
    {
        (Key, MeridianCoreIntake.PhaseCore) => CoreCalibration.IsVerified(item, IndustrialCoreKind.Phase),
        (Key, RodMaterial.PressureShell) => MachineService.IsStockPart(item, key)
            && IndustrialPump.PressureStock(RodCondition.Read(item)) && RodCondition.Read(item).Surface.Corrosion >= 45,
        (Winch, RodMaterial.FeedShaft) => MachineService.IsStockPart(item, key) && (RodCondition.Read(item).Traits & RodTrait.Fatigue) != 0,
        (Drive, RodMaterial.AxialLead) => MachineService.IsStockPart(item, key) && (RodCondition.Read(item).Traits & RodTrait.AxialConductor) != 0,
        (Drive, RodMaterial.IndustrialCoil) => MachineService.IsStockPart(item, key),
        _ => true
    };

    internal static bool Available(GameItem item) => item?.identifier == "corp_presence_" + Key && item.unitCount == 1
        && GeneralHelper.IsItemOwned(item) && !StorePlacement.Destroyed(item) && ShopInventory.Contains(item)
        && Stations.TryGetValue(item, out _);

    internal static bool HasPowerBay(GameItem item, GameSlotInventory bay) => Available(item)
        && Stations.TryGetValue(item, out var actual) && ReferenceEquals(actual.Power, bay);

    internal static (GameItem Pump, GameItem Kit) ServiceStock(GameItem station) => station != null
        && Stations.TryGetValue(station, out var bays) ? (bays.Pump.childItem, bays.Kit.childItem) : default;

    internal static string RouteCommitment() => L.K("workshop.phase.once",
        "每个现场只派工一次。整机回运才开放水站联合复工。",
        "Each site can be dispatched once. Only intact recovery opens joint recommissioning.");

    private static GameItem Build()
    {
        var battery = new GameSlotInventory();
        var pump = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) =>
            item?.identifier == "corp_presence_" + IndustrialPump.Key && GeneralHelper.IsItemOwned(item)
            && !string.IsNullOrEmpty(item.GetTagReadonly(PhaseSite.RecoveredTag).GetString()) };
        var kit = new GameSlotInventory(2, 2) { mayInventoryAddItemFunc = (item, _) => JointWaterWork.IsKit(item) };
        var status = new MachineryText(uppercase: false);
        var mode = new MachineryButton();
        var peak = new MachineryButton();
        var launch = new MachineryButton(L.K("workshop.phase.launch", "签发回收", "Dispatch recovery"));
        var service = new MachineryButton(L.K("workshop.joint.sign", "签发水站复工", "Sign water station recommissioning"));
        var window = new PixelWindow(true, null, WindowStyle.Machinery);
        window.Attach(new MachineryPanelRow(MachineHelper.PowerPanel(battery),
            MachineHelper.Panel(L.K("workshop.phase.control", "运输井回收", "Shaft recovery"),
                MachineryStack.Vertical(status, mode, peak, launch, service), "control"),
            MachineHelper.Panel(L.K("workshop.joint.pump", "复工泵", "Service pump"), pump, "service_pump"),
            MachineHelper.Panel(Goods.NameOf(JointWaterWork.Kit), kit, "service_kit")).SetColumns(2));
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        pump.mayInventoryRemoveItemFunc = (item, _) => !JointWaterWork.Holds(machine, item);
        kit.mayInventoryRemoveItemFunc = (item, _) => !JointWaterWork.Holds(machine, item);
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, PhaseSiteState.WholePower);
        Stations.Add(machine, new Bays(battery, pump, kit));
        PhaseResult? result = null;
        SaleStatus? serviceResult = null;
        mode.SetCallback(() => Configure(() =>
            machine.ModifyTag(WholeTag, tag => tag.Enable().SetBool(!Whole()))));
        peak.SetCallback(() => Configure(() =>
            machine.ModifyTag(PeakTag, tag => tag.Enable().SetInt(Peak() switch { 16 => 24, 24 => 32, 32 => 0, _ => 16 }))));
        launch.SetCallback(() =>
        {
            serviceResult = null;
            result = PhaseSite.Dispatch(machine, battery, Whole() ? PhaseRoute.Whole : PhaseRoute.Parts, Peak());
            Refresh();
        });
        service.SetCallback(() =>
        {
            result = null;
            serviceResult = JointWaterWork.Sign(machine);
            Refresh();
        });
        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run("水站联合复工", () => JointWaterWork.RunNight(machine));
        window.openCallback += () => { result = null; serviceResult = null; Refresh(); };
        return machine;

        bool Whole() => machine.GetTagReadonly(WholeTag).GetBool();
        int Peak() => machine.GetTagReadonly(PeakTag).GetInt();

        void Configure(Action change)
        {
            WorkshopCycle.Run(machine, change);
            result = null;
            serviceResult = null;
            Refresh();
        }

        void Refresh()
        {
            mode.SetText(Whole()
                ? L.K("workshop.phase.whole", "整机回运", "Recover intact")
                : L.K("workshop.phase.parts", "分装回运", "Recover as parts"));
            peak.SetText(L.F("workshop.phase.peak", "起吊校准 {0}", "Lift calibration {0}", Peak()));
            var site = CorpState.Current?.Workshop?.PhaseSite;
            var work = CorpState.Current?.Workshop?.JointWaterWork;
            status.SetText(serviceResult.HasValue ? JointWaterWork.SignMessage(serviceResult.Value)
                : work != null && site?.StationId == machine.uniqueId ? JointWaterWork.Progress(work)
                : result.HasValue ? Message(result.Value)
                : site?.CargoId.HasValue == true ? Message(PhaseResult.Used)
                : site?.DueDay is int due ? L.F("workshop.phase.arrival", "回柜日 {0}", "Counter arrival on day {0}", due)
                : L.K("workshop.phase.setup", "在工程调度勘察布标，腾出回货位。分装 16 电，整机 24 电。", "Survey from the engineering desk and clear a return space. Parts need 16 charge. Intact recovery needs 24.")
                    + "\n" + RouteCommitment());
        }
    }

    internal static string Message(PhaseResult result) => result switch
    {
        PhaseResult.Ready => L.K("workshop.phase.recorded", "工程已登记", "Recovery recorded"),
        PhaseResult.NeedSurvey => L.K("workshop.phase.needSurvey", "先到工程调度请求勘察", "Request a survey from the engineering desk"),
        PhaseResult.Surveying => L.K("workshop.phase.waitSurvey", "班组次日完成定位与布标", "The crew places the beacon the next business day"),
        PhaseResult.NoDispatch => L.K("workshop.phase.noDispatch", "先签发一条回收路线", "Dispatch a recovery route first"),
        PhaseResult.Load => L.K("workshop.phase.needLift", "提高起吊校准到该路线要求", "Set the required lift calibration"),
        PhaseResult.Power => L.K("workshop.pump.battery", "电量不足", "Insufficient charge"),
        PhaseResult.Cash => L.K("workshop.phase.cash", "现金不足以支付班组余款", "Not enough cash for the crew balance"),
        PhaseResult.Space => L.K("workshop.phase.space", "柜台缺少回货位置", "Make space for the return on the counter"),
        PhaseResult.EnRoute => L.K("workshop.phase.enRoute", "货物仍在运输中", "Cargo is still in transit"),
        PhaseResult.Used => L.K("workshop.phase.used", "本现场已经签发或回收", "This site is dispatched or already recovered"),
        PhaseResult.StationMissing => L.K("workshop.phase.stationMissing", "请把签发用的相位站放回店内", "Put the dispatch station back in the shop"),
        PhaseResult.Partial => L.K("workshop.phase.partial", "已记录实付，签发需续交余款", "Payment recorded. Settle the balance to dispatch."),
        PhaseResult.Changed => L.K("workshop.phase.changed", "工程凭单或货位变化，请重新核对", "The recovery record or cargo space changed. Check it again."),
        _ => L.K("workshop.phase.locked", "先结清水站整机验收", "Settle the water station pump acceptance first")
    };
}
