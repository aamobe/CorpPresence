namespace CorpPresence;

internal sealed class PlayerStore
{
    internal static PlayerStore Instance;
    internal static bool IsInstanceExist() => Instance != null;
    internal Dictionary<string, string> modData;
    internal int Cash;
    internal int DebitCalls;
    internal int CreditCalls;
    internal void PayMoney(int amount, bool playRegisterSound)
    {
        if (playRegisterSound) throw new Exception("Unexpected native audio");
        Cash -= amount;
        DebitCalls++;
    }
    internal void ModCash(int amount, bool playSound)
    {
        if (playSound) throw new Exception("Unexpected native audio");
        Cash += amount;
        CreditCalls++;
    }
}

internal sealed class Order { internal int Cash; }
internal sealed class EmporiumAudioPlayer
{
    internal static EmporiumAudioPlayer Instance = new();
    internal void PlayRegister() => throw new Exception("Injected sound failure");
}
internal sealed class NegociationUIManager
{
    internal static NegociationUIManager Instance = new();
    internal Audio audioSourceCashRegister = new();
}
internal sealed class Audio
{
    internal void Play() => throw new Exception("Injected sound failure");
}
