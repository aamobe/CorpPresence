using System.Collections.Generic;

namespace CorpPresence;

internal static class ShopInventory
{
    // Destructive services must verify physical ancestry, rather than trust an
    // owned-item list that another mod can extend with detached virtual storage.
    public static bool Contains(GameItem item)
    {
        var store = EmporiumEntry.Instance;
        if (store == null || item == null) return false;
        var visited = new HashSet<GameInventory>();
        for (var inventory = item.parentInventory; inventory != null && visited.Add(inventory);
             inventory = inventory.GetParentItem()?.parentInventory)
        {
            if (ReferenceEquals(inventory, store.invElement) ||
                ReferenceEquals(inventory, store.backInvinvElement) ||
                ReferenceEquals(inventory, store.backInvinvElementCounter) ||
                ReferenceEquals(inventory, store.frontInvinvElement) ||
                ReferenceEquals(inventory, store.docInvElement) ||
                ReferenceEquals(inventory, store.showcaseElement) ||
                ReferenceEquals(inventory, store.trashInvElement) ||
                ReferenceEquals(inventory, store.vendingFountainElement) ||
                ReferenceEquals(inventory, store.vendingMachineElement) ||
                ReferenceEquals(inventory, store.hiddenElement))
                return true;
        }
        return false;
    }
}
