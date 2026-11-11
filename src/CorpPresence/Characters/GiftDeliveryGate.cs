using System;

namespace CorpPresence;

internal sealed class GiftDeliveryGate
{
    private bool _delivering;

    internal bool TryDeliver(Func<bool> pending, Func<bool> place, Action complete)
    {
        if (_delivering || !pending()) return false;
        _delivering = true;
        try
        {
            if (!place()) return false;
            complete();
            return true;
        }
        finally { _delivering = false; }
    }
}
