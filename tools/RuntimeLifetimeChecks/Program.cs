using CorpPresence.Runtime;
using CorpPresence;

int passed = 0;
void Check(string name, Action test)
{
    test();
    passed++;
    Console.WriteLine("PASS " + name);
}
void Require(bool condition)
{
    if (!condition) throw new Exception("Unexpected lifecycle result");
}

Check("duplicate enable and disable release each acquisition once", () =>
{
    int acquired = 0, released = 0;
    var lifetime = new RuntimeLifetime(_ => { });
    Action initialize = () => lifetime.Own(() => acquired++, () => released++);
    Require(lifetime.Start(initialize));
    Require(!lifetime.Start(initialize));
    lifetime.Stop();
    lifetime.Stop();
    Require(acquired == 1 && released == 1);
    Require(lifetime.Start(initialize));
    lifetime.Stop();
    Require(acquired == 2 && released == 2);
});

Check("partial acquisition failure rolls back in reverse order and allows retry", () =>
{
    var released = new List<int>();
    var lifetime = new RuntimeLifetime(_ => { });
    var failure = new InvalidOperationException("injected acquisition failure");
    try
    {
        lifetime.Start(() =>
        {
            lifetime.Own(() => { }, () => released.Add(1));
            lifetime.Own(() => throw failure, () => released.Add(2));
        });
        throw new Exception("Startup unexpectedly succeeded");
    }
    catch (InvalidOperationException error) { Require(ReferenceEquals(error, failure)); }
    Require(released.SequenceEqual(new[] { 2, 1 }));
    Require(lifetime.Start(() => { }));
    lifetime.Stop();
});

Check("resource phase failure releases resource and core owners despite cleanup errors", () =>
{
    var released = new List<int>();
    int reported = 0;
    var lifetime = new RuntimeLifetime(_ => { reported++; throw new Exception("report failure"); });
    lifetime.Start(() => lifetime.Own(() => { }, () => released.Add(1)));
    try
    {
        lifetime.Continue(() =>
        {
            lifetime.Own(() => { }, () => { released.Add(2); throw new Exception("cleanup failure"); });
            throw new InvalidOperationException("resource phase failure");
        });
        throw new Exception("Continuation unexpectedly succeeded");
    }
    catch (InvalidOperationException) { }
    Require(released.SequenceEqual(new[] { 2, 1 }) && reported == 1);
    bool blocked=false;try { lifetime.Start(() => { }); } catch(InvalidOperationException) { blocked=true; }
    Require(blocked);
    lifetime.Stop();
    Require(released.SequenceEqual(new[] { 2, 1 }));
});

Check("stop during startup rejects completion and remains restartable", () =>
{
    int released = 0;
    var lifetime = new RuntimeLifetime(_ => { });
    try
    {
        lifetime.Start(() =>
        {
            lifetime.Own(() => { }, () => released++);
            lifetime.Stop();
        });
        throw new Exception("Stopped startup unexpectedly succeeded");
    }
    catch (InvalidOperationException) { }
    Require(released == 1);
    Require(lifetime.Start(() => { }));
    lifetime.Stop();
});

Check("unreadable record survives writes while independent keys remain writable", () =>
{
    var original = new Dictionary<string, string> { ["CorpPresence"] = "broken JSON" };
    PlayerStore.Instance = new PlayerStore { modData = original };
    SaveSlot.ProtectUnreadable("CorpPresence");
    SaveSlot.Write("CorpPresence", "replacement");
    SaveSlot.Write("Other", "valid");
    Require(original["CorpPresence"] == "broken JSON" && original["Other"] == "valid");
    SaveSlot.AcceptRead("CorpPresence");
    SaveSlot.Write("CorpPresence", "decoded successfully");
    Require(original["CorpPresence"] == "decoded successfully");
});

Check("read protection follows the actual dictionary across store switches", () =>
{
    var damaged = new Dictionary<string, string> { ["CorpPresence"] = "broken JSON" };
    PlayerStore.Instance = new PlayerStore { modData = damaged };
    SaveSlot.ProtectUnreadable("CorpPresence");
    PlayerStore.Instance = new PlayerStore();
    SaveSlot.Write("CorpPresence", "new save");
    Require(PlayerStore.Instance.modData["CorpPresence"] == "new save");
    PlayerStore.Instance.modData = damaged;
    Require(!SaveSlot.CanWrite("CorpPresence"));
    damaged.Remove("CorpPresence");
    Require(SaveSlot.Read("CorpPresence") == null && SaveSlot.CanWrite("CorpPresence"));
    PlayerStore.Instance = null;
    Require(SaveSlot.CurrentStore == null && !SaveSlot.CanWrite("CorpPresence"));
});

Console.WriteLine($"{passed} passed");

Check("order amount rejects overflow without changing accumulated total", () =>
{
    int total = 100;
    Require(!OrderAmounts.TryAdd(ref total, int.MaxValue, 2, 1, out int count));
    Require(total == 100 && count == 0);
    Require(!OrderAmounts.TryAdd(ref total, 1, 2, int.MaxValue, out count));
    Require(total == 100 && count == 0);
    Require(OrderAmounts.TryAdd(ref total, 200, 3, 4, out count));
    Require(total == 700 && count == 12);
});

Check("quota preparation never mutates history and rejects invalid counters", () =>
{
    var records = new Dictionary<string, int> { ["item@d10"] = 3 };
    Require(QuotaCounts.TryAdd(records, "item@d10", 2, out int next) && next == 5);
    Require(records["item@d10"] == 3);
    records["item@d10"] = int.MaxValue;
    Require(!QuotaCounts.TryAdd(records, "item@d10", 1, out _));
    records["item@d10"] = -1;
    Require(!QuotaCounts.TryAdd(records, "item@d10", 1, out _));
    Require(QuotaCounts.Used(records, "item", 7, 10, null) == int.MaxValue);
});

Check("rolling quota agrees with independent window sum across sparse and dense history", () =>
{
    var random = new Random(491);
    for (int trial = 0; trial < 1000; trial++)
    {
        int day = random.Next(-20, 100);
        int window = random.Next(1, 40);
        var records = new Dictionary<string, int>();
        int expected = 0;
        for (int at = -60; at < 120; at++)
        {
            if (random.Next(4) != 0) continue;
            int quantity = random.Next(1, 10);
            records.Add("item@d" + at, quantity);
            if (at <= day && at > day - window) expected += quantity;
        }
        records["other@d" + day] = 500;
        records["item@period"] = 900;
        Require(QuotaCounts.Used(records, "item", window, day, null) == expected);
    }
});

Console.WriteLine($"{passed} total passed");

Check("failed callback cleanup cannot be forgotten by reuse", () =>
{
    int attempts=0; Action current=()=>{}; Action owned=()=>{}; bool fail=false;
    var callbacks=new OwnedCallbacks(_=>{});
    callbacks.Replace(()=>current,value=>{attempts++;if(fail)throw new Exception();current=value;},owned);
    fail=true;callbacks.Release();fail=false;callbacks.Release();
    Require(ReferenceEquals(current,owned) && attempts==2);
    bool blocked=false;try{callbacks.Replace(()=>current,value=>current=value,new Action(()=>{}));}catch(InvalidOperationException){blocked=true;}
    Require(blocked);
});

Check("register audio failure cannot interrupt debit or repeat delivered cash", () =>
{
    var store = new PlayerStore { Cash = 1000 };
    CompanyMoney.Pay(store, 300);
    Require(store.Cash == 700 && store.DebitCalls == 1);
    var order = new Order { Cash = 200 };
    CompanyMoney.DeliverCash(store, order);
    CompanyMoney.DeliverCash(store, order);
    Require(store.Cash == 900 && store.CreditCalls == 1 && order.Cash == 0);
});

Console.WriteLine($"{passed} total passed");

Check("callback replacement restores previous owner and release is idempotent", () =>
{
    Func<int> original = () => 1, owned = () => 2;
    Func<int> current = original;
    var callbacks = new OwnedCallbacks(null);
    callbacks.Replace(() => current, value => current = value, owned);
    Require(current() == 2);
    callbacks.Release(); callbacks.Release();
    Require(ReferenceEquals(current, original));
});
Check("callback release preserves later replacement", () =>
{
    Func<int> current = () => 1, owned = () => 2, later = () => 3;
    var callbacks = new OwnedCallbacks(null);
    callbacks.Replace(() => current, value => current = value, owned);
    current = later;
    callbacks.Release();
    Require(ReferenceEquals(current, later));
});
Check("event release removes only owned callback and survives cleanup reporter failure", () =>
{
    int originalCalls = 0, ownedCalls = 0, laterCalls = 0;
    Action current = () => originalCalls++;
    Action owned = () => ownedCalls++;
    var callbacks = new OwnedCallbacks(_ => throw new Exception("report failure"));
    callbacks.Add(() => current, value => current = value, owned);
    current += () => laterCalls++;
    Func<int> broken = () => 1;
    bool reject = false;
    callbacks.Replace(() => broken, value => { if (reject) throw new Exception("release failure"); broken = value; }, new Func<int>(() => 2));
    reject = true;
    callbacks.Release(); current();
    Require(originalCalls == 1 && laterCalls == 1 && ownedCalls == 0);
});
Console.WriteLine($"{passed} total passed");
