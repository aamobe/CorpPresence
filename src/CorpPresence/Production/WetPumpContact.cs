namespace CorpPresence;

internal static class WetPumpContact
{
    internal static bool Qualified(RodCondition stock) => stock?.Jacket != null
        && stock.Core.Conductivity >= 60 && stock.Core.Brittleness <= 4
        && stock.Surface.Corrosion >= 45 && stock.Surface.Porosity <= 3;

    // Each successful 40 mL shift abrades one half powder point from the outer layer.
    internal static void Wear(GameItem item, RodCondition stock) => RodJacket.Wear(item, stock, 1);
}
