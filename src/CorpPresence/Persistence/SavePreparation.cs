using System;
namespace CorpPresence;

internal static class SavePreparation
{
    [ThreadStatic] private static bool _entered;

    public static bool TryEnter()
    {
        if (_entered) return false;
        _entered = true;
        return true;
    }
    public static void Exit() => _entered = false;
}
