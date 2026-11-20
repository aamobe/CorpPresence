using System;
using CorpPresence.UI;

namespace CorpPresence;

internal static class CartelReactor
{
    internal const string Key = "cartel_reactor";
    private const int BasePower = 8;
    internal const string Resin = "blue_shell_resin";
    internal const string Salt = "light_salt_core";
    internal const string Gel = "sleep_moss_gel";
    internal const string Crystal = "light_salt_precipitate";
    internal const string Tablets = "zol_tablets";
    internal const string Tins = "nicotine_tin";
    internal const string Ice = "ice_shard_pouch";

    internal static readonly DefinitionSet<Goods.Def> Defs = new("CartelReactor")
    {
        [Resin] = Define("蓝壳树脂", "Blue Shell Resin", "blue-shell-resin", 8),
        [Salt] = Define("光盐芯", "Light Salt Core", "light-salt-core", 8),
        [Gel] = Define("眠苔胶", "Sleep Moss Gel", "sleep-moss-gel", 20),
        [Crystal] = Define("光盐析晶", "Light Salt Precipitate", "light-salt-precipitate", 20),
        [Tablets] = Define("唑眠片", "Zol Tablets", "zol-tablets", 25),
        [Tins] = Define("尼古袋", "Nico Pouches", "nicotine-tin", 35),
        [Ice] = Define("冰棱", "Ice Shards", "ice-shard-pouch", 65),
        [Key] = new Goods.Def
        {
            Name = "双胆反应槽", NameEn = "Twin-Vessel Reactor", Company = "cartel",
            Art = "twin-vessel-reactor", Fallback = "hydroponic", Value = 300,
            W = 4, H = 3, Types = new[] { "MACHINE" }, Shell = Build
        }
    };

    private static Goods.Def Define(string zh, string en, string art, int value) => new()
    {
        Name = zh, NameEn = en, Company = "cartel", Art = art, Fallback = "dream_cap",
        Value = value, W = 1, H = 1, Shape = art == "light-salt-core" ? "#" : null, Types = new[] { "CHEMICAL_SUPPLIES" },
        Shell = () => ItemDirectory.CreateEmptyItem(), Setup = item => CartelChemistry.Initialize(item, art)
    };

    private static string Id(string key) => "corp_presence_" + key;

    private static bool Matches(GameItem item, string id) =>
        item != null && item.identifier == id && item.unitCount > 0 && GeneralHelper.IsItemOwned(item);

    private static GameGridInventory Feed(string id) => new(9, 6)
    {
        mayInventoryAddItemFunc = (item, _) => Matches(item, id)
    };

    private static GameGridInventory Output(int width, int height = 1) => new(width, height)
    {
        mayInventoryAddItemFunc = (_, _) => false
    };

    // Both machines appended their battery inventory to preserve saved slot indices.
    internal static void AfterGetBatterySlot(GameItem machine, ref GameSlotInventory __result)
    {
        if (__result != null || machine?.identifier is not ("corp_presence_" + Key or "corp_presence_" + AlcoholPreparationStation.Key)
            || machine.contentWindow?.childElement is not MachineryPanelRow row) return;
        var storage = new System.Collections.Generic.List<GraphNodeStorage>();
        row.GetPanel("power")?.CollectStorage(storage);
        if (storage.Count == 1) __result = storage[0] as GameSlotInventory;
    }

    private static GameItem Build()
    {
        const string blueTag = "CORP_CARTEL_BLUE_BATCH";
        const string whiteTag = "CORP_CARTEL_WHITE_BATCH";
        var battery = new GameSlotInventory();
        var moss = Feed(SleepMoss.BlockId);
        var resin = Feed(Id(Resin));
        var salt = Feed(Id(Salt));
        var blueOutput = Output(9, 6);
        var whiteOutput = Output(9, 6);
        var refine = Output(3);
        var products = Output(2);
        var collect = new MachineryButton(L.K("production.reactor.collect", "取回旧批次", "Collect old batch"));
        var status = new MachineryText(uppercase: false);

        var oldInputs = MachineHelper.Panel(L.T("取回旧原料", "Retrieve old inputs"), refine, "refine").SetHidden(true);
        var oldProducts = MachineHelper.Panel(L.T("取回旧成品", "Retrieve old products"), products, "output").SetHidden(true);
        var oldBatches = MachineHelper.Panel(L.K("production.reactor.oldBatches", "旧批次", "Old batches"), collect, "legacy_batches").SetHidden(true);
        var window = new PixelWindow(true, null, WindowStyle.Machinery);
        window.Attach(new MachineryPanelRow(
            MachineHelper.Panel(L.T("菌块", "Moss"), moss, "moss"),
            MachineHelper.Panel(L.T("树脂", "Resin"), resin, "resin"),
            MachineHelper.Panel(L.T("盐芯", "Salt core"), salt, "salt"),
            MachineHelper.Panel(L.T("蓝胆出料", "Blue output"), blueOutput, "blue_output"),
            MachineHelper.Panel(L.T("白胆出料", "White output"), whiteOutput, "white_output"),
            oldInputs,
            oldProducts,
            MachineHelper.Panel(L.T("反应", "Reaction"), status, "reaction"),
            MachineHelper.PowerPanel(battery), oldBatches)
            .SetColumns(3).SetDisplayOrder(8, 1, 2, 0, 3, 4, 7, 9, 5, 6));

        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        MachineHelper.SetupBatterySlot(battery, machine);
        MachineryHelper.InitMachinery(machine, BasePower);
        int State(string tag) => machine.GetTagReadonly(tag).GetInt();
        void SetState(string tag, int value) => machine.ModifyTag(tag, state => state.Enable().SetInt(value));
        string VesselStatus(int state, bool hasMoss, GameGridInventory feed, string reagent, GameGridInventory output, string product)
        {
            if (state == 0)
            {
                if (!hasMoss) return L.K("production.reactor.needMoss", "缺眠苔菌块", "Needs moss block");
                if (!feed.childItems.Exists(item => Matches(item, Id(reagent))))
                    return reagent == Resin ? L.K("production.reactor.needResin", "缺蓝壳树脂", "Needs blue shell resin")
                        : L.K("production.reactor.needSalt", "缺光盐芯", "Needs light salt core");
            }
            if (!MachineAppearance.CanFit(output, Id(product))) return L.K("production.reactor.outputFull", "清出出料格", "Clear output space");
            if (state == 2) return L.K("production.reactor.collectible", "旧批次可取", "Old batch ready to collect");
            if (state == 1) return L.K("production.reactor.legacyOvernight", "旧批次待过夜", "Old batch pending overnight");
            return L.K("production.reactor.loaded", "原料已齐", "Ingredients ready");
        }
        void Refresh()
        {
            oldInputs.SetHidden(refine.childItems.Count == 0);
            oldProducts.SetHidden(products.childItems.Count == 0);
            int power = MachineryHelper.GetMachinePowerUsage(machine);
            string readiness = MachineHelper.CanPower(machine, battery)
                ? L.T("电池电量充足", "Battery ready")
                : L.T("需装入有足够电量的电池", "Needs a charged battery");
            int blueState = State(blueTag), whiteState = State(whiteTag);
            oldBatches.SetHidden(blueState != 2 && whiteState != 2);
            bool hasMoss = moss.childItems.Exists(item => Matches(item, SleepMoss.BlockId));
            string blue = VesselStatus(blueState, hasMoss, resin, Resin, blueOutput, Gel);
            string white = VesselStatus(whiteState, hasMoss, salt, Salt, whiteOutput, Crystal);
            status.SetText(L.F("production.reactor.status", "{0} · 每批 {1} 电\n蓝胆：{2}\n白胆：{3}\n每胆每夜一批，各用一块菌块和一份辅料。\n菌块或电只够一批时，蓝胆优先。", "{0} · {1} charge/batch\nBlue: {2}\nWhite: {3}\nEach vessel uses one moss and one reagent per night.\nBlue goes first when moss or power is short.",
                readiness, power, blue, white));
        }
        bool Ready() => GeneralHelper.IsItemOwned(machine) && SaveSlot.CanWrite("CorpPresence");

        bool DrawPower() => MachineHelper.TryDrawCyclePower(machine, battery);

        void Produce(string tag, GameGridInventory ingredient, string id, string key, GameGridInventory output)
        {
            if (State(tag) != 0 || !MachineHelper.CanPower(machine, battery)) return;
            var block = moss.childItems.Find(item => Matches(item, SleepMoss.BlockId));
            var extra = ingredient.childItems.Find(item => Matches(item, Id(id)));
            if (block == null || extra == null || !Place(output, key, DrawPower)) return;
            Consume(block);
            Consume(extra);
        }

        void Collect(string tag, int state, string key, GameGridInventory output, bool overnight)
        {
            int advanced = overnight ? CartelBatch.Advance(state) : state;
            if (advanced != 2) return;
            if (Place(output, key, state == 1 ? DrawPower : null))
                SetState(tag, CartelBatch.AfterOutput(advanced, true));
        }

        void Settle(bool overnight)
        {
            if (!Ready()) return;
            int blueState = State(blueTag);
            int whiteState = State(whiteTag);
            Collect(blueTag, blueState, Gel, blueOutput, overnight);
            if (overnight && blueState == 0) Produce(blueTag, resin, Resin, Gel, blueOutput);
            Collect(whiteTag, whiteState, Crystal, whiteOutput, overnight);
            if (overnight && whiteState == 0) Produce(whiteTag, salt, Salt, Crystal, whiteOutput);
            Refresh();
        }

        collect.SetCallback(() => HookGuard.Run("反应槽取料", () => Settle(false)));
        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run("反应槽过夜", () => Settle(true));
        refine.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        products.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        foreach (var inventory in new GameInventory[] { battery, moss, resin, salt, blueOutput, whiteOutput })
        {
            inventory.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotChangeAmountItemFunc += (_, _, _) => Refresh();
        }
        window.openCallback += Refresh;
        return machine;
    }

    private static void Consume(GameItem item)
    {
        if (item.unitCount == 1) item.Destroy();
        else item.SetUnitCount(item.unitCount - 1);
    }

    private static bool Place(GameGridInventory output, string key, Func<bool> drawPower)
    {
        var item = Goods.Make(key);
        if (item == null) return false;

        bool committed = false;
        try
        {
            GeneralHelper.SetItemOwned(item);
            committed = ProductionOutput.TryAdd(output, item) && (drawPower == null || drawPower());
            return committed;
        }
        finally
        {
            if (!committed && (item is not GameItemElement element || !element.IsDestroyed())) item.Destroy();
        }
    }
}
