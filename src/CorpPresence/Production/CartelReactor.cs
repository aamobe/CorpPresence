using System;
using System.Collections.Generic;
using CorpPresence.UI;

namespace CorpPresence;

internal static class CartelReactor
{
    internal const string Key = "cartel_reactor";
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
        [Tablets] = Define("唑眠片", "Zol Tablets", "zol-tablets", 14),
        [Tins] = Define("尼古袋", "Nico Pouches", "nicotine-tin", 29),
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
        Value = value, W = 1, H = 1, Types = new[] { "CHEMICAL_SUPPLIES" },
        Shell = () => ItemDirectory.CreateEmptyItem(), Setup = item => CartelChemistry.Initialize(item, art)
    };

    private static string Id(string key) => "corp_presence_" + key;

    private static bool Matches(GameItem item, string id) =>
        item != null && item.identifier == id && item.unitCount > 0 && GeneralHelper.IsItemOwned(item);

    private static GameSlotInventory Feed(string id) => new()
    {
        mayInventoryAddItemFunc = (item, _) => Matches(item, id)
    };

    private static GameGridInventory Output(int width) => new(width, 1)
    {
        mayInventoryAddItemFunc = (_, _) => false
    };

    private static GameItem Build()
    {
        const string blueTag = "CORP_CARTEL_BLUE_BATCH";
        const string whiteTag = "CORP_CARTEL_WHITE_BATCH";
        var moss = Feed(SleepMoss.BlockId);
        var resin = Feed(Id(Resin));
        var salt = Feed(Id(Salt));
        var blueOutput = Output(1);
        var whiteOutput = Output(1);
        var refine = Output(3);
        var products = Output(2);
        var blue = new MachineryButton(L.T("启动蓝胆", "Start blue vessel"));
        var white = new MachineryButton(L.T("启动白胆", "Start white vessel"));
        var collect = new MachineryButton(L.T("尝试取料", "Collect batches"));
        var status = new MachineryText(uppercase: false);

        var window = MachineHelper.CreateMachineryWindow(true,
            MachineHelper.Panel(L.T("菌块", "Moss"), moss, "moss"),
            MachineHelper.Panel(L.T("树脂", "Resin"), resin, "resin"),
            MachineHelper.Panel(L.T("盐芯", "Salt core"), salt, "salt"),
            MachineHelper.Panel(L.T("蓝胆出料", "Blue output"), blueOutput, "blue_output"),
            MachineHelper.Panel(L.T("白胆出料", "White output"), whiteOutput, "white_output"),
            MachineHelper.Panel(L.T("取回旧原料", "Retrieve old inputs"), refine, "refine"),
            MachineHelper.Panel(L.T("取回旧成品", "Retrieve old products"), products, "output"),
            MachineHelper.Panel(L.T("反应", "Reaction"), MachineryStack.Vertical(blue, white, collect, status), "reaction"));

        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");
        int State(string tag) => machine.GetTagReadonly(tag).GetInt();
        void SetState(string tag, int value) => machine.ModifyTag(tag, state => state.Enable().SetInt(value));
        string Label(int state) => state switch
        {
            1 => L.T("反应中", "Reacting"),
            2 => L.T("待出料", "Ready to collect"),
            _ => L.T("空闲", "Idle")
        };
        void Refresh() => status.SetText(Label(State(blueTag)) + " / " + Label(State(whiteTag)));
        bool Ready() => GeneralHelper.IsItemOwned(machine) && SaveSlot.CanWrite("CorpPresence");
        void Note(string zh, string en) => StoreUIManager.Instance?.Notify(L.T(zh, en));

        void Start(string tag, GameSlotInventory ingredient, string id)
        {
            if (!Ready() || State(tag) != 0) return;
            var block = moss.childItem;
            var extra = ingredient.childItem;
            if (!Matches(block, SleepMoss.BlockId) || !Matches(extra, Id(id)))
            {
                Note("需要一块眠苔菌块和一份对应辅料。", "Load one moss block and one matching reagent.");
                return;
            }
            Consume(block);
            Consume(extra);
            SetState(tag, 1);
            Refresh();
        }

        void Collect(string tag, string key, GameGridInventory output)
        {
            int state = State(tag);
            if (state == 2) SetState(tag, CartelBatch.AfterOutput(state, Place(output, key, 1)));
        }

        void Settle(bool overnight)
        {
            if (!Ready()) return;
            if (overnight)
            {
                SetState(blueTag, CartelBatch.Advance(State(blueTag)));
                SetState(whiteTag, CartelBatch.Advance(State(whiteTag)));
            }
            Collect(blueTag, Gel, blueOutput);
            Collect(whiteTag, Crystal, whiteOutput);
            Refresh();
        }

        blue.SetCallback(() => HookGuard.Run("蓝胆开批", () => Start(blueTag, resin, Resin)));
        white.SetCallback(() => HookGuard.Run("白胆开批", () => Start(whiteTag, salt, Salt)));
        collect.SetCallback(() => HookGuard.Run("反应槽取料", () => Settle(false)));
        machine.onCycleEndSlotItemFunc = (_, _, _) => HookGuard.Run("反应槽过夜", () => Settle(true));
        window.openCallback += Refresh;
        return machine;
    }

    private static void Consume(GameItem item)
    {
        if (item.unitCount == 1) item.Destroy();
        else item.SetUnitCount(item.unitCount - 1);
    }

    private static bool Place(GameGridInventory output, string key, int count)
    {
        var items = new List<GameItem>(count);
        bool placed = false;
        try
        {
            for (int i = 0; i < count; i++)
            {
                var item = Goods.Make(key);
                if (item == null) return false;
                items.Add(item);
                GeneralHelper.SetItemOwned(item);
            }
            placed = ProductionOutput.TryAddBatch(output, items);
            return placed;
        }
        finally
        {
            if (!placed)
                foreach (var item in items)
                    if (item is not GameItemElement element || !element.IsDestroyed()) item.Destroy();
        }
    }
}
