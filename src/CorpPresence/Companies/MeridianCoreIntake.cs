using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class MeridianCoreIntake
{
    internal const string TimingFeed = "spent_timing_controller", PhaseFeed = "retired_phase_locator";
    internal const string TimingCore = "verified_timing_core", PhaseCore = "verified_phase_core";
    internal const string OriginTag = "CORP_CORE_ORIGIN_RUN";

    internal static string Feed(IndustrialCoreKind kind) => kind == IndustrialCoreKind.Timing ? TimingFeed : PhaseFeed;
    internal static string Product(IndustrialCoreKind kind) => kind == IndustrialCoreKind.Timing ? TimingCore : PhaseCore;

    internal static readonly Dictionary<string, Goods.Def> Definitions = new()
    {
        [TimingFeed] = Stock("旧时序控制器", "Old Timing Controller", 0, "wire", "spent-timing-controller"),
        [PhaseFeed] = Stock("运输井定位组件", "Shaft Locator Assembly", 0, "steel_sheet", "retired-phase-locator"),
        [TimingCore] = Stock("已校时序核心", "Verified Timing Core", 210, "wire", "verified-timing-core"),
        [PhaseCore] = Stock("已校相位核心", "Verified Phase Core", 250, "steel_sheet", "verified-phase-core")
    };

    private static Goods.Def Stock(string name, string english, int value, string fallback, string art) =>
        new()
        {
            Name = name, NameEn = english, Value = value, Company = "mineral", Art = art, Fallback = fallback,
            W = 2, H = 1, Shape = "##", Types = new[] { "MATERIAL" },
            Shell = () => ItemDirectory.CreateEmptyItem(), Setup = item => item.mayStackItemFunc = null
        };

    internal static bool IsIssued(CorpState root, GameItem item, IndustrialCoreKind kind) =>
        item?.identifier == "corp_presence_" + Feed(kind) && item.unitCount == 1 && item.uniqueId > 0
        && GeneralHelper.IsItemOwned(item) && !StorePlacement.Destroyed(item) && root?.Workshop?.CoreIntake?.Id(kind) == item.uniqueId
        && !string.IsNullOrEmpty(root.RunId) && item.GetTagReadonly(OriginTag).GetString() == root.RunId;

    internal static SaleStatus Claim(IndustrialCoreKind kind)
    {
        if (!CompanyTransaction.TryEnter(SaveSlot.CurrentStore)) return SaleStatus.Unavailable;
        try
        {
            var root = CorpState.Current;
            var session = StoreSession.Capture();
            if (!Current(root, session) || !WaterStationRepair.EngineeringCleared(root)) return SaleStatus.Unavailable;
            var before = root.Workshop.CoreIntake;
            if (before != null && !before.Valid()) return SaleStatus.Frozen;
            if (before?.Id(kind) != null) return SaleStatus.TrialSettled;
            if (!WorkshopCounter.HasRoom) return SaleStatus.Short;
            var item = DirectoryMaster.Item("corp_presence_" + Feed(kind), isOwned: true);
            if (item == null) return SaleStatus.Unavailable;
            try
            {
                int id = item.GetUniqueID();
                if (id <= 0 || before?.Id(kind == IndustrialCoreKind.Timing ? IndustrialCoreKind.Phase : IndustrialCoreKind.Timing) == id)
                    return SaleStatus.Changed;
                item.ModifyTag(OriginTag, tag => tag.Enable().SetString(root.RunId), skipValidate: true);
                bool placed = WorkshopCounter.TryPlaceOnCounter(item);
                if (!Current(root, session) || !ReferenceEquals(before, root.Workshop.CoreIntake)) return SaleStatus.Changed;
                if (!placed && item.parentInventory == null) return SaleStatus.Short;
                var receipt = before ??= new CoreIntakeState();
                receipt.Set(kind, id);
                root.Workshop.CoreIntake = receipt;
                return placed ? SaleStatus.Ready : SaleStatus.Changed;
            }
            finally { CompanyVisitors.DiscardUnshipped(item); }
        }
        finally { CompanyTransaction.Exit(); }
    }

    private static bool Current(CorpState root, StoreSession session) => session.IsCurrent
        && ReferenceEquals(CorpState.Current, root) && root?.Workshop != null && !string.IsNullOrEmpty(root.RunId)
        && !StoreSession.CompanyProgressPaused && SaveSlot.CanWrite("CorpPresence");
}
