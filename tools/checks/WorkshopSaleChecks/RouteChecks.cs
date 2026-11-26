using CorpPresence;

partial class Program
{
    // The native split helper is called only on a single-component bin or its filled box.
    static int[] DrawFromPurePowder(int[] parts, int amount)
    {
        int component = Array.FindIndex(parts, points => points > 0);
        if (parts.Count(points => points > 0) != 1 || amount < 0 || amount > parts[component])
            throw new InvalidOperationException("Expected a draw from pure powder.");
        var taken = new int[parts.Length];
        taken[component] = amount;
        return taken;
    }

    static void CheckSalvageBasketRoute()
    {
        Reset();
        LowerMachinist.Advanced = false;
        // Item identities after opening the salvage starter crate. The delivery check
        // in RestorationChecks also verifies the catalog's three ordered pieces.
        string[] items = { "scrap_metal", "corp_presence_salvage_electronics", "corp_presence_spent_lining" };
        var feedBox = new int[PowderBox.ComponentCount];
        foreach (string item in items)
        {
            var feed = CrusherBatch.Classify(item);
            Check(feed != CrusherFeed.None && CrusherBatch.CanFitFeed(feedBox.Sum(), feed, 0), "crusher accepts " + item);
            var batch = CrusherBatch.Parts(feed, 0);
            for (int i = 0; i < feedBox.Length; i++) feedBox[i] += batch[i];
        }
        Check(items.Length <= CrusherBatch.NightlyLimit(0) && feedBox.Sum() == PowderSieveRules.BaseThroughput,
            "base crusher and sieve each handle the basket in one cycle");
        var recovered = PowderSieveRules.Sort(feedBox, PowderSieveRules.RecoveryRate(0));
        int residue = (int)PowderComponent.Residue;
        var bin = new int[feedBox.Length];
        bin[residue] = recovered[residue];
        var recipe = SinteringRecipes.All.Single(r => r.Id == "hearth_tile");
        var emptyBox = new int[bin.Length];
        int draw = PowderTransfer.ToGoal(bin, emptyBox, PowderSieveRules.BinCapacity, recipe.Ingredients.Single(), DrawFromPurePowder);
        Check(bin[residue] == 60 && draw == 40, "salvage yields enough sorted residue for a tile");
        var transfer = PowderTransfer.Plan(bin, emptyBox, PowderSieveRules.BinCapacity, draw, DrawFromPurePowder);
        var fired = SinteringBatch.Plan(new[] { transfer.TargetAfter }, recipe.Ingredients, DrawFromPurePowder);
        Check(fired != null && fired.Used.Single() && fired.Remaining.Single().Sum() == 0 && transfer.SourceAfter[residue] == 20,
            "firing the measured box leaves twenty points in the sieve");
        var tile = Stock(recipe.Product, 1);
        Check(Quote(recipe.Id).Submit() == SaleStatus.Paid && tile.Dead && Cash == 24,
            "Cen buys the sintering product before the second visit for 24");
    }
}
