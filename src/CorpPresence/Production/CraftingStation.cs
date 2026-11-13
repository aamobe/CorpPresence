namespace CorpPresence;

internal static class CraftingStation
{
    internal const string Key = "crafting_station";
    internal const string SiliconPlate = "silicon_plate";
    internal const string Substrate = "electronic_substrate";
    internal const string Blueprint = "electronic_blueprint";
    private const string Product = "common_electronic";

    internal static readonly Goods.Def Definition = new()
    {
        Name = "综合工艺台", NameEn = "Assembly Workbench", Value = 240,
        Company = "mineral", Art = "crafting-station", Fallback = "furnace",
        W = 3, H = 3, Types = new[] { "MACHINE" }, Shell = Build
    };

    internal static readonly Goods.Def SiliconPlateDefinition = new()
    {
        Name = "硅板", NameEn = "Silicon Plate", Value = 6,
        Company = "mineral", Art = "silicon-plate", Fallback = "steel_sheet",
        W = 2, H = 2, Shape = "##|##", Types = new[] { "MATERIAL" }, Shell = () => ItemDirectory.CreateEmptyItem()
    };

    internal static readonly Goods.Def SubstrateDefinition = new()
    {
        Name = "电子基材", NameEn = "Electronic Substrate", Value = 3,
        Company = "mineral", Art = "electronic-substrate", Fallback = "common_electronic",
        W = 2, H = 1, Shape = "##", Types = new[] { "MATERIAL" }, Shell = () => ItemDirectory.CreateEmptyItem()
    };

    internal static readonly Goods.Def BlueprintDefinition = new()
    {
        Name = "普通电子元件蓝图", NameEn = "Common Electronics Blueprint", Value = 35,
        Company = "mineral", Art = "electronic-blueprint", Fallback = "printer_chip_10_mag",
        W = 1, H = 1, Shell = () => ItemDirectory.CreateEmptyItem()
    };

    private static GameItem Build()
    {
        var blueprint = new GameSlotInventory(2, 2)
        {
            mayInventoryAddItemFunc = (item, _) => Goods.IdOf(item) == Blueprint
        };
        var inputs = new GameSlotInventory[3];
        for (int i = 0; i < inputs.Length; i++)
            inputs[i] = new GameSlotInventory(3, 3) { mayInventoryAddItemFunc = (item, _) => MaterialIndex(item) >= 0 };
        var output = new GameGridInventory(4, 3) { mayInventoryAddItemFunc = (_, _) => false };
        var assemble = new MachineryButton(L.K("production.crafting.assemble", "装配", "Assemble"));
        var status = new MachineryText(uppercase: false);
        var window = MachineHelper.CreateMachineryWindow(true, new[] { 0, 1, 2, 3 },
            MachineHelper.Panel(L.K("production.crafting.blueprint", "蓝图", "Blueprint"), blueprint, "blueprint"),
            MachineHelper.Panel(L.K("production.crafting.materials", "材料", "Materials"), MachineryStack.Horizontal(inputs), "input"),
            MachineHelper.Panel(L.K("production.crafting.assembly", "装配", "Assembly"), MachineryStack.Vertical(status, assemble), "control"),
            MachineHelper.Panel(L.K("production.crafting.output", "出料", "Output"), output, "output"));
        var machine = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        machine.EnableTag("CONTAINER_TAG");

        bool HasBlueprint() => Goods.IdOf(blueprint.childItem) == Blueprint;
        bool HasMaterials()
        {
            int mask = 0;
            foreach (var slot in inputs)
            {
                var item = slot.childItem;
                int index = MaterialIndex(item);
                if (index < 0 || item.unitCount < 1) return false;
                mask |= 1 << index;
            }
            return mask == 7;
        }

        void Refresh()
        {
            status.SetText(!HasBlueprint()
                ? L.K("production.crafting.needBlueprint", "放入普通电子元件蓝图", "Insert the Common Electronics Blueprint")
                : !HasMaterials()
                    ? L.K("production.crafting.needMaterials", "需要硅板、电线和电子基材各 1 件", "Requires one silicon plate, wire and electronic substrate")
                    : !MachineAppearance.CanFit(output, Product)
                        ? L.K("production.crafting.full", "出料区空间不足", "Insufficient output space")
                        : L.K("production.crafting.ready", "可装配 1 件普通电子元件", "Ready to assemble one common electronic component"));
        }

        assemble.SetCallback(() => HookGuard.Run("装配电子元件", () =>
        {
            if (!GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence")
                || !HasBlueprint() || !HasMaterials() || !DirectoryMaster.Has<GameItem>(Product)
                || !MachineAppearance.CanFit(output, Product)) return;

            var product = DirectoryMaster.Item(Product, isOwned: true);
            if (product == null) return;
            if (!ProductionOutput.TryAdd(output, product))
            {
                product.Destroy();
                return;
            }

            foreach (var slot in inputs)
            {
                var item = slot.childItem;
                if (item.unitCount == 1) item.Destroy();
                else item.SetUnitCount(item.unitCount - 1);
            }
            Refresh();
        }));

        void Watch(GameInventory inventory)
        {
            inventory.onSlotAddItemEndFunc += (_, _, _) => Refresh();
            inventory.onSlotRemoveItemEndFunc += (_, _, _) => Refresh();
        }
        Watch(blueprint);
        foreach (var input in inputs) Watch(input);
        Watch(output);
        MachineAppearance.WatchOutput(machine, output);
        window.openCallback += Refresh;
        return machine;
    }

    private static int MaterialIndex(GameItem item) => item?.identifier == "wire" ? 1 : Goods.IdOf(item) switch
    {
        SiliconPlate => 0,
        Substrate => 2,
        _ => -1
    };
}
