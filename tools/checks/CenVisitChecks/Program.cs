using CorpPresence;

static void Check(bool ok, string detail)
{
    if (!ok) throw new Exception(detail);
    Console.WriteLine("PASS " + detail);
}
var cen = new LowerMachinist();
var root = CorpState.Current;
var store = SaveSlot.CurrentStore;
var progress = root.Of(cen.Id);
Engine.OnDayEnd();
Check(progress.PendingDay == -1, "rep 9 cannot book Cen");
Engine.LowerRep = 10;
Engine.OnDayEnd();
Check(progress.PendingKind == "ch1" && progress.PendingDay == 1, "rep 10 books first visit even without cash");
static bool Purchase(Visit visit)
{
    visit.Choices[0].Then();
    return visit.TryComplete();
}
Check(!Purchase(cen.Chapter(1)), "cannot receive equipment on credit");
store.Cash = 1000;
CompanyVisitors.Full = true;
Check(!Purchase(cen.Chapter(1)) && store.Cash == 1000 && store.Counter.Count == 0, "full counter keeps cash");
CompanyVisitors.Full = false;
Check(Purchase(cen.Chapter(1)) && store.Cash == 700 && store.Counter.Single().Contents.Count == 6,
    "first delivery places three machines and three boxes for 300");
progress.Chapter = 1; // Native dialogue advances only after TryComplete succeeds.
progress.PendingDay = -1;
progress.PendingKind = null;
StoreStation.Day = 4;
var owned = EmporiumEntry.Instance.Items;
var scrap = CrusherBatch.Parts(CrusherBatch.Classify("scrap_metal"), 0).ToArray();
var mixed = new GameItem("powder_box", Array.ConvertAll(scrap, x => x * 3));
owned.Add(mixed);
Check(LowerMachinist.QualifiedStock() == 0, "three scrap give 300 points but only 75% metal");
int[] sorted = PowderSieveRules.Sort(mixed.Parts, PowderSieveRules.RecoveryRate(0));
var sieve = new GameItem(PowderSieve.Key, sorted);
owned.Clear();
owned.AddRange(new[] {sieve, sieve, new GameItem("bound_display", sorted)});
var bins = new long[6];
Check(LowerMachinist.QualifiedStock(bins) == 148 && bins.SequenceEqual(new long[] {112,15,7,7,7,0}),
    "sorting one scrap kit credits each pure bin once");
Engine.OnDayEnd();
Check(progress.PendingDay == -1, "148 eligible points do not book second visit");
owned.Clear();
var borderline = new GameItem("powder_box", new[] {80,19,0,0,0});
owned.Add(borderline);
Check(LowerMachinist.QualifiedStock() == 0, "80.80% rounds down for eligibility");
var exact = new GameItem("powder_box", new[] {243,57,0,0,0});
owned.AddRange(new[] {exact, exact});
var detail = new List<LowerMachinist.StockLine>();
Check(LowerMachinist.QualifiedStock(null, detail) == 300 && detail.Count == 2 && detail[0].Qualified == 0 && detail[1].Qualified == 300, "audit lists each loose box, explains rejected purity, and agrees with visit tally");
Check(LowerMachinist.QualifiedStock() == 300, "exactly 81% qualifies and repeated references count once");
owned.Clear();
var glass = new GameItem("powder_box", Array.ConvertAll(CrusherBatch.Parts(CrusherBatch.Classify("empty_beer_bottle"), 0).ToArray(), x => x * 3));
owned.Add(glass);
store.Cash = 449;
Engine.OnDayEnd();
Check(progress.PendingKind == "ch2" && !CartelShizuka.MineralizerUnlocked(root), "three bottles book visit without granting blueprint early");
Check(!Purchase(cen.Chapter(2)) && store.Cash == 449, "second equipment offer can be retried");
store.Cash = 450;
CompanyMoney.ThrowAfterPayment = true;
Check(Purchase(cen.Chapter(2)) && store.Counter[1].Contents.SequenceEqual(new[] {"crafting_station","mineral_growth_station","electronic_blueprint","mineral_ore_blueprint"})
    && store.Cash == 0 && glass.Parts.Sum() == 300, "second offer costs 450, delivers both machines and plans, keeps the powder");
var shizuka = new CartelShizuka();
var giftId = shizuka.Chapter(2).Gift.Single();
Check(!shizuka.DeliverGift(giftId), "Cartel gift factory cannot bypass the mineralizer unlock");
progress.Chapter = 2;
CompanyVisitors.Full = true;
Check(!shizuka.DeliverGift(giftId), "Cartel blueprint remains pending when the player's counter is full");
CompanyVisitors.Full = false;
Check(shizuka.DeliverGift(giftId) && store.Counter.Last().Id == giftId, "post-unlock blueprint goes to the same persistent counter as the machines");
Check(CartelShizuka.MineralizerUnlocked(root) && new CartelShizuka().Chapter(2).Gift.Single() == "corp_presence_light_salt_blueprint",
    "completed second purchase opens the existing Cartel blueprint handoff");
