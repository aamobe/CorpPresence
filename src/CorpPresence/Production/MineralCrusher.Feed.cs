using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class MineralCrusher
{
    private static bool IsFeed(GameItem item)
    {
        if (item == null || item.unitCount <= 0 || !GeneralHelper.IsItemOwned(item)) return false;
        var feed = CrusherBatch.Classify(item.identifier);
        return feed == CrusherFeed.AlloyOffcut ? OffcutParts(item) != null : feed != CrusherFeed.None;
    }

    private static int[] OffcutParts(GameItem item)
    {
        if (item.unitCount != 1 || RodMaterial.Read(item) is not { } stock
            || stock.PowderPoints > PowderColor.Capacity) return null;
        // The material ledger stores half points. Recover 80% of each component
        // as whole powder points, rounding down rather than creating another component.
        int Recover(int halfPoints) => halfPoints * 2 / 5;
        var parts = new[] { Recover(stock.Metal), Recover(stock.Silica), Recover(stock.Rare),
            Recover(stock.Conductive), Recover(stock.Residue) };
        return parts.Sum() > 0 ? parts : null;
    }

    private static int MaximumPowder(GameItem item, CrusherFeed feed, int quality)
    {
        if (feed != CrusherFeed.AlloyOffcut) return CrusherBatch.MaximumPowder(feed, quality);
        var parts = OffcutParts(item);
        return parts == null ? 0 : PowderCount(parts, CrusherBatch.Residue(parts[4], quality));
    }

    private static int PowderCount(System.ReadOnlySpan<int> parts, int residue) =>
        parts[0] + parts[1] + parts[2] + parts[3] + residue;

    private const string FeedOrderTag = "CORP_CRUSHER_FEED_ORDER";
    private static int FeedOrder(GameItem item) => item.IsTag(FeedOrderTag) ? item.GetTagReadonly(FeedOrderTag).GetInt() : 0;

    internal static IEnumerable<GameItem> FeedInOrder(GameGridInventory input) =>
        input.childItems.Where(IsFeed).OrderBy(FeedOrder)
            .ThenBy(item => item.modifiedShape.minY).ThenBy(item => item.modifiedShape.minX);

    internal static void QueueFeed(GameItem added, GameInventory inventory, SlotMarker marker)
    {
        // Native decoding invokes the same add callback. Keep tagged positions and leave
        // older untagged stacks at priority zero so their grid coordinates break ties.
        if (SaveManager.IsDecoding) return;
        int prior = FeedOrder(added);
        if (prior > 0 && !inventory.childItems.Any(other => other != added && FeedOrder(other) == prior)) return;
        int last = inventory.childItems.Select(FeedOrder).DefaultIfEmpty(0).Max();
        if (last == int.MaxValue)
        {
            last = 0;
            foreach (var item in FeedInOrder((GameGridInventory)inventory).Where(item => item != added))
            {
                int position = ++last;
                item.ModifyTag(FeedOrderTag, tag => tag.Enable().SetInt(position), skipValidate: true);
            }
        }
        added.ModifyTag(FeedOrderTag, tag => tag.Enable().SetInt(last + 1), skipValidate: true);
    }

    internal static void DequeueFeed(GameItem item, GameInventory inventory, SlotMarker marker) =>
        item.DisableTag(FeedOrderTag, skipValidate: true);
}
