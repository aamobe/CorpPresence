using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
namespace CorpPresence;

// Called on the game's main thread. Holds only bounded values and weak session references.
internal static class MaintenanceDiagnostics
{
    private static readonly List<MaintenanceFault> Faults = new();
    private static WeakReference<PlayerStore> _store;
    private static WeakReference<Dictionary<string, string>> _data;
    private static long _generation;
    private static bool _sessionKnown;

    private static bool Same<T>(WeakReference<T> weak, T value) where T : class =>
        weak == null ? value == null : weak.TryGetTarget(out var target) && ReferenceEquals(target, value);

    private static void CheckSession()
    {
        var store = SaveSlot.CurrentStore;
        var data = store?.modData;
        long generation = GameResources.Generation;
        if (_sessionKnown && Same(_store, store) && Same(_data, data) && _generation == generation) return;
        Faults.Clear();
        _store = store == null ? null : new WeakReference<PlayerStore>(store);
        _data = data == null ? null : new WeakReference<Dictionary<string, string>>(data);
        _generation = generation;
        _sessionKnown = true;
    }

    internal static void Reset()
    {
        Faults.Clear(); _store = null; _data = null; _sessionKnown = false;
    }

    private static string Bound(string value, int limit) => string.IsNullOrEmpty(value) ? "unknown" : value.Length <= limit ? value : value.Substring(0, limit);

    internal static void Record(string source, string stage, string code, Exception error)
    {
        try
        {
            CheckSession();
            source = Bound(source, 128); stage = Bound(stage, 128); code = Bound(code, 128);
            string type = error == null ? "none" : Bound(error.GetType().FullName, 256);
            for (int i = 0; i < Faults.Count; i++)
            {
                var previous = Faults[i];
                if (previous.Source == source && previous.Stage == stage && previous.Code == code && previous.ExceptionType == type)
                { Faults[i] = previous.Repeated(); return; }
            }
            var detail = new StringBuilder();
            if (error != null)
            {
                // No Message, Data, file names, line numbers, or retained Exception object.
                var trace = new StackTrace(error, false);
                for (int i = 0; i < trace.FrameCount && detail.Length < 4096; i++)
                {
                    var method = trace.GetFrame(i)?.GetMethod();
                    if (method != null) detail.Append(Bound(method.DeclaringType?.FullName, 256)).Append('.').Append(Bound(method.Name, 128)).Append(';');
                }
            }
            if (Faults.Count == 64) Faults.RemoveAt(0);
            Faults.Add(new MaintenanceFault(source, stage, code, type, detail.ToString(0, Math.Min(detail.Length, 4096))));
        }
        catch { /* Diagnostics must not escape into the business operation. */ }
    }

    internal static MaintenanceSnapshot Capture()
    {
        try
        {
            CheckSession();
            var store = SaveSlot.CurrentStore;
            var data = store?.modData;
            var state = CorpState.Current;
            long generation = GameResources.Generation;
            bool ready = GameResources.Ready;
            var status = store == null ? SaveDiagnosticStatus.NoStore : state == null ? SaveDiagnosticStatus.NotLoaded
                : state.IsBoundReadonly(store) ? SaveDiagnosticStatus.Bound : SaveDiagnosticStatus.Stale;
            bool? corp = store == null ? (bool?)null : !SaveSlot.CanWrite("CorpPresence");
            bool? malice = store == null ? (bool?)null : !SaveSlot.CanWrite("CorpPresence.Malice");
            int? orders = null; long? units = null;
            if (status == SaveDiagnosticStatus.Bound && corp == false)
            {
                int count = 0; long remaining = 0;
                foreach (var company in state.Companies.Values)
                    foreach (var order in company.Orders)
                    { count = checked(count + 1); remaining = checked(remaining + order.Items.Count); }
                orders = count; units = remaining;
            }
            var counters = new List<MaintenanceCounter>();
            Portraits.AppendDiagnostics(counters);
            CatalogPricing.AppendDiagnostics(counters);
            ItemRegistry.AppendDiagnostics(counters);
            var registrations = new List<MaintenanceRegistration>();
            foreach (var owner in Ext.BeforeSave.Owners) registrations.Add(new MaintenanceRegistration("BeforeSave", owner));
            foreach (var owner in Ext.DayStart.Owners) registrations.Add(new MaintenanceRegistration("DayStart", owner));
            foreach (var owner in Ext.StateReset.Owners) registrations.Add(new MaintenanceRegistration("StateReset", owner));
            var modules = new[] { new MaintenanceModuleState("malice", Malice.Malice.IsInstalled, Malice.Malice.LastReleaseFailed) };
            if (!ReferenceEquals(store, SaveSlot.CurrentStore) || !ReferenceEquals(data, store?.modData)
                || generation != GameResources.Generation || ready != GameResources.Ready || !ReferenceEquals(state, CorpState.Current)
                || (status == SaveDiagnosticStatus.Bound && !state.IsBoundReadonly(store)))
            { CheckSession(); return Unknown(); }
            return new MaintenanceSnapshot(Main.Version, ready, generation, status, corp, malice, orders, units,
                counters, Faults, registrations, modules);
        }
        catch { return Unknown(); }
    }

    private static MaintenanceSnapshot Unknown() => new(Main.Version, GameResources.Ready, GameResources.Generation,
        SaveDiagnosticStatus.Unstable, null, null, null, null, Array.Empty<MaintenanceCounter>(),
        Array.Empty<MaintenanceFault>(), Array.Empty<MaintenanceRegistration>(), Array.Empty<MaintenanceModuleState>());
}
