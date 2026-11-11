using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class Perfume
{
    internal const string SeedId = "corp_presence_oilflower_seed";
    internal const string FlowerId = "corp_presence_oilflower";
    internal const string LegacyOilBottleKey = "perfume_base_oil";
    internal const string OilKey = "perfume_base_oil_material";
    internal const string RedKey = "perfume_extract";
    internal const string BlueKey = "perfume_blue_extract";
    internal const string OilLiquid = "corp_flower_oil";
    internal const string MashLiquid = "corp_flower_mash";
    internal const string ExtractorKey = "perfume_oil_extractor";
    internal const string BloodKey = "perfume_blood_extractor";
    internal const string MixerKey = "perfume_mixer";
    internal const string CollectorKey = "perfume_blood_collector";
    internal const string CarryTag = "CORP_PERFUME_CARRY_INT";
    internal const int OilPerMaterialUl = 10000;

    internal static readonly CatalogEntry[] Catalog = {
        new CatalogEntry { Id = "@oilflower_seed", Page = "香水", MinTier = 2, WeeklyLimit = 2, WindowDays = 7 },
        new CatalogEntry { Id = "@" + ExtractorKey, Page = "香水", MinTier = 2, WeeklyLimit = 1, WindowDays = 7 },
        new CatalogEntry { Id = "@" + BloodKey, Page = "香水", MinTier = 2, WeeklyLimit = 1, WindowDays = 7 },
        new CatalogEntry { Id = "@" + MixerKey, Page = "香水", MinTier = 2, WeeklyLimit = 1, WindowDays = 7 },
        new CatalogEntry { Id = "@" + CollectorKey, Page = "香水", MinTier = 2, WeeklyLimit = 1, WindowDays = 7 }
    };

    // Member supply prices are explicit, so catalog markup cannot change the agreed prices.
    internal static int CatalogPrice(CatalogEntry entry) => entry == null ? 0 : (int)Goods.ValueOf(entry.Id.TrimStart('@'));

    internal static readonly DefinitionSet<Goods.Def> Defs = new("Perfume")
    {
        ["oilflower_seed"] = Item("油花种子", "Oilflower Seeds", 220, "oilflower_seed", 1, 1,
            "标准条件约6晚收获4份花材。先天具有芳香物质Ⅰ、不育Ⅲ；不育降低返种概率，仍可能返种。移除不育后可移入多年生。多年生再次结果基础进度21，产量与生长速度遵循原版基因效果。金卡会员每7天限购2颗。",
            "About 6 nights for 4 flowers. Innate Aromatic Compounds I and Infertile III reduce, but do not eliminate, seed returns. Remove infertility to insert Perennial. Refruit base work: 21, with native gene yield and speed modifiers. Gold members may buy 2 per rolling 7 days.",
            InitFlowerSeed, "SEED"),
        ["oilflower"] = Item("油花花材", "Oilflower Blossoms", 15, "oilflower", 1, 2,
            "可用搅拌机打浆，再以酒精温浸释放基底油成分。含油混合液放进基底油提取器，每10mL油成分制成1份材料，无纯度要求。芳香物质Ⅰ时每份花材理想产油10mL，高等级增加花浆产量。不能种植。",
            "Blend, then extract with warm alcohol. Each 10 mL of oil becomes 1 material, with no purity requirement. Aromatic Compounds I yields up to 10 mL per flower; higher ranks produce more mash. Cannot be planted.", InitFlower, "CHEMICAL_SUPPLIES"),
        [OilKey] = Item("香水基底油", "Perfume Base Oil", 5, LegacyOilBottleKey, 1, 2,
            "定量的香水材料。基底油提取器默认每次从液体中扣除10mL基底油成分，制成1份。神经活性香水需3份，费洛蒙香水需2份；与对应血液提取物一起合成。",
            "A measured crafting material. By default the extractor removes 10 mL of the base oil component from a liquid to make 1 portion. Neuroactive Perfume uses 3 portions; Pheromone Perfume uses 2, plus the corresponding blood extract.", null, "CHEMICAL_SUPPLIES"),
        [LegacyOilBottleKey] = LegacyOilBottle(),
        [RedKey] = Item("神经活性提取物", "Neuroactive Extract", 40, RedKey, 1, 2,
            "2袋普通血袋提取而成。与3份香水基底油材料一起放入香水合成机，生产1瓶原版神经活性香水。",
            "Extracted from 2 ordinary blood bags. Combine with 3 base oil materials in a synthesizer for 1 vanilla Neuroactive Perfume.", null, "CHEMICAL_SUPPLIES"),
        [BlueKey] = Item("费洛蒙提取物", "Pheromone Extract", 140, BlueKey, 1, 2,
            "1袋蓝血提取而成。与2份香水基底油材料一起放入香水合成机，生产1瓶原版费洛蒙香水。",
            "Extracted from 1 blue blood bag. Combine with 2 base oil materials in a synthesizer for 1 vanilla Pheromone Perfume.", null, "CHEMICAL_SUPPLIES"),
        [ExtractorKey] = Machine("基底油提取器", "Base Oil Extractor", 2400, ExtractorKey, 2, 3, 0,
            "放入含基底油成分的液体。默认每次扣除10mL基底油成分，产出1份香水基底油材料；不检查纯度，水、酒精和杂质原样留在容器中。默认每晚2份，耗12电。",
            "Load a liquid containing base oil. By default each batch removes 10 mL of that component to make 1 base oil material. No purity check; water, alcohol and impurities remain in the container. Default: 2 portions per night, 12 charge per run."),
        [BloodKey] = Machine("费洛蒙提取器", "Pheromone Extractor", 1200, BloodKey, 2, 3, 1,
            "选择普通血或蓝血。2袋普通血袋制成1份神经活性提取物；1袋蓝血制成1份费洛蒙提取物。默认每晚1批，耗8电。",
            "Select ordinary or blue blood. Two ordinary bags make 1 Neuroactive Extract; 1 blue bag makes 1 Pheromone Extract. Default: 1 batch per night, 8 charge per run."),
        [MixerKey] = Machine("香水合成机", "Perfume Synthesizer", 1600, MixerKey, 4, 3, 2,
            "选择配方，将两种材料放入原料槽。基础配方：普通提取物1份+基底油材料3份→神经活性香水；蓝血提取物1份+基底油材料2份→费洛蒙香水。默认每晚1瓶，耗6电。",
            "Select a recipe and load both materials. Base recipes: one Neuroactive Extract + 3 base oil materials makes Neuroactive Perfume; 1 Pheromone Extract + 2 base oil materials makes Pheromone Perfume. Default: 1 bottle per night, 6 charge per run."),
        [CollectorKey] = Machine("自动采血机", "Blood Collector", 400, CollectorKey, 3, 3, 3,
            "手动按采血，默认每次耗2电，获得1袋普通血。每日第一次无伤，第二次轻伤，第三次重伤，最多3次。重伤时不能采血。次数由同一存档所有采血机共享，次日重置但不治愈伤势。",
            "Press Draw Blood: 2 charge by default for 1 ordinary blood bag. First daily draw causes no injury, second a minor wound, third a major wound; maximum 3. Serious wounds block drawing. All collectors share the saved daily count. A new day resets the count, not wounds.")
    };

    private static Goods.Def Item(string name, string en, int value, string art, int w, int h, string desc, string descEn, Action<GameItem> setup, params string[] types) => new()
    {
        Name = name, NameEn = en, Value = value, Company = "solana", Manufacturer = "solana", Art = art,
        W = w, H = h, Shape = string.Join("|", Enumerable.Repeat(new string('#', w), h)),
        Shell = () => ItemDirectory.CreateEmptyItem(),
        Setup = setup, Desc = desc, DescEn = descEn, Types = types
    };

    private static Goods.Def Machine(string name, string en, int value, string art, int w, int h, int kind, string desc, string descEn)
    {
        var d = Item(name, en, value, art, w, h, desc, descEn, null, "MACHINE");
        d.Shell = () => kind == 3 ? BuildCollector() : BuildMachine(kind);
        d.ModuleEffects = Effects(kind);
        d.Manual = (desc, descEn);
        if (kind == 0) d.Manual = (d.Manual.Zh + "\n\n花材先用原版搅拌机打浆，加入酒精，在至少50℃下温浸。每份花材含已释放基底油2mL、花浆16mL，花浆理想再释放8mL油；提取速度和溶剂不足的损失参照咖啡。将含油混合液装入本机即可，不需要过滤、蒸发或达到指定纯度。本机只扣除基底油成分，默认每10mL转成1份材料，所有其他成分原样保留。不足当前用量的油留在瓶中，补足后可继续加工。",
            d.Manual.En + "\n\nBlend flowers with vanilla equipment, add alcohol and extract at 50 C or above. A flower contains 2 mL released oil and 16 mL mash, which yields up to 8 mL more oil; throughput and solvent-shortage losses follow coffee. Load the oil-containing mixture into this machine. No filtering, evaporation or purity threshold is required. It removes only base oil: by default 10 mL becomes 1 material; all other components stay unchanged. Oil below the current required amount remains for later processing.");
        if (kind == 3) d.Manual = (desc, descEn);
        string capacityZh = kind is 0 or 3 ? "成品格" : "原料格和成品格";
        string capacityEn = kind is 0 or 3 ? "the output grid" : "both input and output grids";
        d.Manual = (d.Manual.Zh + $"\n\n扩容：将金属锭拖到机器上，纯度不限。第一级需3块，第二级再需6块，最多2级；每级为{capacityZh}增加2行。升级进度和容量随存档保存。",
            d.Manual.En + $"\n\nCapacity: drag metal ingots onto the machine, any purity. Level 1 costs 3 ingots; level 2 costs 6 more. Maximum 2 levels. Each level adds 2 rows to {capacityEn}. Upgrade progress and capacity persist across saves.");
        return d;
    }

    internal static void Tell(string text) => StoreUIManager.Instance?.Notify(text, "sec");
}
