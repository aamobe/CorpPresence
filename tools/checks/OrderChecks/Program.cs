using CorpPresence;
void Require(bool condition) { if (!condition) throw new Exception("Order invariant failed"); }
var company = new TestCompany();
var entry = new CatalogEntry();
company.TestCatalog.Add(entry);
var cart = new Dictionary<CatalogEntry,int> { [entry] = 2 };
Services.Payment = () => "insufficient funds";
Require(Services.PlaceOrder(company,cart,_ => 100) != null);
Require(Services.Charges == 0 && Companies.State.Orders.Count == 0 && Companies.State.LimitedBought.Count == 0);
Services.Payment = null;
Require(Services.PlaceOrder(company,cart,_ => 100) == null);
Require(Services.Charges == 1 && Companies.State.Orders.Single().Items.Count == 2);
Require(Services.PlaceOrder(company,cart,_ => 100) != null && Services.Charges == 1);
Console.WriteLine("PASS failed payment preserves state; success and repeat submission charge once");
Companies.State = new();
Services.Charges = 0;
bool inside = false;
Services.Payment = () => {
 if (!inside) {
  inside = true;
  Services.PlaceOrder(company,cart,_ => 100);
 }
 return null;
};
Services.PlaceOrder(company,cart,_ => 100);
Console.WriteLine($"reentrant charges={Services.Charges}; orders={Companies.State.Orders.Count}");
Require(Services.Charges == 1 && Companies.State.Orders.Count == 1);

foreach (bool failDuringPayment in new[] { false, true })
{
    Companies.State = new();
    Services.Charges = 0;
    Services.Payment = failDuringPayment ? () => throw new InvalidOperationException("payment probe") : null;
    if (failDuringPayment)
        Require(Services.PlaceOrder(company, cart, _ => 100) != null); // Real Pay catches extension uncertainty.
    else
    {
        bool quoteFailed = false;
        try { Services.PlaceOrder(company, cart, _ => throw new InvalidOperationException("quote probe")); }
        catch (InvalidOperationException) { quoteFailed = true; }
        Require(quoteFailed);
    }
    Require(Services.Charges == 0 && Companies.State.Orders.Count == 0);
    Services.Payment = null;
    Require(Services.PlaceOrder(company, cart, _ => 100) == null);
    Require(Services.Charges == 1 && Companies.State.Orders.Count == 1);
}
Console.WriteLine("PASS quote/payment exceptions release the guard for a later order");

foreach (int quantity in new[] { 0, -1, 11, int.MaxValue })
{
    Companies.State = new();
    Services.Charges = 0;
    Require(Services.PlaceOrder(company, new() { [entry] = quantity }, _ => 100) != null);
    Require(Services.Charges == 0 && Companies.State.Orders.Count == 0 && Companies.State.LimitedBought.Count == 0);
}
Console.WriteLine("PASS invalid or over-quota quantities cannot charge or reserve quota");

Companies.State = new();
Services.Charges = 0;
var previousState = Companies.State;
string switchedResult = Services.PlaceOrder(company, cart, _ =>
{
    Companies.State = new();
    return 100;
});
Console.WriteLine($"state replacement charges={Services.Charges}; old orders={previousState.Orders.Count}");
Require(switchedResult != null && Services.Charges == 0 && previousState.Orders.Count == 0);

Companies.State = new();
previousState = Companies.State;
Services.Charges = 0;
Services.Payment = () => { Companies.State = new(); return null; };
Services.PlaceOrder(company, cart, _ => 100);
Console.WriteLine($"payment switch: current cooperation={Companies.State.Cooperation}; current orders={Companies.State.Orders.Count}");
Require(Companies.State.Cooperation == 0 && Companies.State.Orders.Count == 0);
Require(previousState.Orders.Count == 1 && Services.Charges == 1);

Companies.State=new();Services.Charges=0;Services.Payment=()=>{PlayerStore.Instance.PayMoney(100,false);throw new InvalidOperationException();};
Require(Services.PlaceOrder(company,cart,_=>100)!=null);
Require(Services.Charges==1 && Companies.State.Orders.Count==0 && Companies.State.LimitedBought.Count==0);
Console.WriteLine("PASS real payment uncertainty does not supplement debit or reserve quota");


// Actual equipment rule + actual order orchestration; game inventory probing remains a substitute.
Services.Payment = null;
var jackson = new JacksonContact();
var warehouse = new CatalogEntry { Id = CargoWarehouse.OrderId };
jackson.TestCatalog.Add(warehouse);
Companies.State = new(); Services.Charges = 0;
CargoWarehouse.IsUnlocked = false;
Require(Services.Left(Companies.State, warehouse) == 0);
Require(Services.PlaceOrder(jackson, new() { [warehouse] = 1 }, _ => 1) != null && Services.Charges == 0);
CargoWarehouse.IsUnlocked = true;
Require(Services.PlaceOrder(company, new() { [warehouse] = 1 }, _ => 1) != null && Services.Charges == 0);
Require(Services.PlaceOrder(jackson, new() { [warehouse] = 1, [new CatalogEntry { Id = warehouse.Id }] = 1 }, _ => 1) != null && Services.Charges == 0);
int paid = 0;
Services.Payment = () => "";
CorpPresence.Malice.Malice.Handler = (_, amount, _) => { paid = amount; return ""; };
Require(Services.PlaceOrder(jackson, new() { [warehouse] = 1 }, _ => 1) == null);
Require(paid == CargoWarehouse.WarehousePrice && Companies.State.LimitedBought[warehouse.Id + "@lifetime"] == 1);
Require(Services.Left(Companies.State, warehouse) == 0);
Companies.State = new();
var controller = new CatalogEntry { Id = CargoWarehouse.ControllerOrderId };
jackson.TestCatalog.Add(controller);
Require(Services.PlaceOrder(jackson, new() { [controller] = 1 }, _ => 1) == null);
Require(Companies.State.LimitedBought.Count == 0 && Services.Left(Companies.State, controller) == 0);
Console.WriteLine("PASS equipment rules enforce unlock, seller, duplicate identity, fixed price, lifetime count and queued controller reservation");
Services.Payment = null;

Companies.State = new(); Services.Charges = 0;
company.EntryError = "content locked";
Require(Services.PlaceOrder(company, cart, _ => throw new Exception("must not quote locked goods")) == "content locked");
Require(Services.Charges == 0 && Companies.State.Orders.Count == 0 && Companies.State.LimitedBought.Count == 0);
company.EntryError = null;
Console.WriteLine("PASS shared content eligibility rejects before quote, payment and reservation");

foreach (int stalePrice in new[] { 1, 99, 101, int.MaxValue })
{
    Companies.State = new(); Services.Charges = 0; Services.Payment = null;
    Require(Services.PlaceOrder(company, cart, _ => stalePrice) != null);
    Require(Services.Charges == 0 && Companies.State.Orders.Count == 0 && Companies.State.LimitedBought.Count == 0);
}
Console.WriteLine("PASS stale or forged ordinary quotes cannot debit or reserve quota");

var solana = new SolanaCo();
foreach (int price in new[] { 220, 2400, 1200, 1600, 400 })
foreach (int cooperation in new[] { 50, 100 })
foreach (double factor in new[] { 1.0, 1.5 }) {
    Companies.State = new() { Coop = cooperation };
    Services.Charges = 0;
    Services.Payment = null;
    CorpPresence.Malice.Envoy.Factor = factor;
    var supply = new CatalogEntry { Id = "@supply_" + price, FixedValue = price, MinTier = 2 };
    Perfume.Catalog.Add(supply);
    solana.TestCatalog.Add(supply);
    Require(solana.OrderUnitPrice(supply, Companies.State) == price);
    int cashBefore = PlayerStore.Instance.Cash;
    Require(Services.PlaceOrder(solana, new() { [supply] = 1 }, Perfume.CatalogPrice) == null);
    Require(cashBefore - PlayerStore.Instance.Cash == price && Services.Charges == 1);
    Require(Companies.State.Orders.Single().Items.Single() == supply.Id);
    Companies.State = new() { Coop = cooperation };
    Services.Charges = 0;
    Require(Services.PlaceOrder(solana, new() { [supply] = 1 }, _ => price - 1) != null);
    Require(Services.Charges == 0 && Companies.State.Orders.Count == 0 && Companies.State.LimitedBought.Count == 0);
}
Console.WriteLine("PASS five fixed supplies at Gold/Black Gold tiers and different corporate multipliers");
CorpPresence.Malice.Envoy.Factor = 1;
