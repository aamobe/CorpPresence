using System;

namespace CorpPresence;

internal static class SleeveOiling
{
    internal const string Recipe = "oil_feed_sleeve";
    internal const string Lubricant = "mechanical_lubricant";
    private const string OilTag = "CORP_SLEEVE_OIL";
    internal static int Oil(GameItem item) =>
        item?.identifier == "corp_presence_" + RodMaterial.FeedSleeve
            ? Math.Clamp(item.GetTagReadonly(OilTag).GetInt(), 0, 1) : 0;

    internal static void Consume(GameItem item) => item.DisableTag(OilTag, skipValidate: true);

    internal static bool Accept(GameItem item) => item?.unitCount > 0 && GeneralHelper.IsItemOwned(item)
        && (item.identifier == "corp_presence_" + Lubricant ||
            item.identifier == "corp_presence_" + RodMaterial.FeedSleeve && item.unitCount == 1);

    private static bool Find(GameSlotInventory[] slots, out GameItem sleeve, out GameItem oil)
    {
        sleeve = null;
        oil = null;
        foreach (var slot in slots)
        {
            var item = slot.childItem;
            if (!Accept(item)) continue;
            if (item.identifier == "corp_presence_" + Lubricant) oil ??= item;
            else if (Oil(item) == 0 && RodCondition.Read(item) is { Scored: false, Surface: { Porosity: >= 5 and <= 9, Wear: >= 40 } })
                sleeve ??= item;
        }
        return sleeve != null && oil != null;
    }

    internal static string Status(GameSlotInventory[] slots) => Find(slots, out _, out _)
        ? L.T("可浸油 · 消耗 1 份润滑料，原轴套保留", "Ready to oil · uses one portion of lubricant on the sleeve in place")
        : L.T("放入干轴套与润滑料各 1 件，已浸油的轴套无需再加",
            "Insert one dry feed sleeve and a lubricant portion. A filled sleeve needs no more oil.");

    internal static void Apply(GameSlotInventory[] slots)
    {
        if (!Find(slots, out var sleeve, out var oil)) return;
        sleeve.ModifyTag(OilTag, tag => tag.Enable().SetInt(1), skipValidate: true);
        oil.unitCount--;
        HookGuard.Run("刷新轴套浸油用料", () =>
        {
            if (oil.unitCount == 0) IndustrialWorkbench.Discard(oil);
            else oil.SetUnitCount(oil.unitCount);
        });
    }
}
