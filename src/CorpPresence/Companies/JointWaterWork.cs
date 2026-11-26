using System;

namespace CorpPresence;

internal static class JointWaterWork
{
    internal const string Kit = "water_service_kit";
    internal static readonly Goods.Def Definition = new()
    {
        Name = "水站复工备件", NameEn = "Water Station Service Kit", Value = 80,
        Company = "mineral", Art = "water-service-kit", Fallback = "wire",
        W = 1, H = 1, Shape = "#", Types = new[] { "MATERIAL" },
        Shell = () => ItemDirectory.CreateEmptyItem(), Setup = item => item.mayStackItemFunc = null
    };

    internal static bool IsKit(GameItem item) => item?.identifier == "corp_presence_" + Kit && item.unitCount == 1
        && GeneralHelper.IsItemOwned(item) && !StorePlacement.Destroyed(item);

    internal static bool Holds(GameItem station, GameItem item)
    {
        var work = CorpState.Current?.Workshop;
        var site = work?.PhaseSite;
        var order = work?.JointWaterWork;
        return item is { unitCount: > 0, uniqueId: > 0 } && site is { Route: PhaseRoute.Whole, StationId: > 0 }
            && order?.Valid() == true && station.uniqueId == site.StationId
            && (item.uniqueId == order.KitId && !order.LoadDay.HasValue
                || item.uniqueId == site.CargoId && order.ServiceNights < JointWaterWorkState.ShiftsRequired);
    }

    internal static string Progress(JointWaterWorkState order)
    {
        if (order.ServiceNights == JointWaterWorkState.ShiftsRequired)
            return L.K("workshop.joint.complete", "复工已完成，可以取回泵组", "Service complete. Retrieve the pump.");
        if (order.LoadDay.HasValue)
            return L.F("workshop.joint.shifts", "有效供水 {0}/{1} 夜，请保持水量、电量与回水空间", "Completed wet shifts {0}/{1}. Keep water, charge and receiving space available.", order.ServiceNights, JointWaterWorkState.ShiftsRequired);
        return order.ArrivalDay.HasValue
            ? L.K("workshop.joint.waitLoad", "已到位。等待加压夜班，请留好备件、电量和试液", "On site. Keep the kit, charge and water ready for the pressure shift.")
            : L.K("workshop.joint.waitArrival", "已签发。原泵和备件下一营业日登记到位", "Signed. The original pump and kit are recorded on site next business day.");
    }

    internal static string SignMessage(SaleStatus status) => status switch
    {
        SaleStatus.Ready => L.K("workshop.joint.signed", "已签发联合复工", "Joint recommissioning signed"),
        SaleStatus.Outstanding => L.K("workshop.joint.already", "这张联合工单已签发", "This joint work order is already signed"),
        SaleStatus.Short => L.K("workshop.joint.needDock", "请装入已完成起停试水且带承压壳的原回收泵和复工备件", "Dock the recovered pump with restart proof and a pressure shell, plus the service kit."),
        _ => L.K("workshop.joint.needReturn", "从运输井整机回柜后开放联合复工", "Joint work opens after the intact shaft pump arrives")
    };

    private static bool Current(CorpState root, StoreSession session) => session.IsCurrent
        && ReferenceEquals(CorpState.Current, root) && root?.Workshop != null && !string.IsNullOrEmpty(root.RunId)
        && !StoreSession.CompanyProgressPaused && SaveSlot.CanWrite("CorpPresence");

    private static bool Matches(CorpState root, PhaseSiteState site, GameItem station, GameItem pump) =>
        site is { Route: PhaseRoute.Whole, CargoId: > 0, StationId: > 0 } && site.Valid()
        && PhaseRecoveryStation.Available(station) && station.uniqueId == site.StationId
        && pump?.identifier == "corp_presence_" + IndustrialPump.Key && pump.unitCount == 1 && pump.uniqueId == site.CargoId
        && GeneralHelper.IsItemOwned(pump) && !StorePlacement.Destroyed(pump) && ShopInventory.Contains(pump)
        && pump.GetTagReadonly(PhaseSite.RecoveredTag).GetString() == root.RunId;

    internal static SaleStatus Sign(GameItem station)
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return SaleStatus.Unavailable;
        try
        {
            var root = CorpState.Current;
            var session = StoreSession.Capture();
            int day = StoreStation.GetDayCounter();
            var site = root?.Workshop?.PhaseSite;
            if (!Current(root, session) || site is not { Route: PhaseRoute.Whole, CargoId: > 0 } || !(site.DueDay <= day))
                return SaleStatus.Unavailable;
            if (root.Workshop.JointWaterWork != null) return SaleStatus.Outstanding;
            var (pump, kit) = PhaseRecoveryStation.ServiceStock(station);
            if (!Matches(root, site, station, pump) || !IsKit(kit) || !IndustrialPump.ReadyForCommissioning(pump))
                return SaleStatus.Short;
            var order = new JointWaterWorkState { SignedDay = day, KitId = kit.GetUniqueID() };
            if (!order.Valid() || order.KitId == pump.uniqueId || order.KitId == station.uniqueId
                || !Current(root, session) || !ReferenceEquals(site, root.Workshop.PhaseSite)) return SaleStatus.Changed;
            root.Workshop.JointWaterWork = order;
            return SaleStatus.Ready;
        }
        finally { CompanyTransaction.Exit(); }
    }

    internal static void RunNight(GameItem station)
    {
        WorkshopCycle.Run(station, () =>
        {
            var root = CorpState.Current;
            var session = StoreSession.Capture();
            if (!Current(root, session) || root.Workshop.JointWaterWork is not { } order || !order.Valid()
                || order.InFlight || order.ServiceNights >= JointWaterWorkState.ShiftsRequired) return;
            var site = root.Workshop.PhaseSite;
            int day = StoreStation.GetDayCounter();
            if (day <= (order.LastServiceDay ?? order.LoadDay ?? order.ArrivalDay ?? order.SignedDay)) return;
            var (pump, kit) = PhaseRecoveryStation.ServiceStock(station);
            if (!Matches(root, site, station, pump) || !IndustrialPump.ReadyForCommissioning(pump)) return;
            if (!order.ArrivalDay.HasValue)
            {
                if (IsKit(kit) && kit.uniqueId == order.KitId) order.ArrivalDay = day;
                return;
            }
            bool testing = !order.LoadDay.HasValue;
            if (testing && (!IsKit(kit) || kit.uniqueId != order.KitId)) return;
            if (!testing && IndustrialPump.Passed(pump, IndustrialPump.Test.Pressure) == null) return;
            if (!IndustrialPump.TryCommissioningShift(pump, testing, DockIsCurrent)) return;
            if (!Current(root, session) || !ReferenceEquals(order, root.Workshop.JointWaterWork)) return;
            if (testing)
            {
                kit.unitCount = 0;
                order.LoadDay = day;
                HookGuard.Run("清空已使用的水站备件", () => IndustrialWorkbench.Discard(kit));
            }
            else
            {
                order.ServiceNights++;
                order.LastServiceDay = day;
            }

            bool DockIsCurrent()
            {
                var docked = PhaseRecoveryStation.ServiceStock(station);
                return Current(root, session) && ReferenceEquals(order, root.Workshop.JointWaterWork)
                    && ReferenceEquals(site, root.Workshop.PhaseSite)
                    && docked.Pump == pump && Matches(root, site, station, pump)
                    && (!testing || docked.Kit == kit && IsKit(kit));
            }
        });
    }

    internal static SaleStatus PayNext()
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return SaleStatus.Unavailable;
        try
        {
            var root = CorpState.Current;
            var session = StoreSession.Capture();
            if (!Current(root, session) || root.Workshop.JointWaterWork is not { } order) return SaleStatus.Unavailable;
            if (!order.Valid() || order.InFlight || StoreStation.GetDayCounter() < (order.LastServiceDay ?? order.LoadDay ?? order.ArrivalDay ?? order.SignedDay))
                return SaleStatus.Frozen;
            if (order.Paid == JointWaterWorkState.Total) return SaleStatus.TrialSettled;
            int due = order.Due;
            if (due == 0) return SaleStatus.Short;
            var store = SaveSlot.CurrentStore;
            int before = store.GetCash();
            if (before > int.MaxValue - due) return SaleStatus.WalletFull;
            order.InFlight = true;
            try { store.ModCash(due, playSound: false); }
            catch (Exception error) { ModLog.Warn("水站复工付款中断：" + error.GetType().Name); }
            if (!Current(root, session) || !ReferenceEquals(order, root.Workshop.JointWaterWork)) return SaleStatus.Frozen;
            long paid = (long)store.GetCash() - before;
            if (paid < 0 || paid > due) return SaleStatus.Frozen;
            order.Paid += (int)paid;
            order.InFlight = false;
            return paid == due ? SaleStatus.Paid : SaleStatus.Partial;
        }
        finally { CompanyTransaction.Exit(); }
    }
}
