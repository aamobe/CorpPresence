namespace CorpPresence;

// 把模组发的东西放进店里。
// 原版 TryAddDirectSellingItemToTable 在柜台放满时会先把物品销毁再返回失败，
// 旧写法接着拿这个已销毁的对象去 EmporiumTryAdd，可能把它挂进库存或白白丢掉。
// 这里先确认柜台放得下再放（放不下不销毁），然后交给原版按顺序试各个库存；
// 原版所有库存都满时会销毁物品，此时返回 false。
internal static class StorePlacement
{
    public static bool Place(GameItem item)
    {
        if (item == null) return false;
        EmporiumEntry store = EmporiumEntry.Instance;
        if (store == null || (store.frontInvinvElement == null && store.invElement == null)) return false;
        GeneralHelper.SetItemOwned(item, true);
        if (store.frontInvinvElement != null && GraphUtils.CanAccept(store.frontInvinvElement, item))
            GraphUtils.TryAcceptAll(store.frontInvinvElement, item);
        if (Attached(item)) return true;
        if (Destroyed(item)) return false;
        GraphUtils.EmporiumTryAdd(store.invElement ?? store.frontInvinvElement, item);
        return Attached(item);
    }

    public static bool Destroyed(GameItem item) => item is GameItemElement element && element.IsDestroyed();

    public static bool Attached(GameItem item) => item != null && !Destroyed(item) && item.parentInventory != null;
}
