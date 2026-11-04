using System;
namespace CorpPresence;

internal static class CrateFillGate
{
    [ThreadStatic] private static object _current;

    public static object Enter(object inventory)
    {
        if (inventory == null) throw new ArgumentNullException(nameof(inventory));
        object previous = _current;
        _current = inventory;
        return previous;
    }

    public static void Restore(object previous) => _current = previous;
    public static bool Allows(object inventory) => inventory != null && ReferenceEquals(_current, inventory);
}
