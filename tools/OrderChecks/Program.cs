using CorpPresence;
void Require(bool condition) { if (!condition) throw new Exception("Order invariant failed"); }
var company = new Company();
var entry = new CatalogEntry();
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
