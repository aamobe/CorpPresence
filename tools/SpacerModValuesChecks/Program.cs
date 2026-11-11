using CorpPresence;

var failures = new List<string>();
void Check(bool condition, string name)
{
    Console.WriteLine((condition ? "PASS " : "FAIL ") + name);
    if (!condition) failures.Add(name);
}

// Actual production sampler; only the game factory/item boundary is substituted.
var positive = new GameItem { Value = 120 };
var zero = new GameItem { Value = 0 };
var negative = new GameItem { Value = -1 };
var unreadable = new GameItem { ThrowOnRead = true };
var badRelease = new GameItem { Value = 90, ThrowOnDestroy = true };
DirectoryMaster.Factory = id => id switch
{
    "internal_springs" => positive,
    "trigger_group" => zero,
    "match_barrel" => negative,
    "pistol_switch" => unreadable,
    "ammo_display" => badRelease,
    "grip_gel" => throw new InvalidOperationException("factory unavailable"),
    _ => null
};
var first = SpacerCo.SampleForCheck();
Check(first.Count == 2 && first["internal_springs"] == 120 && first["ammo_display"] == 90,
    "only positive readable base values enter the snapshot");
Check(positive.DestroyCalls == 1, "positive sample released once");
Check(zero.DestroyCalls == 1, "zero-value sample released once");
Check(negative.DestroyCalls == 1, "negative-value sample released once");
Check(unreadable.DestroyCalls == 1, "sample released when reading value throws");
Check(badRelease.DestroyCalls == 1 && DirectoryMaster.Calls == 30, "release failure does not abort remaining pool");

var recovered = new GameItem { Value = 220 };
DirectoryMaster.Calls = 0;
DirectoryMaster.Factory = id => id == "trigger_group" ? recovered : null;
var second = SpacerCo.SampleForCheck();
Check(second.Count == 1 && second.TryGetValue("trigger_group", out var value) && value == 220,
    "later call retries unavailable entries instead of retaining partial snapshot");
Check(DirectoryMaster.Calls == 30 && recovered.DestroyCalls == 1, "each roll owns and releases its new samples");
Check(first.Count == 2 && first["internal_springs"] == 120, "earlier snapshot remains independent");

DirectoryMaster.Factory = _ => null;
Check(SpacerCo.SampleForCheck() == null, "empty pool returns null for existing roll fallback");
if (failures.Count > 0) throw new Exception(string.Join("; ", failures));
Console.WriteLine("SPACER MOD VALUES CHECKS PASSED");

namespace CorpPresence
{
    internal sealed partial class SpacerCo
    {
        internal static Dictionary<string, long> SampleForCheck() => ModValues();
    }
}

internal sealed class GameItem
{
    internal long Value;
    internal bool ThrowOnRead, ThrowOnDestroy;
    internal int DestroyCalls;
    public long unitValue => ThrowOnRead ? throw new InvalidOperationException("value unavailable") : Value;
    public void Destroy()
    {
        DestroyCalls++;
        if (ThrowOnDestroy) throw new InvalidOperationException("release failed");
    }
}

internal static class DirectoryMaster
{
    internal static Func<string, GameItem> Factory;
    internal static int Calls;
    public static GameItem Item(string id, bool isOwned)
    {
        if (isOwned) throw new Exception("valuation samples must not be owned inventory");
        Calls++;
        return Factory(id);
    }
}
