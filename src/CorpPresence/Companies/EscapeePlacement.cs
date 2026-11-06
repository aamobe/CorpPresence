namespace CorpPresence;

internal static class EscapeePlacement
{
    public static bool IsSaved(EmporiumEntry store, GameItem item)
    {
        if (item?.parentInventory == null || (item is GameItemElement element && element.IsDestroyed()))
            return false;
        return ReferenceEquals(item.parentInventory, store.invElement)
            || ReferenceEquals(item.parentInventory, store.backInvinvElement);
    }

    public static bool TryPlace(EmporiumEntry store, GameItem item)
    {
        // Vanilla serializes the main and back inventories, but not the front counter.
        if (store.invElement != null)
            GraphUtils.TryAcceptAll(store.invElement, item);
        if (item.parentInventory == null && !(item is GameItemElement element && element.IsDestroyed())
            && store.backInvinvElement != null)
            GraphUtils.TryAcceptAll(store.backInvinvElement, item);
        return IsSaved(store, item);
    }
}
