using System.Collections.Generic;

namespace CorpPresence;

internal static class IndustrialGoods
{
    internal const string Billet = IndustrialRecipes.Billet;
    internal const string Alloy = IndustrialRecipes.Alloy;
    internal const string Blend = IndustrialRecipes.Blend;
    internal const string Contact = IndustrialRecipes.Contact;
    internal const string Blank = IndustrialRecipes.Blank;
    internal const string Liner = IndustrialRecipes.Liner;
    internal const string Frame = IndustrialRecipes.Frame;
    internal const string Unit = IndustrialRecipes.Unit;
    internal const string SpentContact = IndustrialRecipes.SpentContact;
    internal const string Pad = "heavy_screen_pad", OldShell = "returned_filter_shell", CleanShell = "cleaned_filter_shell";

    private const string FreshTag = "CORP_FILTER_NEW_FRAME";
    private const string RunTag = "CORP_FILTER_RUN", InstanceTag = "CORP_FILTER_INSTANCE", SlotTag = "CORP_FILTER_SLOT";
    internal static bool IsFreshUnit(GameItem item) => Goods.IdOf(item) == Unit
        && item.GetTagReadonly(FreshTag).GetBool() && string.IsNullOrEmpty(item.GetTagReadonly(RunTag).GetString())
        && item.GetTagReadonly(InstanceTag).GetInt() == 0 && item.GetTagReadonly(SlotTag).GetInt() == 0;
    internal static void MarkFresh(GameItem unit) => unit.ModifyTag(FreshTag, t => t.Enable().SetBool(true), skipValidate: true);

    internal static bool TryReadOrigin(GameItem item, out string run, out int instance, out int slot)
    {
        run = item?.GetTagReadonly(RunTag).GetString();
        instance = item?.GetTagReadonly(InstanceTag).GetInt() ?? 0;
        slot = (item?.GetTagReadonly(SlotTag).GetInt() ?? 0) - 1;
        return item?.unitCount == 1 && Goods.IdOf(item) is OldShell or CleanShell or Unit
            && !item.GetTagReadonly(FreshTag).GetBool() && !string.IsNullOrWhiteSpace(run) && instance > 0 && slot is 0 or 1;
    }

    internal static void MarkOrigin(GameItem item, string run, int instance, int slot)
    {
        if (item?.unitCount != 1 || Goods.IdOf(item) is not (OldShell or CleanShell or Unit)
            || string.IsNullOrWhiteSpace(run) || instance <= 0 || slot is not (0 or 1))
            throw new System.ArgumentException("Invalid repair shell origin");
        item.ModifyTag(RunTag, t => t.Enable().SetString(run), skipValidate: true);
        item.ModifyTag(InstanceTag, t => t.Enable().SetInt(instance), skipValidate: true);
        // Zero is the missing-tag default, so persist the two slots as 1 and 2.
        item.ModifyTag(SlotTag, t => t.Enable().SetInt(slot + 1), skipValidate: true);
    }

    internal static readonly Dictionary<string, Goods.Def> Definitions = new()
    {
        [SinteringRecipes.StandardRod] = Rod("标准合金棒", "Standard Alloy Rod", 20,
            "金属 30 点、硅质 6 点。", "30 metal points and 6 silica points."),
        [SinteringRecipes.WearRod] = Rod("耐磨合金棒", "Wear Alloy Rod", 24,
            "金属 22.5 点、稀有金属 2.5 点。", "22.5 metal points and 2.5 rare metal points."),
        [SinteringRecipes.ConductiveRod] = Rod("导电合金棒", "Conductive Alloy Rod", 26,
            "导电料 12 点、硅质 4 点、稀有金属 2 点。", "12 conductive points, 4 silica points and 2 rare metal points."),
        [SinteringRecipes.TrialRod] = Rod("试配合金棒", "Trial Alloy Rod", 24,
            "材料档案记录实际配比，每根含 50 粉点。", "Its material record holds the actual blend. Each rod contains 50 powder points."),
        [RodMaterial.CladRod] = Rod("芯套复合棒", "Clad Alloy Rod", 52,
            "芯和外套分别验料。可切成两段，再车成有独立接触面的给料轴。",
            "The core and jacket are assayed separately. Cut into segments, then turn a shaft with its own contact surface."),
        [RodMaterial.ShortRod] = Formed("合金短棒", "Short Alloy Rod", 9, "metal_ingot", 1,
            "从完整棒切下，保留原配比和工艺。可加工轴套坯。", "Cut from a full rod. Keeps its original alloy and treatment, and can be bored into a sleeve blank.",
            art: RodVisual.ShortArt),
        [RodMaterial.Strip] = Formed("轧制合金带", "Rolled Alloy Strip", 25, "steel_sheet", 2,
            "连续板带，用于衬板和包套。", "Continuous strip for chute liners and cladding.", art: "rolled-alloy-strip"),
        [RodMaterial.WireBlank] = Formed("合金粗线坯", "Coarse Alloy Wire Blank", 27, "wire", 2,
            "保留原料导电与方向，供拉细或绕线。", "Retains conductivity and orientation for finer drawing or winding.", art: "drawn-wire-blank"),
        [RodMaterial.PorousBlank] = Formed("多孔轴套坯", "Porous Sleeve Blank", 15, "metal_ingot", 1,
            "从有储油孔隙的短棒镗出。可继续精整和浸油。", "Bored from short stock with oil-retaining pores. Can be finished and oiled.", art: "porous-sleeve-blank"),
        [RodMaterial.Offcut] = Formed("合金边料", "Alloy Offcut", 0, "scrap_metal", 1,
            "加工留下的余料，五种成分仍按实物记录。", "Forming offcut with all five material components recorded.", art: "alloy-offcut"),
        [RodMaterial.FeedShaft] = Formed("给料轴", "Feed Shaft", 36, "metal_ingot", 1,
            "给料机构的可换轴，运转规格读取芯部组织。", "Replaceable feeder shaft. Operating specifications use its core structure.", art: "feed-shaft"),
        [RodMaterial.FeedSleeve] = Formed("给料轴套", "Feed Sleeve", 32, "metal_ingot", 1,
            "精整后保留储油孔隙。可在工艺台浸油。", "Finishing retains oil pores. Add lubricant at the workbench.", art: "feed-sleeve"),
        [RodMaterial.IndustrialCoil] = Formed("工业线圈", "Industrial Coil", 60, "wire", 1,
            "用连续线坯绕制，保留导电性和致密处理。", "Wound from continuous wire, retaining conductivity and densification.", art: "industrial-coil"),
        [RodMaterial.AxialLead] = Formed("定向引线", "Axial Lead", 48, "wire", 2,
            "用于有规定安装方向的电路，保留沿轴导电读数。", "For circuits with a specified mounting direction. Retains axial conductivity.", art: "axial-lead"),
        [RodMaterial.ReturnGuide] = Formed("回料导槽", "Return Guide", 52, "steel_sheet", 2,
            "连续合金带折弯成形。在旧筛分机与细筛配套使用，可排一批尾粉回筛。",
            "Bent from continuous alloy strip. Fit it with a fine screen in the old sieve to queue one tailings batch.", art: "sieve-return-guide"),
        [RodMaterial.FilterBasket] = Formed("泵前滤篮", "Pump Filter Basket", 58, "steel_sheet", 1,
            "粗线织成的可换滤篮，可单独取下清理。", "Replaceable coarse-wire basket that can be removed for cleaning.", art: "pump-filter-basket"),
        [RodMaterial.PressureShell] = Formed("试水泵承压壳", "Pump Pressure Shell", 64, "steel_sheet", 2,
            "用合格板带精整。装进试水泵后可以加压试水。", "Finished from qualifying strip. Install in a test pump to run a pressure test.", art: "pump-pressure-shell"),
        [RodMaterial.WetContact] = Formed("试水泵耐蚀触点", "Pump Wet Contact", 54, "wire", 1,
            "导电芯和低孔耐蚀外套。配耐蚀滤篮开启高杂质工况，湿班会磨去外套。",
            "Conductive core with a dense corrosion-resistant jacket. Enables high-grit operation with a suitable basket. Each wet shift wears the jacket.",
            art: "wet-pump-contact"),
        [RodMaterial.ToothRoller] = Formed("组合齿辊", "Composite Toothed Roller", 76, "steel_sheet", 2,
            "回火芯承受冲击，硬化外圈处理旧炉衬。用螺丝刀装进破碎机；磨穿后保留芯换面。",
            "The tempered hub absorbs impact and the hardened face grinds spent linings. Fit with a screwdriver and reuse the hub when the face wears through.",
            art: "crusher-tooth-roller"),
        [RodMaterial.CastingDie] = Formed("致密试配铸模", "Dense Trial Die", 68, "steel_sheet", 2,
            "用致密板带精整。装入烧结站，近配时最多保留 5 点多取的矿粉在原盒。",
            "Finished from densified strip. Fit in the sinter to retain up to five points of near-fit casting surplus in the source boxes.", art: "dense-trial-die"),
        [RodMaterial.ClassifierSupport] = Formed("精密筛支承", "Precision Screen Support", 72, "steel_sheet", 2,
            "回火板带经校直制成。与细筛同时装入分级机，硅质占比达到 70% 的粉料即可细筛。",
            "Trued from tempered strip. Fit beside a fine screen in the classifier to process powder with at least 70% silica.", art: "classifier-support"),
        [RodMaterial.GrowthGuide] = Formed("矿化耐蚀导流件", "Mineralizer Flow Guide", 74, "steel_sheet", 2,
            "低孔隙的致密板带精整而成。装入矿化机后，每个普通矿石培养夜少用 6 mL 水。",
            "Finished from low-porosity densified strip. Fit in a mineralizer to use 6 mL less water per active common ore night.", art: "mineral-growth-guide"),
        [SleeveOiling.Lubricant] = Formed("机修润滑料", "Mechanical Lubricant", 8, "med_bottle_orange", 1,
            "阿岑供应的单份润滑料。工艺台每次浸油消耗一份。", "Single portion supplied by Cen. Oiling one sleeve uses one portion.", art: "mechanical-lubricant"),
        [Billet] = Material("耐磨合金坯", "Wear Alloy Billet", 25, "wear-alloy-billet",
            "45 金属和 5 稀有定量烧结。破碎机回磨每件得两包耐磨合金粉。",
            "Sinter 45 metal and 5 rare points. Regrind each billet into two bags of wear alloy powder."),
        [Alloy] = Material("耐磨合金粉", "Wear Alloy Powder", 18, "wear-alloy-powder",
            "回磨合金坯所得。每个工业框架需一包。",
            "Reground from alloy billets. Each industrial frame needs one bag."),
        [Blend] = Material("导电复合粉", "Conductive Blend", 29, "conductive-blend",
            "细砂一包、20 导电和 4 稀有定量配制。每包可做两片触片。",
            "Blend one fine grit, 20 conductive points and 4 rare points. Each bag makes two contacts."),
        [Contact] = Material("泵用触片", "Pump Contact", 30, "pump-contact",
            "每包导电复合粉做两片。水站大修首车需四片。",
            "Make two from one bag of conductive blend. The first overhaul truck needs four."),
        [Blank] = Material("滤芯坯", "Filter Blank", 24, "filter-blank",
            "将一包粗砂放在烧结站固体料位，选此配方，过夜烧结。",
            "Place one coarse grit in the sintering solid-feed slot and select this recipe for overnight processing."),
        [Liner] = Material("工业衬层", "Industrial Filter Liner", 26, "industrial-liner",
            "黑羽静送达光盐蓝图后，在工艺台用一包光盐晶粉制成。",
            "After Shizuka delivers the light salt blueprint, form one bag of crystal powder into a liner at the workbench."),
        [Frame] = Material("工业框架", "Industrial Frame", 58, "industrial-frame",
            "钢胚与合金粉可在锻台加工。子午线也接受合格锭与合金粉的代工委托。",
            "Forge a steel billet with alloy powder, or commission Meridian with a qualifying ingot and alloy powder."),
        [Unit] = Material("滤芯总成", "Filter Assembly", 100, "filter-unit",
            "工艺台用框架、滤芯坯、衬层和眠苔胶装配。大修收全新件；使用净滤壳的总成只能交原返修单，也可拆回材料。",
            "Assemble a frame, blank, liner and gel at the workbench. Overhauls require new frames. Assemblies made with a clean housing can be dismantled or delivered to its repair contract."),
        [SpentContact] = Material("罗森报废触点", "Rosen’s Spent Contact", 15, "spent-pump-contact",
            "每件破碎得稀有金属和导电料各 10 点。",
            "Crush each spent contact for 10 rare metal and 10 conductive points."),
        [Pad] = Material("重载筛垫", "Heavy Screen Pad", 40, "heavy-screen-pad",
            "一包粗砂与一包耐磨合金粉在工艺台制成。装入分级机的筛垫槽，可选双份通量档。",
            "Combine coarse grit and wear alloy powder at the workbench. Install in the classifier’s pad slot to select double throughput."),
        [OldShell] = Material("旧滤壳", "Returned Filter Housing", 0, "returned-filter-shell",
            "水站返修合同领用的旧壳。用 15 mL 酒精清洗，或归还原合同。",
            "Issued for a water station repair. Wash with 15 mL of alcohol or return to the issuing contract."),
        [CleanShell] = Material("净滤壳", "Clean Filter Housing", 0, "cleaned-filter-shell",
            "可代替框架装配本次返修的滤芯总成。归还净壳也可销还该单退件。",
            "Use in place of a frame to assemble a filter for its repair contract, or return the clean housing.")
    };

    static IndustrialGoods()
    {
        foreach (string key in new[] { Unit, OldShell, CleanShell, Pad })
            Definitions[key].Setup = item => item.mayStackItemFunc = null;
    }

    private static Goods.Def Rod(string name, string english, int value, string description, string englishDescription) =>
        new() { Name = name, NameEn = english, Value = value, Company = "mineral", Fallback = "metal_ingot",
            Art = RodVisual.CastArt,
            W = 1, H = 2, Shape = "#|#", Types = new[] { "MATERIAL" }, Desc = description, DescEn = englishDescription,
            Shell = () => ItemDirectory.CreateEmptyItem(), Setup = item => item.mayStackItemFunc = null };

    private static Goods.Def Formed(string name, string english, int value, string fallback, int width,
        string description, string englishDescription, string art = null) =>
        new() { Name = name, NameEn = english, Value = value, Company = "mineral", Fallback = fallback, Art = art,
            ArtSource = art == null ? Goods.ArtKind.NativeSprite : Goods.ArtKind.Embedded,
            W = width, H = 1, Shape = new string('#', width), Types = new[] { "MATERIAL" },
            Desc = description, DescEn = englishDescription, Shell = () => ItemDirectory.CreateEmptyItem(),
            Setup = item => item.mayStackItemFunc = null };

    private static Goods.Def Material(string name, string english, int value, string art, string description, string englishDescription) =>
        new() { Name = name, NameEn = english, Value = value, Company = "mineral", Art = art, Fallback = "scrap_metal",
            W = 1, H = 1, Shape = "#", Types = new[] { "MATERIAL" }, Desc = description, DescEn = englishDescription,
            Shell = () => ItemDirectory.CreateEmptyItem(), Setup = item =>
            {
                item.mayStackItemFunc = (a, b) => a.identifier == b.identifier && GeneralHelper.IsItemOwned(a) == GeneralHelper.IsItemOwned(b);
                item.maxSlotAmountItemFunc = (_, _, _) => 10;
            } };
}
