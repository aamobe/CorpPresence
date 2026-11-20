#if LOCAL_TEST_KIT
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorpPresence.Runtime;

internal static class TestKit
{
    private static readonly RuntimeTask Grant = new("F3 产线测试包", Give,
        (name, error, _) => ModLog.Warn(name + "失败：" + error.Message));

    internal static void Tick()
    {
        if (Input.GetKeyDown(KeyCode.F3)) Grant.Run(Time.unscaledTime);
    }

    private static void Give()
    {
        var ui = StoreUIManager.Instance;
        var shop = EmporiumEntry.Instance;
        if (ui == null || shop == null || !GameResources.Ready || Settings.CleanupForUninstall
            || !StoreSession.Capture().IsCurrent || !SaveSlot.CanWrite("CorpPresence")) return;

        var crates = new (string Key, Action<Packing> Fill)[]
        {
            ("crate_carmicheal", FillCartel),
            ("crate_solana", FillLab),
            ("crate_meridian", FillMineral),
            ("crate_meridian", crate => AddNative(crate, "module_bay_expansion_kit", 6)),
            ("crate_spacer", FillPowders),
            ("crate_spacer", crate => AddNative(crate, "water_jug", 4, WaterHelper.FillWithPureWater)),
            // Native junk and scrap occupy 3×2 each, so a 12×10 crate holds at most 20.
            ("crate_pastel", crate => AddNative(crate, "junk", 15)),
            ("crate_pastel", crate => AddNative(crate, "junk", 15)),
            ("crate_davre", crate => AddNative(crate, "scrap_metal", 12)),
            ("crate_davre", crate => AddNative(crate, "scrap_metal", 12))
        };
        var packed = new List<GameItem>();
        bool delivered = false;
        string packing = null;
        try
        {
            for (int i = 0; i < crates.Length; i++)
            {
                packing = crates[i].Key;
                crates[i].Fill(new Packing(packing, packed));
            }
            for (int i = 0; i < packed.Count; i++)
            {
                var crate = packed[i];
                ModLog.Info($"F3 装箱通过 {i + 1}/{packed.Count}：{Goods.IdOf(crate)}，箱内对象 {Crates.InnerOf(crate).childItems.Count}");
            }
            packing = null;
            foreach (var crate in packed)
            {
                if (EscapeePlacement.TryPlace(shop, crate)) continue;
                ui.Notify($"测试：请在主库存或后柜台腾出 {packed.Count} 个 3×2 的位置，再按 F3。");
                return;
            }
            delivered = true;
            ModLog.Info($"F3 发放成功：{packed.Count} 箱。");
            ui.Notify($"测试：已发放 {packed.Count} 箱测试物资。");
        }
        catch (Exception error)
        {
            ui.Notify("测试包组装失败，请查看游戏日志。");
            ModLog.Warn($"F3 测试包发放失败（{packing ?? "放置"}）：{error.Message}");
        }
        finally
        {
            if (!delivered)
            {
                foreach (var crate in packed) crate?.Destroy();
            }
        }
    }

    private static void FillCartel(Packing crate)
    {
        Add(crate, CartelReactor.Key);
        Add(crate, AlcoholPreparationStation.Key);
        AddNative(crate, "energy_credit_ext", 2);
        AddNative(crate, "nutrifruit", 4);
        Add(crate, "sleep_moss_seed", 3);
        Add(crate, "sleep_moss_block", 16);
        Add(crate, CartelReactor.Resin, 12);
        Add(crate, CartelReactor.Salt, 16);
        Add(crate, CartelReactor.Gel, 8);
        Add(crate, CartelReactor.Crystal, 8);
        Add(crate, CartelReactor.Tablets, 3);
        Add(crate, CartelReactor.Tins, 3);
        Add(crate, CartelReactor.Ice, 3);
        AddNative(crate, "turbo_booster_hypertech", prepare: item =>
        {
            item.ModifyTag("CURRENT_CHARGE_TAG", state => state.Enable().SetInt(100 * 15));
            MachineTurboBoosterAdv.RefreshHypertechReady(item);
        });
    }

    private static void FillLab(Packing crate)
    {
        AddNative(crate, "hydroponic");
        AddNative(crate, "blender");
        AddNative(crate, "heating_plate");
        AddNative(crate, "evaporator");
        AddNative(crate, "chem_finisher");
        AddNative(crate, "chem_scanner");
        AddNative(crate, "chem_module");
        AddNative(crate, "100ml_cylinder", 3);
        AddNative(crate, "energy_credit_ext", 6);
    }

    private static void FillMineral(Packing crate)
    {
        Add(crate, MineralCrusher.Key);
        Add(crate, PowderSieve.Key);
        Add(crate, SinteringStation.Key);
        Add(crate, CraftingStation.Key);
        Add(crate, CraftingStation.Blueprint);
        Add(crate, MineralGrowthStation.Key);
        Add(crate, MineralGrowthBatch.OreBlueprint);
        Add(crate, MineralGrowthBatch.LightSaltBlueprint);
        AddNative(crate, "energy_credit_ext", 6);
        AddNative(crate, "system_module_quality", 2);
        AddNative(crate, "system_module_performance", 2);
        AddNative(crate, "system_module_efficiancy", 2);
    }

    private static void FillPowders(Packing crate)
    {
        Add(crate, PowderBox.Key, 2);
        AddPowder(crate, 400, 0, 0, 0, 0);
        AddPowder(crate, 188, 6, 0, 0, 6);
        AddPowder(crate, 164, 16, 0, 0, 20);
        AddPowder(crate, 0, 180, 0, 0, 0);
        AddPowder(crate, 0, 0, 80, 0, 0);
        AddPowder(crate, 0, 0, 0, 120, 0);
        AddPowder(crate, 0, 0, 0, 0, 120);
        AddPowder(crate, 130, 95, 18, 40, 117);
    }

    private static void AddPowder(Packing crate, params int[] parts) =>
        AddItems(crate, PowderBox.Key, 1, () => Goods.Make(PowderBox.Key), item => PowderBox.WriteComposition(item, parts));

    private static void Add(Packing crate, string key, int count = 1) =>
        AddItems(crate, key, count, () => Goods.Make(key), null);

    private static void AddNative(Packing crate, string id, int count = 1, Action<GameItem> prepare = null) =>
        AddItems(crate, id, count, () => DirectoryMaster.Has<GameItem>(id) ? DirectoryMaster.Item(id, isOwned: true) : null, prepare);

    private sealed class Packing
    {
        private readonly string _key;
        private readonly List<GameItem> _crates;
        private GameItem _current;

        internal Packing(string key, List<GameItem> crates)
        {
            _key = key;
            _crates = crates;
        }

        internal bool Put(GameItem item)
        {
            if (_current == null || !HasRoom(_current, item))
            {
                _current = Goods.Make(_key) ?? throw new InvalidOperationException("找不到测试箱。");
                _crates.Add(_current);
                GeneralHelper.SetItemOwned(_current);
            }
            return HasRoom(_current, item) && Crates.Put(_current, item);
        }

        private static bool HasRoom(GameItem crate, GameItem item)
        {
            var inventory = Crates.InnerOf(crate);
            var previous = CrateFillGate.Enter(inventory);
            try { return GraphUtils.CanAccept(inventory, item); }
            finally { CrateFillGate.Restore(previous); }
        }
    }

    private static void AddItems(Packing crate, string key, int count, Func<GameItem> create, Action<GameItem> prepare)
    {
        for (int i = 0; i < count; i++)
        {
            var item = create() ?? throw new InvalidOperationException("找不到物品 " + key);
            bool packed = false;
            try
            {
                GeneralHelper.SetItemOwned(item);
                prepare?.Invoke(item);
                packed = crate.Put(item);
                if (!packed) throw new InvalidOperationException("装不下 " + key);
            }
            finally
            {
                if (!packed && item.parentInventory == null && !(item is GameItemElement element && element.IsDestroyed()))
                    item.Destroy();
            }
        }
    }
}
#endif
