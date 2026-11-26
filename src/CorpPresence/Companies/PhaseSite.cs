using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal enum PhaseResult { Ready, Locked, NeedSurvey, Surveying, NoDispatch, Load, Power, Cash, Space, EnRoute, Used, StationMissing, Changed, Partial }

internal static class PhaseSite
{
    internal const string RecoveredTag = "CORP_PHASE_RECOVERED_RUN";

    internal static PhaseResult Survey()
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return PhaseResult.Locked;
        try
        {
            var root = CorpState.Current;
            int day = StoreStation.GetDayCounter();
            if (!Current(root, StoreSession.Capture()) || !WaterStationRepair.EngineeringCleared(root) || day < 0 || day > int.MaxValue - 3)
                return PhaseResult.Locked;
            if (root.Workshop.PhaseSite != null) return PhaseResult.Used;
            root.Workshop.PhaseSite = new PhaseSiteState { SurveyDay = day };
            return PhaseResult.Ready;
        }
        finally { CompanyTransaction.Exit(); }
    }

    internal static PhaseResult BookCrew()
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return PhaseResult.Locked;
        try
        {
            var root = CorpState.Current;
            var session = StoreSession.Capture();
            var state = root?.Workshop?.PhaseSite;
            int day = StoreStation.GetDayCounter();
            var status = BeforeDeparture(root, session, state, day, PhaseRoute.Crew);
            if (status != PhaseResult.Ready) return status;
            var store = SaveSlot.CurrentStore;
            int before = store.GetCash();
            int due = PhaseSiteState.CrewFee - state.CrewPaid;
            if (before < due) return PhaseResult.Cash;
            if (!ReturnFits(PhaseRoute.Crew)) return PhaseResult.Space;
            if (!Same(root, session, state)) return PhaseResult.Changed;
            state.Route = PhaseRoute.Crew;
            state.InFlight = true;
            try { store.ModCash(-due, playSound: false); }
            catch (Exception error) { ModLog.Warn("运输井班组扣款中断：" + error.GetType().Name); }
            if (!Same(root, session, state)) return PhaseResult.Changed;
            long paid = (long)before - store.GetCash();
            if (paid < 0 || paid > due) return PhaseResult.Changed;
            state.CrewPaid += (int)paid;
            state.InFlight = false;
            if (state.CrewPaid == 0)
            {
                state.Route = PhaseRoute.None;
                return PhaseResult.Changed;
            }
            if (state.CrewPaid != PhaseSiteState.CrewFee) return PhaseResult.Partial;
            state.DispatchDay = day;
            return PhaseResult.Ready;
        }
        finally { CompanyTransaction.Exit(); }
    }

    internal static PhaseResult Dispatch(GameItem station, GameSlotInventory power, PhaseRoute route, int peak)
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return PhaseResult.Locked;
        try
        {
            var root = CorpState.Current;
            var session = StoreSession.Capture();
            var state = root?.Workshop?.PhaseSite;
            int day = StoreStation.GetDayCounter();
            if (route is not (PhaseRoute.Parts or PhaseRoute.Whole) || !PhaseRecoveryStation.HasPowerBay(station, power))
                return PhaseResult.StationMissing;
            var status = BeforeDeparture(root, session, state, day, route);
            if (status != PhaseResult.Ready) return status;
            int minimum = route == PhaseRoute.Whole ? PhaseSiteState.WholePower : PhaseSiteState.SplitPower;
            if (peak < minimum || peak is not (16 or 24 or 32)) return PhaseResult.Load;
            if (!WorkshopCycle.CanDrawPower(power, peak)) return PhaseResult.Power;
            if (!ReturnFits(route)) return PhaseResult.Space;
            if (!Same(root, session, state) || !PhaseRecoveryStation.HasPowerBay(station, power)) return PhaseResult.Changed;
            int id = station.GetUniqueID();
            if (id <= 0 || !WorkshopCycle.DrawForCurrentStock(power, peak, () =>
                Same(root, session, state) && station.uniqueId == id && PhaseRecoveryStation.HasPowerBay(station, power)))
                return PhaseResult.Changed;
            state.Route = route;
            state.DispatchDay = day;
            state.StationId = id;
            state.PowerSpent = peak;
            return PhaseResult.Ready;
        }
        finally { CompanyTransaction.Exit(); }
    }

    private static PhaseResult BeforeDeparture(CorpState root, StoreSession session, PhaseSiteState state, int day, PhaseRoute route)
    {
        if (!Current(root, session) || !WaterStationRepair.EngineeringCleared(root)) return PhaseResult.Locked;
        if (state == null) return PhaseResult.NeedSurvey;
        if (!state.Valid() || state.InFlight || day > int.MaxValue - 2) return PhaseResult.Changed;
        if (day <= state.SurveyDay) return PhaseResult.Surveying;
        return state.DispatchDay.HasValue || state.Route != PhaseRoute.None && state.Route != route ? PhaseResult.Used : PhaseResult.Ready;
    }

    internal static PhaseResult Claim()
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return PhaseResult.Locked;
        try
        {
            var root = CorpState.Current;
            var session = StoreSession.Capture();
            var state = root?.Workshop?.PhaseSite;
            if (!Current(root, session)) return PhaseResult.Locked;
            if (state == null) return PhaseResult.NeedSurvey;
            if (!state.Valid() || state.InFlight) return PhaseResult.Changed;
            if (state.CargoId.HasValue) return PhaseResult.Used;
            if (state.DueDay is not int due) return PhaseResult.NoDispatch;
            if (StoreStation.GetDayCounter() < due) return PhaseResult.EnRoute;
            if (!HasReturnStation(state)) return PhaseResult.StationMissing;
            if (!WorkshopCounter.HasRoom) return PhaseResult.Space;

            var cargo = CargoShell(state.Route);
            if (cargo == null) return PhaseResult.Changed;
            var staged = new List<GameItem>();
            try
            {
                if (!WorkshopCounter.CanFit(cargo)) return PhaseResult.Space;
                if (!Populate(root, state.Route, cargo, staged)) return PhaseResult.Changed;
                int id = cargo.GetUniqueID();
                if (id <= 0 || !Same(root, session, state) || !HasReturnStation(state)) return PhaseResult.Changed;
                bool delivered = WorkshopCounter.TryPlaceOnCounter(cargo);
                if (!Same(root, session, state)) return PhaseResult.Changed;
                if (!delivered && cargo.parentInventory == null) return PhaseResult.Space;
                state.CargoId = id;
                return delivered ? PhaseResult.Ready : PhaseResult.Changed;
            }
            finally
            {
                if (cargo.parentInventory == null)
                {
                    foreach (var part in staged) HookGuard.Run("撤回未交柜回收物", () => IndustrialWorkbench.Discard(part));
                    CompanyVisitors.DiscardUnshipped(cargo);
                }
            }
        }
        finally { CompanyTransaction.Exit(); }
    }

    private static bool HasReturnStation(PhaseSiteState state) => state.Route == PhaseRoute.Crew
        || EmporiumEntry.Instance?.GetAllOwnedItems(includeAfterhour: false)?.Any(item =>
            item?.uniqueId == state.StationId && PhaseRecoveryStation.Available(item)) == true;

    private static bool ReturnFits(PhaseRoute route)
    {
        if (!WorkshopCounter.HasRoom) return false;
        var probe = CargoShell(route);
        try { return probe != null && WorkshopCounter.CanFit(probe); }
        finally { CompanyVisitors.DiscardUnshipped(probe); }
    }

    private static GameItem CargoShell(PhaseRoute route) => route == PhaseRoute.Whole
        ? DirectoryMaster.Item("corp_presence_" + IndustrialPump.Key, isOwned: true) : Goods.Make("crate_meridian");

    private static bool Populate(CorpState root, PhaseRoute route, GameItem cargo, List<GameItem> staged)
    {
        if (route == PhaseRoute.Whole)
        {
            if (!IndustrialPump.PopulateRepairPump(cargo, staged)) return false;
            cargo.ModifyTag(RecoveredTag, tag => tag.Enable().SetString(root.RunId), skipValidate: true);
            Goods.Relabel(cargo, IndustrialPump.Key);
            return true;
        }
        var winding = IndustrialPump.MakeOldWinding();
        if (winding == null) return false;
        staged.Add(winding);
        var lining = DirectoryMaster.Item("corp_presence_spent_lining", isOwned: true);
        if (lining == null) return false;
        staged.Add(lining);
        return Crates.Put(cargo, winding) && Crates.Put(cargo, lining);
    }

    private static bool Current(CorpState root, StoreSession session) => session.IsCurrent
        && ReferenceEquals(CorpState.Current, root) && root?.Workshop != null && !string.IsNullOrWhiteSpace(root.RunId)
        && !StoreSession.CompanyProgressPaused && SaveSlot.CanWrite("CorpPresence");

    private static bool Same(CorpState root, StoreSession session, PhaseSiteState state) =>
        Current(root, session) && ReferenceEquals(root.Workshop.PhaseSite, state);
}
