using System;
using System.Runtime.CompilerServices;
namespace CorpPresence;

internal static class ServiceRetry
{
    private sealed class Attempt { public float Delay, Next; }
    private static readonly ConditionalWeakTable<object, Attempt> Attempts = new();
    public static bool Ready(object key, float now) => key != null && (!Attempts.TryGetValue(key, out var attempt) || now >= attempt.Next);
    public static void Failed(object key, float now)
    {
        if (key == null) return;
        var attempt = Attempts.GetValue(key, _ => new Attempt());
        attempt.Delay = attempt.Delay <= 0f ? 5f : Math.Min(60f, attempt.Delay * 2f);
        attempt.Next = now + attempt.Delay;
    }
    public static void Succeeded(object key) { if (key != null) Attempts.Remove(key); }
}
