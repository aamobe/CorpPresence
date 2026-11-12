using System;
using System.Collections.Generic;
namespace CorpPresence;
internal enum SaveDiagnosticStatus { NoStore, NotLoaded, Bound, Stale, Unstable }
internal sealed class MaintenanceModuleState
{
    public string Owner { get; }
    public bool Installed { get; }
    public bool ReleaseFailed { get; }
    public MaintenanceModuleState(string owner, bool installed, bool releaseFailed)
    { Owner = owner; Installed = installed; ReleaseFailed = releaseFailed; }
}
internal sealed class MaintenanceCounter
{
    public string Name { get; }
    public long Value { get; }
    public MaintenanceCounter(string name, long value) { Name = name; Value = value; }
}
internal sealed class MaintenanceRegistration
{
    public string EventName { get; }
    public string Owner { get; }
    public MaintenanceRegistration(string eventName, string owner) { EventName = eventName; Owner = owner; }
}
internal enum MaintenanceFaultScope { Session, Runtime }
internal sealed class MaintenanceFault
{
    public MaintenanceFaultScope Scope { get; }
    public string Source { get; }
    public string Stage { get; }
    public string Code { get; }
    public string ExceptionType { get; }
    public string Detail { get; }
    public string ItemId { get; }
    public int Count { get; }
    public MaintenanceFault(string source, string stage, string code, string type, string detail, int count = 1, MaintenanceFaultScope scope = MaintenanceFaultScope.Session, string itemId = null)
    { Scope = scope; Source = source; Stage = stage; Code = code; ExceptionType = type; Detail = detail; Count = count; ItemId = itemId; }
    internal static int Increment(int value) => value == int.MaxValue ? value : value + 1;
    internal MaintenanceFault Repeated() => new(Source, Stage, Code, ExceptionType, Detail, Increment(Count), Scope, ItemId);
}
internal sealed class MaintenanceSnapshot
{
    public string InventoryLoadReport { get; }
    public bool CompanyProgressPaused { get; }
    public string Version { get; }
    public bool ResourcesReady { get; }
    public long Generation { get; }
    public SaveDiagnosticStatus SaveStatus { get; }
    public bool? CorpWriteProtected { get; }
    public bool? MaliceWriteProtected { get; }
    public int? PendingOrders { get; }
    public long? PendingItemUnits { get; }
    public IReadOnlyList<MaintenanceCounter> Counters { get; }
    public IReadOnlyList<MaintenanceFault> Faults { get; }
    public IReadOnlyList<MaintenanceRegistration> Registrations { get; }
    public IReadOnlyList<MaintenanceModuleState> Modules { get; }
    public MaintenanceSnapshot(string version, bool ready, long generation, SaveDiagnosticStatus status,
        bool? corpProtected, bool? maliceProtected, int? orders, long? units,
        IEnumerable<MaintenanceCounter> counters, IEnumerable<MaintenanceFault> faults,
        IEnumerable<MaintenanceRegistration> registrations, IEnumerable<MaintenanceModuleState> modules, string inventoryLoadReport = null, bool companyProgressPaused = false)
    {
        InventoryLoadReport = inventoryLoadReport;
        CompanyProgressPaused = companyProgressPaused;
        Version = version; ResourcesReady = ready; Generation = generation; SaveStatus = status;
        CorpWriteProtected = corpProtected; MaliceWriteProtected = maliceProtected;
        PendingOrders = orders; PendingItemUnits = units;
        Counters = new List<MaintenanceCounter>(counters).AsReadOnly();
        Faults = new List<MaintenanceFault>(faults).AsReadOnly();
        Registrations = new List<MaintenanceRegistration>(registrations).AsReadOnly();
        Modules = new List<MaintenanceModuleState>(modules).AsReadOnly();
    }
}
