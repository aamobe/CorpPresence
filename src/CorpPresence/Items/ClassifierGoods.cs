using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class ClassifierGoods
{
    internal const string Coarse = "coarse_silica_grit", Fine = "fine_silica_grit", Crystal = "light_salt_grit";
    internal const string Screen = ClassifierBatch.Screen, Sleeve = ClassifierBatch.Sleeve;
    private const string ChargeTag = "CORP_SLEEVE_CHARGES";

    internal static readonly Dictionary<string, Goods.Def> Definitions = new()
    {
        [Coarse] = Material("粗硅砂", "Coarse Silica Grit", 12, "mineral", "coarse-silica-grit",
            "每包 20 点。分级机通量档每批出两粗一细。用一包粗砂和一块硅板在综合工艺台制作细筛。",
            "20 points per bag. Throughput mode makes two coarse bags and one fine bag. Combine one coarse bag with a silicon plate at the workbench to make a fine screen."),
        [Fine] = Material("细硅砂", "Fine Silica Grit", 18, "mineral", "fine-silica-grit",
            "每包 20 点。分级机装上细筛并选细筛档，可用 60 点合格硅质粉在两夜制成一粗两细。",
            "20 points per bag. With a fine screen fitted, fine mode turns 60 qualifying silica points into one coarse bag and two fine bags in two nights."),
        [Crystal] = Material("光盐晶粉", "Light Salt Crystal Powder", 16, "cartel", "light-salt-grit",
            "分级机晶磨一份光盐析晶得两包。可出售，也可在综合工艺台用一包翻新磨套。",
            "Mill one light salt precipitate into two bags. Sell the powder or use one bag at the workbench to refurbish a grinding sleeve."),
        [Screen] = Material("细筛", "Fine Screen", 30, "mineral", "fine-screen",
            "分级机可换模式件。装入后切到细筛档，每批两夜。",
            "A reusable classifier attachment. Fit it and select fine mode for a two-night batch."),
        [Sleeve] = Material("晶磨套", "Crystal Grinding Sleeve", 40, "mineral", "crystal-grinding-sleeve",
            "黑羽静送达光盐蓝图后供应。晶磨基础两夜。用一包晶粉翻新可快磨三批，每批一夜。",
            "Available after Shizuka delivers the light salt blueprint. Crystal milling takes two nights. Refurbish with one bag of crystal powder for three one-night batches.")
    };

    private static Goods.Def Material(string name, string english, int value, string company, string art, string zh, string en) =>
        new() { Name = name, NameEn = english, Value = value, Company = company, Art = art, Fallback = "scrap_metal",
            W = 1, H = 1, Shape = "#", Types = new[] { "MATERIAL" }, Desc = zh, DescEn = en,
            Shell = () => ItemDirectory.CreateEmptyItem() };

    static ClassifierGoods()
    {
        foreach (string key in new[] { Coarse, Fine, Crystal })
            Definitions[key].Setup = item =>
            {
                // Same ten-unit material stacks as the native steel sheet. Sleeves carry individual use counts.
                item.mayStackItemFunc = (a, b) => a.identifier == b.identifier
                    && GeneralHelper.IsItemOwned(a) == GeneralHelper.IsItemOwned(b);
                item.maxSlotAmountItemFunc = (_, _, _) => 10;
            };
        Definitions[Sleeve].Setup = item => item.mayStackItemFunc = null;
    }

    internal static int Charges(GameItem sleeve) => Goods.IdOf(sleeve) == Sleeve
        ? Math.Clamp(sleeve.GetTagReadonly(ChargeTag).GetInt(), 0, 3) : 0;
    internal static void SetCharges(GameItem sleeve, int value) =>
        sleeve.ModifyTag(ChargeTag, tag => tag.Enable().SetInt(Math.Clamp(value, 0, 3)), skipValidate: true);
}
