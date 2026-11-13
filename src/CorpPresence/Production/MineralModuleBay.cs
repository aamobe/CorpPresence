namespace CorpPresence;

internal static class MineralModuleBay
{
    // Match the vanilla furnace so both expansion stages use the same grid.
    internal static GameGridInventory Create() => new("10000010001000001110000010001000001", 7);
}
