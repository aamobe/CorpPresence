#if LOCAL_TEST_KIT
using System;
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

        var crates = new (string Key, Action<GameItem> Fill)[]
        {
            ("crate_carmicheal", FillCartel),
            ("crate_solana", FillLab),
            ("crate_meridian", FillMineral),
            ("crate_spacer", FillPowders),
            // Native junk and scrap occupy 3×2 each, so a 12×10 crate holds at most 20.
            ("crate_pastel", crate => AddNative(crate, "junk", 15)),
            ("crate_pastel", crate => AddNative(crate, "junk", 15)),
            ("crate_davre", crate => AddNative(crate, "scrap_metal", 12)),
            ("crate_davre", crate => AddNative(crate, "scrap_metal", 12))
        };
        var packed = new GameItem[crates.Length];
        bool delivered = false;
        try
        {
            for (int i = 0; i < crates.Length; i++)
            {
                packed[i] = Goods.Make(crates[i].Key);
                crates[i].Fill(packed[i]);
            }
            foreach (var crate in packed)
            {
                if (EscapeePlacement.TryPlace(shop, crate)) continue;
                ui.Notify("测试：请在主库存或后柜台腾出八个 3×2 的位置，再按 F3。");
                return;
            }
            delivered = true;
            ui.Notify("测试：已发放四箱产线物资和四箱破碎原料。");
        }
        catch (Exception error)
        {
            ui.Notify("测试包组装失败，请查看游戏日志。");
            ModLog.Warn("F3 测试包装箱失败：" + error.Message);
        }
        finally
        {
            if (!delivered)
            {
                foreach (var crate in packed) crate?.Destroy();
            }
        }
    }

    private static void FillCartel(GameItem crate)
    {
        Add(crate, CartelReactor.Key);
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

    private static void FillLab(GameItem crate)
    {
        AddNative(crate, "hydroponic");
        AddNative(crate, "blender");
        AddNative(crate, "heating_plate");
        AddNative(crate, "evaporator");
        AddNative(crate, "chem_finisher");
        AddNative(crate, "chem_scanner");
        AddNative(crate, "chem_module");
        AddNative(crate, "100ml_cylinder", 3);
        AddNative(crate, "water_jug", 2, WaterHelper.FillWithPureWater);
        AddNative(crate, "energy_credit_ext", 6);
    }

    private static void FillMineral(GameItem crate)
    {
        Add(crate, MineralCrusher.Key);
        Add(crate, PowderSieve.Key);
        Add(crate, SinteringStation.Key);
        Add(crate, CraftingStation.Key);
        Add(crate, CraftingStation.Blueprint);
        AddNative(crate, "energy_credit_ext", 6);
        AddNative(crate, "system_module_quality", 2);
        AddNative(crate, "system_module_performance", 2);
        AddNative(crate, "system_module_efficiancy", 2);
        AddNative(crate, "module_bay_expansion_kit", 6);
    }

    private static void FillPowders(GameItem crate)
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

    private static void AddPowder(GameItem crate, params int[] parts) =>
        AddItems(crate, PowderBox.Key, 1, () => Goods.Make(PowderBox.Key), item => PowderBox.WriteComposition(item, parts));

    private static void Add(GameItem crate, string key, int count = 1) =>
        AddItems(crate, key, count, () => Goods.Make(key), null);

    private static void AddNative(GameItem crate, string id, int count = 1, Action<GameItem> prepare = null) =>
        AddItems(crate, id, count, () => DirectoryMaster.Has<GameItem>(id) ? DirectoryMaster.Item(id, isOwned: true) : null, prepare);

    private static void AddItems(GameItem crate, string key, int count, Func<GameItem> create, Action<GameItem> prepare)
    {
        if (crate == null) throw new InvalidOperationException("找不到测试箱。");
        for (int i = 0; i < count; i++)
        {
            var item = create() ?? throw new InvalidOperationException("找不到物品 " + key);
            bool packed = false;
            try
            {
                GeneralHelper.SetItemOwned(item);
                prepare?.Invoke(item);
                packed = Crates.Put(crate, item);
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
