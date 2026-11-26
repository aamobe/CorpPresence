using System;

namespace CorpPresence;

internal enum WorkshopSaleBook { Product, Powder, FirstGel, CartelDaily, Contacts, Rods, MeridianFeeder }

internal sealed class WorkshopSaleOffer
{
    internal string Id { get; }
    internal string ItemId { get; }
    internal int Component { get; }
    internal int Quantity { get; }
    internal int Price { get; }
    internal bool IsPowder => Component >= 0;
    internal bool IsAlloy => Book is WorkshopSaleBook.Rods or WorkshopSaleBook.MeridianFeeder;
    internal WorkshopSaleBook Book { get; }

    private WorkshopSaleOffer(string id, string itemId, int component, int quantity, int price,
        WorkshopSaleBook book = WorkshopSaleBook.Product)
    {
        Id = id; ItemId = itemId; Component = component; Quantity = quantity; Price = price;
        Book = component >= 0 ? WorkshopSaleBook.Powder : book;
    }

    internal static readonly WorkshopSaleOffer[] All =
    {
        new("hearth_tile", "corp_presence_hearth_tile", -1, 1, 24),
        new("wire", "wire", -1, 2, 64),
        new("silicon_plate", "corp_presence_silicon_plate", -1, 2, 70),
        new("metal", null, 0, 60, 22),
        new("silica", null, 1, 60, 18),
        new("rare", null, 2, 10, 16),
        new("conductive", null, 3, 20, 16),
        new("residue", null, 4, 40, 12)
    };

    internal static readonly WorkshopSaleOffer RepairContacts =
        new("pump_contact", "corp_presence_pump_contact", -1, 2, 76, WorkshopSaleBook.Contacts);
    internal static readonly WorkshopSaleOffer BatchRod = new("rod_batch_stock", null, -1, 1, 24, WorkshopSaleBook.Rods);
    internal static readonly WorkshopSaleOffer FatigueRod = new("rod_fatigue_stock", null, -1, 1, 50, WorkshopSaleBook.Rods);
    internal static readonly WorkshopSaleOffer HardRod = new("rod_hard_stock", null, -1, 1, 52, WorkshopSaleBook.Rods);
    internal static readonly WorkshopSaleOffer ConductorRod = new("rod_conductor_stock", null, -1, 1, 46, WorkshopSaleBook.Rods);
    internal static readonly WorkshopSaleOffer MeridianFeederSpec =
        new("meridian_feeder_spec", "corp_presence_feed_shaft", -1, 1, 68, WorkshopSaleBook.MeridianFeeder);
    internal const int FrequentStartRodQuote = 54;
    internal static readonly WorkshopSaleOffer[] RodOrders = { BatchRod, FatigueRod, HardRod, ConductorRod };
    internal static readonly WorkshopSaleOffer[] PartOrders =
    {
        new("part_feed_shaft", "corp_presence_feed_shaft", -1, 1, 68, WorkshopSaleBook.Rods),
        new("part_feed_sleeve", "corp_presence_feed_sleeve", -1, 1, 38, WorkshopSaleBook.Rods),
        new("part_industrial_coil", "corp_presence_industrial_coil", -1, 1, 72, WorkshopSaleBook.Rods),
        new("part_axial_lead", "corp_presence_axial_lead", -1, 1, 52, WorkshopSaleBook.Rods),
        new("part_return_guide", "corp_presence_sieve_return_guide", -1, 1, 56, WorkshopSaleBook.Rods),
        new("part_filter_basket", "corp_presence_pump_filter_basket", -1, 1, 64, WorkshopSaleBook.Rods)
    };
    internal static readonly WorkshopSaleOffer FirstGel =
        new("first_gel", "corp_presence_sleep_moss_gel", -1, 1, 55, WorkshopSaleBook.FirstGel);
    internal static readonly WorkshopSaleOffer[] CartelDaily =
    {
        new("cartel_gel", FirstGel.ItemId, -1, 2, 65, WorkshopSaleBook.CartelDaily),
        new("cartel_precipitate", "corp_presence_light_salt_precipitate", -1, 2, 92, WorkshopSaleBook.CartelDaily),
        new("cartel_fine_grit", "corp_presence_fine_silica_grit", -1, 2, 75, WorkshopSaleBook.CartelDaily)
    };

    internal static WorkshopSaleOffer Find(string id) => id == RepairContacts.Id ? RepairContacts : id == FirstGel.Id ? FirstGel
        : id == MeridianFeederSpec.Id ? MeridianFeederSpec
        : Array.Find(All, o => o.Id == id) ?? Array.Find(CartelDaily, o => o.Id == id)
            ?? Array.Find(RodOrders, o => o.Id == id) ?? Array.Find(PartOrders, o => o.Id == id);
    internal static bool GelOrderSettled(CorpState root) =>
        root?.Workshop?.GelOrder is { Complete: true } receipt && receipt.ValidForFirstGel();
    internal int PremiumPrice => Price * 5 / 4;
}

internal sealed class WorkshopSaleState
{
    public WorkshopSaleReceipt Contacts { get; set; }
    public WorkshopSaleReceipt Rods { get; set; }
    public WorkshopSaleReceipt Product { get; set; }
    public WorkshopSaleReceipt Powder { get; set; }
    internal WorkshopSaleReceipt Get(bool powder) => powder ? Powder : Product;
    internal void Put(bool powder, WorkshopSaleReceipt receipt)
    {
        if (powder) Powder = receipt;
        else Product = receipt;
    }
    internal bool Valid() => (Contacts == null || Contacts.ValidFor(WorkshopSaleBook.Contacts)) && (Product == null || Product.Valid(false))
        && (Powder == null || Powder.Valid(true)) && (Rods == null || Rods.ValidFor(WorkshopSaleBook.Rods));
}

internal sealed class WorkshopSaleReceipt
{
    public int Day { get; set; } = -1;
    public int? CompletedDay { get; set; }
    public string Offer { get; set; }
    public int Amount { get; set; }
    public int Received { get; set; }
    public int Paid { get; set; }
    // 原生钱包/库存回调期间取得的快照，重读时无法判定结果；保留并冻结此凭单。
    public bool InFlight { get; set; }
    internal bool Complete => !InFlight && Paid == Amount;
    internal int QuotaDay => CompletedDay ?? Day;

    internal bool Valid(bool powder)
        => ValidFor(powder ? WorkshopSaleBook.Powder : WorkshopSaleBook.Product);

    internal bool ValidForFirstGel() => ValidFor(WorkshopSaleBook.FirstGel);
    internal bool ValidForCartelDaily() => ValidFor(WorkshopSaleBook.CartelDaily);

    internal bool ValidFor(WorkshopSaleBook book)
    {
        var offer = WorkshopSaleOffer.Find(Offer);
        return Day >= 0 && offer?.Book == book
            && (Amount == offer.Price || offer.IsPowder && Amount == offer.PremiumPrice
                || offer == WorkshopSaleOffer.FatigueRod && Amount == WorkshopSaleOffer.FrequentStartRodQuote)
            && Received >= 0 && Received <= offer.Quantity && Paid >= 0 && Paid <= Amount
            && (CompletedDay == null || (Complete && CompletedDay >= Day))
            && (book is WorkshopSaleBook.Product or WorkshopSaleBook.Powder || !Complete || CompletedDay.HasValue)
            && (Paid == 0 || Received == offer.Quantity);
    }
}
