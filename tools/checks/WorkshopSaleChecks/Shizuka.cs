using System.Text.Json;
using CorpPresence;

partial class Program
{
    static void CheckShizukaSales()
    {
        const string gelId = "corp_presence_sleep_moss_gel";
        const string crystalId = "corp_presence_light_salt_precipitate";
        const string gritId = "corp_presence_fine_silica_grit";
        Reset();
        Check(WorkshopSaleOffer.Find("first_gel")?.ItemId == gelId, "trial uses blue-vessel output");
        var gel = Stock(gelId, 4);
        var crystal = Stock(crystalId, 2);
        var grit = Stock(gritId, 2);
        CartelShizuka.Visited = false;
        Check(Quote("first_gel").Submit() == SaleStatus.Unavailable && Quote("cartel_gel").Submit() == SaleStatus.Unavailable, "Shizuka must visit first");
        CartelShizuka.Visited = true;
        CartelShizuka.HasBlueprint = LowerMachinist.Advanced = false;
        Check(Quote("cartel_precipitate").Submit() == SaleStatus.Unavailable && Quote("cartel_fine_grit").Submit() == SaleStatus.Unavailable, "stock order progression gates");
        var trial = Quote("first_gel");
        Check(Quote("cartel_gel").Submit() == SaleStatus.Paid && gel.unitCount == 2 && Cash == 65, "two gel stock order before trial");
        Check(trial.Submit() == SaleStatus.Changed, "stock sold invalidates open trial quote");
        SaveSlot.CurrentStore.Pay = amount => { SaveSlot.CurrentStore.Cash += 19; throw new Exception("partial credit"); };
        Check(Quote("first_gel").Submit() == SaleStatus.Partial && Cash == 84 && gel.unitCount == 1, "trial consumes one and records actual credit");
        Check(!WorkshopSaleOffer.GelOrderSettled(CorpState.Current), "partial payment keeps alcohol station locked");
        Reload();
        StoreStation.Day++;
        SaveSlot.CurrentStore.Pay = amount => { SaveSlot.CurrentStore.Cash += amount; throw new Exception("after credit"); };
        Check(Quote("first_gel").Submit() == SaleStatus.Paid && Cash == 120 && gel.unitCount == 1, "pay remaining 36 on next day with no new gel");
        Reload();
        Check(WorkshopSaleOffer.GelOrderSettled(CorpState.Current) && Quote("first_gel").Check() != SaleStatus.Ready, "alcohol station stays unlocked after reload");
        CartelShizuka.HasBlueprint = true;
        Check(Quote("cartel_precipitate").Submit() == SaleStatus.Paid && crystal.Dead && Cash == 212, "blueprint opens precipitate order");
        LowerMachinist.Advanced = true;
        Check(Quote("cartel_fine_grit").Submit() == SaleStatus.DailyLimit, "cartel stock choices share one daily slot");
        StoreStation.Day++;
        Check(Quote("cartel_fine_grit").Submit() == SaleStatus.Paid && grit.Dead && Cash == 287, "classifier catalog opens grit cash order");
        Stock("wire", 2);
        Check(Quote("wire").Submit() == SaleStatus.Paid && Cash == 351, "Cen and cartel daily quotas independent");
        Check(Quote("first_gel").Submit() != SaleStatus.Paid && gel.unitCount == 1 && Cash == 351, "trial cannot recur on a later day");
        var workshop = CorpState.Current.Workshop;
        Check(workshop.GelOrder.ValidForFirstGel() && workshop.CartelDaily.ValidForCartelDaily()
            && !workshop.GelOrder.ValidForCartelDaily() && !workshop.CartelDaily.ValidForFirstGel()
            && !workshop.GelOrder.Valid(false), "saved receipts validated against their own slot");

        Reset();
        var item = Stock(gelId, 1);
        item.parentInventory.Detached = true;
        Check(Quote("first_gel").Submit() == SaleStatus.Short && Cash == 0, "virtual stock cannot fill trial");
        item.parentInventory.Detached = false;
        trial = Quote("first_gel");
        SaveSlot.CurrentStore.Cash = int.MaxValue - 54;
        Check(trial.Submit() == SaleStatus.WalletFull && !item.Dead, "trial checks wallet before collection");
        SaveSlot.CurrentStore.Cash = 0;
        SaveSlot.Writable = false;
        Check(trial.Submit() == SaleStatus.Unavailable && !item.Dead, "protected save blocks trial");
        SaveSlot.Writable = true;
        StoreSession.Revision++;
        Check(trial.Submit() == SaleStatus.Unavailable && !item.Dead, "stale trial does not touch stock");
        string snapshot = null;
        SaveSlot.CurrentStore.Pay = amount => {
            SaveSlot.CurrentStore.Cash += amount;
            snapshot = JsonSerializer.Serialize(CorpState.Current.Workshop);
            Check(Quote("first_gel").Submit() == SaleStatus.Unavailable, "reentrant trial cannot pay again");
        };
        Check(Quote("first_gel").Submit() == SaleStatus.Paid && item.Dead && Cash == 55, "ordinary trial");
        CorpState.Current.Workshop = JsonSerializer.Deserialize<WorkshopState>(snapshot);
        Check(Quote("first_gel").Submit() == SaleStatus.Frozen && Cash == 55
            && !WorkshopSaleOffer.GelOrderSettled(CorpState.Current), "ambiguous mid-credit snapshot stays frozen");

        var damaged = CorpState.Current.Workshop.GelOrder;
        damaged.InFlight = false;
        damaged.Received = int.MaxValue;
        SaveSlot.Writable = false;
        Check(Quote("first_gel").Check() == SaleStatus.Unavailable, "protected damaged receipt can be displayed");
        SaveSlot.Writable = true;
        Check(Quote("first_gel").Submit() == SaleStatus.Frozen && Cash == 55, "damaged quantities cannot be resumed");
    }
}
