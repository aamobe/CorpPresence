using CorpPresence;

partial class Program
{
    private static void CheckRotations()
    {
        Reset();
        MeridianIndustrial.Forge = true;
        Check(!new RotationQuote().CanSign, "rotation needs a fully paid first overhaul");
        Stock("corp_presence_sleep_moss_gel", 1);
        new WorkshopSaleQuote(WorkshopSaleOffer.FirstGel).Submit();
        StoreStation.Day = 10;
        new FirstOverhaulQuote().Sign();
        Stock(Frame, 2);
        Stock(Contact, 4);
        Stock(Unit, 2).Fresh = true;
        new FirstOverhaulQuote().Submit();
        new FirstOverhaulQuote().Submit();
        Check(Overhaul.CompletedDay == 10 && !new RotationQuote().CanSign, "first paid day excluded");

        StoreStation.Day = 11;
        var q = new RotationQuote();
        Check(q.CanSign && q.Window == 0 && q.Kind == RotationKind.Expansion && q.Sign() == SaleStatus.Ready
            && q.Sign() == SaleStatus.Unavailable, "first rotation day and single signature");
        Check(new RotationQuote().Abandon() == SaleStatus.Ready, "suspend expansion");
        Reload();
        StoreStation.Day = 13;
        Check(new RotationQuote().Sign() == SaleStatus.Ready && new RotationQuote().Contract.SignedDay == 11,
            "third day resumes original quote");
        var obsolete = new RotationQuote();
        StoreStation.Day = 14;
        q = new RotationQuote();
        Check(obsolete.Submit() == SaleStatus.Unavailable && q.Window == 1 && q.Kind == RotationKind.Expansion
            && q.DueDay == 19 && !q.CanSign, "active expansion persists into supplies window");
        Stock(Frame, 4);
        Stock("corp_presence_heavy_screen_pad", 2);
        int before = Cash;
        Check(new RotationQuote().Submit() == SaleStatus.Paid && Cash == before + 680, "expansion delivery and payment");

        q = new RotationQuote();
        Check(q.Contract == null && q.Kind == RotationKind.Supplies && q.Sign() == SaleStatus.Ready, "open current supply window");
        Stock("corp_presence_fine_silica_grit", 3);
        Stock("corp_presence_filter_liner", 2);
        Stock("corp_presence_sleep_moss_gel", 2);
        before = Cash;
        SaveSlot.CurrentStore.Pay = amount => SaveSlot.CurrentStore.Cash += 17;
        Check(new RotationQuote().DueDay == 20 && new RotationQuote().Submit() == SaleStatus.Partial && Cash == before + 17,
            "supplies received, only actual partial transfer recorded");
        Reload();
        var unpaid = new RotationQuote();
        Check(unpaid.Slots.Length == 0 && !unpaid.CanAbandon && unpaid.Contract.Trucks[0].Paid == 17,
            "reload keeps all supply materials and first seventeen paid");
        int remainder = 0;
        SaveSlot.CurrentStore.Pay = amount => { remainder = amount; SaveSlot.CurrentStore.Cash += amount; };
        Check(unpaid.Submit() == SaleStatus.Paid && remainder == 313 && Cash == before + 330,
            "reloaded supplies settle the exact balance without a second collection");
        SaveSlot.CurrentStore.Pay = amount => SaveSlot.CurrentStore.Cash += amount;
        Reload();
        Check(!new RotationQuote().CanSign && new RotationQuote().Sign() == SaleStatus.Paid && Cash == before + 330,
            "settled window cannot reissue");

        StoreStation.Day = 17;
        new RotationQuote().Sign();
        var old = new RotationQuote().Contract;
        old.Shells[0].Issued = true;
        Stock(Contact, 2);
        before = Cash;
        Check(old.Kind == RotationKind.Repair && new RotationQuote().DueDay == 21 && new RotationQuote().Submit() == SaleStatus.Paid
            && Cash == before + 105, "first repair truck independent from shell pickup");
        Check(new RotationQuote().Abandon() == SaleStatus.Ready, "exit after paid first truck");
        StoreStation.Day = 19;
        Check(new RotationQuote().Sign() == SaleStatus.Ready && new RotationQuote().TruckIndex == 1
            && new RotationQuote().DueDay == 26 && new RotationQuote().Abandon() == SaleStatus.Ready, "repair resumes in window");
        StoreStation.Day = 20;
        q = new RotationQuote();
        Check(q.Kind == RotationKind.Joint && q.Contract == null && q.Sign() == SaleStatus.Ready, "expired repair stays retired");
        Stock(Frame, 2);
        Stock(Contact, 4);
        Check(new RotationQuote().Submit() == SaleStatus.Paid, "joint first truck");
        var fromOld = TaggedUnit("test", old.Serial, 0);
        Stock(Unit, 1).Fresh = true;
        Check(new RotationQuote().Check() == SaleStatus.Short && !fromOld.Dead, "joint excludes old serial and unmarked units");
        Stock(Unit, 1).Fresh = true;
        before = Cash;
        Check(new RotationQuote().Submit() == SaleStatus.Paid && Cash == before + 480, "joint final truck requires two fresh units");

        StoreStation.Day = 29;
        q = new RotationQuote();
        Check(q.Window == 6 && q.Kind == RotationKind.Repair && q.Sign() == SaleStatus.Ready, "skipped windows keep calendar cadence");
        q = new RotationQuote();
        var repair = q.Contract;
        repair.Shells[0].Issued = repair.Shells[1].Issued = true;
        StoreStation.Day = 34;
        before = Cash;
        Stock(Contact, 2);
        Check(new RotationQuote().Submit() == SaleStatus.Paid && Cash == before + 90, "overdue first repair truck loses only bonus");
        var tagged = TaggedUnit("test", repair.Serial, 0);
        var twin = TaggedUnit("test", repair.Serial, 0);
        TaggedUnit("foreign", repair.Serial, 1);
        Check(new RotationQuote().Stock.Count == 2 && new RotationQuote().Submit() == SaleStatus.Changed
            && !tagged.Dead && !twin.Dead, "foreign run and old serial excluded, duplicate shell slots rejected before collection");
        var fresh = Stock(Unit, 1);
        fresh.Fresh = true;
        q = new RotationQuote();
        q.Sources[1] = q.Stock.FindIndex(stock => stock.Item == fresh);
        fresh.parentInventory.Remove = _ => throw new Exception("second item blocked");
        StoreStation.Day = 38;
        q = new RotationQuote();
        q.Sources[1] = q.Stock.FindIndex(stock => stock.Item == fresh);
        Check(q.Amount == 365 && q.Submit() == SaleStatus.Partial && repair.Shells[0].Delivered && !repair.Shells[1].Delivered,
            "only actually received numbered shell retired, inclusive day-nine price");
        Reload();
        q = new RotationQuote();
        repair = q.Contract;
        Check(!q.CanAbandon && q.Stock.All(stock => stock.Item != twin) && repair.Shells[0].Delivered,
            "reloaded partial truck blocks cancellation and duplicate shell");
        StoreStation.Day = 39;
        fresh.parentInventory.Remove = item => item.parentInventory = null;
        before = Cash;
        Check(new RotationQuote().Submit() == SaleStatus.Paid && Cash == before + 365
            && repair.CompletedDay == 39 && !repair.LiningClaimed, "fresh alternative settles once at frozen price");
        Check(CorpState.Current.Workshop.Rotations.Count == 5 && CorpState.Current.Workshop.Valid(), "retired contract and shells remain recorded");
        repair.Shells[0].Returned = true;
        Check(!CorpState.Current.Workshop.Valid(), "one shell cannot be delivered and returned");
        repair.Shells[0].Returned = false;
        repair.Window--;
        Check(!CorpState.Current.Workshop.Valid(), "corrupt serial/window relation protected");
    }

    private static GameItem TaggedUnit(string run, int serial, int slot)
    {
        var item = Stock(Unit, 1);
        item.OriginRun = run;
        item.OriginSerial = serial;
        item.OriginSlot = slot;
        return item;
    }
}
