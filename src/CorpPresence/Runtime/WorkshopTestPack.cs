#if LOCAL_WORKSHOP_TEST
using System;
using System.Collections.Generic;

namespace CorpPresence.Runtime;

internal static class WorkshopTestPack
{
    internal const string Receipt = "CorpPresence.WorkshopPlaytestPack";
    internal static readonly RuntimeTask Grant = new("F5 三线工坊备料", Give,
        (name, error, _) =>
        {
            ModLog.Warn(name + "失败：" + error.Message);
            StoreUIManager.Instance?.Notify("备料发放中断。请查看游戏日志后再按 F5。");
        });

    private static void Give()
    {
        if (SaveSlot.CurrentStore?.runID != WorkshopTestRun.Id || !GameResources.Ready ||
            StoreSession.CompanyProgressPaused || Settings.CleanupForUninstall ||
            !SaveSlot.CanWrite("CorpPresence") || !SaveSlot.CanWrite(Receipt)) return;
        var shop = EmporiumEntry.Instance;
        var ui = StoreUIManager.Instance;
        var session = StoreSession.Capture();
        if (shop == null || ui == null || !session.IsCurrent || SaveManager.IsDecoding) return;
        if (SaveSlot.Read(Receipt) == WorkshopTestRun.Id)
        {
            ui.Notify("这份测试档已经领过四箱物资。");
            return;
        }

        var items = new List<GameItem>();
        bool delivered = false;
        try
        {
            var mineral = new Bay("F5 ① 矿粉回收", "设备和电池", "原料和粉盒", items);
            var cartel = new Bay("F5 ② 卡尔特", "设备和原料", "半成品与成品", items);
            var meridian = new Bay("F5 ③ 子午线", "锻造和原料", "筛具和部件", items);
            var orders = new Bay("F5 ④ 工单备料", "交货样品", "返修和装配", items);
            FillMineral(mineral);
            FillCartel(cartel);
            FillMeridian(meridian);
            FillOrders(orders);

            foreach (var bay in new[] { mineral, cartel, meridian, orders })
                if (!session.IsCurrent || !EscapeePlacement.TryPlace(shop, bay.Item))
                {
                    ui.Notify("请在主库存或后柜台腾出四只超科技储藏箱的位置，再按 F5。");
                    return;
                }
            if (!session.IsCurrent) return;
            SaveSlot.Write(Receipt, WorkshopTestRun.Id);
            delivered = true;
            ModLog.Info("F5 三线工坊备料已发放：4 箱，" + (items.Count - 4) + " 件物资。");
            ui.Notify("已发放四箱三线工坊物资，查看主库存和后柜台。");
        }
        finally
        {
            if (!delivered)
            {
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    try { items[i].Destroy(); }
                    catch (Exception error) { ModLog.Warn("撤回测试物品失败：" + error.Message); }
                }
            }
        }
    }

    private static void FillMineral(Bay bay)
    {
        foreach (string key in new[] { MineralCrusher.Key, PowderSieve.Key, SinteringStation.Key,
            PowderClassifier.Key, MineralGrowthStation.Key, CraftingStation.Key, CraftingStation.Blueprint,
            MineralGrowthBatch.OreBlueprint })
            bay.Goods(0, key);
        bay.Native(0, "energy_credit_ext", 6, Charge);
        foreach (string id in new[] { "system_module_quality", "system_module_performance", "system_module_efficiancy" })
            bay.Native(0, id, 3);

        bay.Native(1, "junk", 12);
        bay.Native(1, "scrap_metal", 12);
        bay.Native(1, "empty_beer_bottle", 12);
        bay.Native(1, "glass_shard", 8);
        bay.Goods(1, WorkshopMaterials.Electronics, 8);
        bay.Goods(1, WorkshopMaterials.Lining, 4);
        bay.Goods(1, WorkshopMaterials.Tile, 2);
        bay.Goods(1, PowderBox.Key, 8);
        bay.Powder(400, 0, 0, 0, 0);
        bay.Powder(0, 360, 0, 0, 0);
        bay.Powder(0, 0, 100, 0, 0);
        bay.Powder(0, 0, 0, 160, 0);
        bay.Powder(0, 0, 0, 0, 200);
        bay.Powder(120, 95, 25, 40, 120);
        bay.Goods(1, CraftingStation.SiliconPlate, 4);
        bay.Goods(1, CraftingStation.Substrate, 4);
    }

    private static void FillCartel(Bay bay)
    {
        bay.Goods(0, CartelReactor.Key);
        bay.Goods(0, AlcoholPreparationStation.Key);
        bay.Goods(0, MineralGrowthBatch.LightSaltBlueprint);
        bay.Native(0, "hydroponic");
        bay.Native(0, "energy_credit_ext", 4, Charge);
        bay.Native(0, "nutrifruit", 12);
        bay.Native(0, "water_jug", 3, WaterHelper.FillWithPureWater);
        bay.Native(0, "water_jug", 3);
        bay.Goods(0, "sleep_moss_seed", 3);
        bay.Goods(0, "sleep_moss_block", 20);
        bay.Goods(0, CartelReactor.Resin, 16);
        foreach (string key in new[] { CartelReactor.Salt, CartelReactor.Crystal, CartelReactor.Gel, ClassifierGoods.Crystal })
            bay.Goods(1, key, 8);
        foreach (string key in new[] { CartelReactor.Tablets, CartelReactor.Tins, CartelReactor.Ice })
            bay.Goods(1, key, 3);
    }

    private static void FillMeridian(Bay bay)
    {
        bay.Goods(0, "forge_furnace");
        bay.Goods(0, "forge_anvil");
        bay.Native(0, "energy_credit_ext", 4, Charge);
        bay.Native(0, "water_jug", 2, WaterHelper.FillWithPureWater);
        bay.Native(0, "metal_ingot", 12, i => IngotPurityHelper.SetPurity(i, IngotPurityHelper.INGOT_PURITY_HIGH));
        bay.Goods(0, "forged_billet", 8);
        bay.Goods(0, IndustrialGoods.Billet, 6);
        bay.Goods(0, IndustrialGoods.Alloy, 16);
        foreach (string key in new[] { ClassifierGoods.Screen, ClassifierGoods.Sleeve, IndustrialGoods.Pad })
            bay.Goods(1, key, 3);
        foreach (string key in new[] { ClassifierGoods.Coarse, ClassifierGoods.Fine, IndustrialGoods.Blend,
            IndustrialGoods.Contact, IndustrialGoods.SpentContact, IndustrialGoods.Blank, IndustrialGoods.Liner })
            bay.Goods(1, key, 8);
    }

    private static void FillOrders(Bay bay)
    {
        var cargo = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var kind in new[] { RotationKind.Joint, RotationKind.Expansion, RotationKind.Supplies, RotationKind.Repair, RotationKind.Joint })
            foreach (var truck in TruckSpec.For(kind))
                for (int i = 0; i < truck.ItemIds.Length; i++)
                {
                    string key = truck.ItemIds[i].Substring("corp_presence_".Length);
                    // The repair truck requires the actual filter shells stamped by the contract.
                    if (kind == RotationKind.Repair && key == IndustrialGoods.Unit) continue;
                    cargo.TryGetValue(key, out int amount);
                    cargo[key] = amount + truck.Quantities[i];
                }
        foreach (var (key, amount) in cargo)
            bay.Goods(0, key, amount, key == IndustrialGoods.Unit ? IndustrialGoods.MarkFresh : null);

        foreach (string key in new[] { IndustrialGoods.Blank, IndustrialGoods.Liner, CartelReactor.Gel })
            bay.Goods(1, key, 6);
        bay.Native(1, "module_bay_expansion_kit", 4);
        bay.Native(1, "water_jug", 2, i =>
        {
            // A spare jug of wash liquid keeps repair tests independent from alcohol production timing.
            WaterHelper.AddPureWater(i, 100);
            AlcoholPreparationStation.CommitAlcohol(i, 90_000);
            WaterFeatureHelper.InitWaterFeature(i);
            GeneralHelper.SetCustomName(i, "洗壳备用 · 90 mL 酒精");
        });
    }

    private static void Charge(GameItem item) => PowerHelper.SetPowerSourceAt(item, 250);

    private sealed class Bay
    {
        internal readonly GameItem Item;
        private readonly List<GameItem> _batch;
        private readonly List<GameInventory> _pages;

        internal Bay(string label, string firstPage, string secondPage, List<GameItem> batch)
        {
            _batch = batch;
            Item = Track(DirectoryMaster.Item("hypertech_bay", isOwned: true), "hypertech_bay");
            GeneralHelper.SetCustomName(Item, label);
            StorageExpansionHelper.SetTabName(Item, 0, firstPage);
            StorageExpansionHelper.SetTabName(Item, 1, secondPage);
            _pages = GraphUtils.FindChildrenType<GameInventory>(Compat.Cast<GraphNodeStorage>(Item));
            if (_pages.Count != 2 || _pages.Exists(page => page is not GameGridInventory))
                throw new InvalidOperationException("超科技储藏箱的两页库存未就绪。");
        }

        internal void Goods(int page, string key, int count = 1, Action<GameItem> prepare = null) =>
            Pack(page, key, count, () => CorpPresence.Goods.Make(key), prepare);

        internal void Native(int page, string id, int count = 1, Action<GameItem> prepare = null) =>
            Pack(page, id, count, () => DirectoryMaster.Item(id, isOwned: true), prepare);

        internal void Powder(params int[] parts) => Goods(1, PowderBox.Key, 1, i => PowderBox.WriteComposition(i, parts));

        private GameItem Track(GameItem item, string id)
        {
            if (item == null) throw new InvalidOperationException("找不到测试物品 " + id);
            _batch.Add(item);
            GeneralHelper.SetItemOwned(item);
            return item;
        }

        private void Pack(int page, string id, int count, Func<GameItem> create, Action<GameItem> prepare)
        {
            var inventory = _pages[page];
            for (int i = 0; i < count; i++)
            {
                var item = Track(create(), id);
                prepare?.Invoke(item);
                if (!ProductionOutput.WithSeparateUnit(item, () =>
                    {
                        GraphUtils.TryAcceptAll(inventory, item);
                        return ReferenceEquals(item.parentInventory, inventory);
                    }))
                    throw new InvalidOperationException("无法装箱 " + id);
            }
        }
    }
}
#endif
