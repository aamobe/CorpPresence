using System;
using CorpPresence;
static void Check(bool pass) { if (!pass) throw new Exception("Powder color assertion failed"); }
Check(PowderColor.Level(0) == 0);
Check(PowderColor.Level(1) == 1 && PowderColor.Level(100) == 1 && PowderColor.Level(101) == 2);
Check(PowderColor.Level(500) == 5 && PowderColor.Level(600) == 5);
Check(PowderColor.Mix(new[] {100, 0, 0, 0, 0}) == 0xB8C1CA);
Check(PowderColor.Mix(new[] {0, 100, 0, 0, 0}) == 0xF2F0E8);
Check(PowderColor.Mix(new[] {50, 50, 0, 0, 0}) == 0xD5D9D9);
Console.WriteLine("6 powder color checks passed.");

foreach (var (roll, expected) in new[] {
    (0, "25,20,3,7,45"), (39, "25,20,3,7,45"), (40, "45,15,5,10,25"),
    (64, "45,15,5,10,25"), (65, "15,45,2,8,30"), (89, "15,45,2,8,30"),
    (90, "20,25,8,22,25"), (99, "20,25,8,22,25")
})
    Check(string.Join(",", CrusherBatch.Parts(CrusherFeed.Junk, roll).ToArray()) == expected);
Check(string.Join(",", CrusherBatch.Parts(CrusherFeed.Scrap, 0).ToArray()) == "75,10,5,5,5");
foreach (string id in new[] { "empty_beer_bottle", "glass_shard" })
{
    var glassFeed = CrusherBatch.Classify(id);
    Check(glassFeed == CrusherFeed.Glass);
    Check(string.Join(",", CrusherBatch.Parts(glassFeed, 0).ToArray()) == "0,85,0,0,15");
    Check(CrusherBatch.CanFitFeed(415, glassFeed, 100) && !CrusherBatch.CanFitFeed(416, glassFeed, 100));
}
Check(CrusherBatch.Classify("wine_bottle") == CrusherFeed.None);
Check(CrusherBatch.Fits(400) && !CrusherBatch.Fits(401) && !CrusherBatch.Fits(500));
Check(PowderColor.Mix(CrusherBatch.Parts(CrusherFeed.Scrap, 0)) != PowderColor.Mix(CrusherBatch.Parts(CrusherFeed.Junk, 65)));
Console.WriteLine("Crusher recipes, capacity and tint checks passed.");

Check(CrusherBatch.NightlyLimit(300) == 3);
Check(CrusherBatch.NightlyLimit(400) == 4);
Check(CrusherBatch.NightlyLimit(600) == 5);
Check(CrusherBatch.Residue(45, 20) == 36);
Check(CrusherBatch.Residue(45, 150) == 0);
Check(CrusherBatch.Residue(45, -10) == 45);
Check(CrusherBatch.Fits(409, 91) && !CrusherBatch.Fits(410, 91));
Check(CrusherBatch.CanFitFeed(422, CrusherFeed.Junk, 100) && !CrusherBatch.CanFitFeed(423, CrusherFeed.Junk, 100));
Check(CrusherBatch.CanFitFeed(405, CrusherFeed.Scrap, 100) && !CrusherBatch.CanFitFeed(406, CrusherFeed.Scrap, 100));
Console.WriteLine("9 overnight crusher checks passed.");

int normalQuality = CrusherBatch.EffectiveQuality(100, preserveResidue: true);
int highQuality = CrusherBatch.EffectiveQuality(100, preserveResidue: false);
Check(CrusherBatch.Residue(45, normalQuality) == 45 && CrusherBatch.Residue(45, highQuality) == 0);
Check(!CrusherBatch.CanFitFeed(422, CrusherFeed.Junk, normalQuality) && CrusherBatch.CanFitFeed(422, CrusherFeed.Junk, highQuality));
Console.WriteLine("2 crusher quality switch checks passed.");

Check(PowderSieveRules.RecoveryRate(0) == 50);
Check(PowderSieveRules.RecoveryRate(20) == 70);
Check(PowderSieveRules.RecoveryRate(70) == 85);
Check(PowderSieveRules.Recovered(45, 30) == 13);
Check(PowderSieveRules.Recovered(45, 100) == 45);
Console.WriteLine("5 sieve recovery checks passed.");

Check(PowderClassification.FromParts(new[] { 0, 0, 0, 0, 0 }).Grade == PowderGrade.Empty);
Check(PowderClassification.FromParts(new[] { 404, 96, 0, 0, 0 }).Grade == PowderGrade.Mixed);
var coarse = PowderClassification.FromParts(new[] { 405, 95, 0, 0, 0 });
Check(coarse.Grade == PowderGrade.Coarse && coarse.Branch == PowderComponent.Metal && coarse.PurityBasisPoints == 8100);
Check(PowderClassification.FromParts(new[] { 0, 494, 0, 6, 0 }).Grade == PowderGrade.Coarse);
var pure = PowderClassification.FromParts(new[] { 0, 495, 0, 5, 0 });
Check(pure.Grade == PowderGrade.Pure && pure.Branch == PowderComponent.Silica && pure.PurityBasisPoints == 9900);
Check(PowderClassification.FromParts(new[] { 0, 0, 0, 0, 500 }).Branch == PowderComponent.Residue);
Check(PowderClassification.FromParts(new[] { 250, 250, 0, 0, 0 }).Branch == null);
Console.WriteLine("7 powder classification checks passed.");

Check(PowderPricing.BoxValue(new[] { 0, 0, 0, 0, 0 }) == 20);
Check(PowderPricing.BoxValue(new[] { 500, 0, 0, 0, 0 }) == 164);
Check(PowderPricing.BoxValue(new[] { 495, 5, 0, 0, 0 }) == 164);
Check(PowderPricing.BoxValue(new[] { 494, 6, 0, 0, 0 }) == 140);
Check(PowderPricing.BoxValue(new[] { 400, 100, 0, 0, 0 }) == 106);
Console.WriteLine("5 powder pricing checks passed.");

Check(SinteringBatch.GetIngotGrade(80, 100) == SinteringIngotGrade.None);
Check(SinteringBatch.GetIngotGrade(81, 100) == SinteringIngotGrade.High);
Check(SinteringBatch.GetIngotGrade(90, 100) == SinteringIngotGrade.High);
Check(SinteringBatch.GetIngotGrade(901, 1000) == SinteringIngotGrade.VeryHigh);
Check(SinteringBatch.GetIngotGrade(989, 1000) == SinteringIngotGrade.VeryHigh);
Check(SinteringBatch.GetIngotGrade(99, 100) == SinteringIngotGrade.Perfect);
var boxes = new[] { new[] { 81, 0, 0, 0, 19 }, new[] { 100, 0, 0, 0, 0 } };
int[] SplitFixture(int[] parts, int amount) => parts[4] == 0
    ? new[] { amount, 0, 0, 0, 0 }
    : amount == 100 ? (int[])parts.Clone() : new[] { 80, 0, 0, 0, 19 };
var requirements = new[] { new SinteringIngredient(PowderComponent.Metal, 120) };
var plan = SinteringBatch.Plan(boxes, requirements, SplitFixture);
Check(plan != null && plan.Remaining[0][0] == 0 && plan.Remaining[1][0] == 61);
Check(plan.Metal == 120 && plan.Total == 139 && SinteringBatch.GetIngotGrade(plan.Metal, plan.Total) == SinteringIngotGrade.High);
Check(boxes[0][4] == 19 && boxes[1][0] == 100);
Check(SinteringBatch.Plan(new[] { boxes[0] }, requirements, SplitFixture) == null);
Check(SinteringBatch.Plan(boxes, new[] { new SinteringIngredient(PowderComponent.Metal, 120, PowderGrade.Pure) }, SplitFixture) == null);
var electronic = SinteringBatch.Plan(new[] { new[] { 0, 0, 0, 20, 0 }, new[] { 0, 40, 0, 0, 0 }, boxes[1] },
    new[] { new SinteringIngredient(PowderComponent.Metal, 20), new SinteringIngredient(PowderComponent.Silica, 30), new SinteringIngredient(PowderComponent.Conductive, 15) },
    (parts, amount) => Array.ConvertAll(parts, point => point == 0 ? 0 : amount));
Check(electronic != null && electronic.Remaining[0][3] == 5 && electronic.Remaining[1][1] == 10 && electronic.Remaining[2][0] == 80);
Console.WriteLine("12 sintering checks passed.");

var jittered = CrusherBatch.Parts(CrusherFeed.Junk, 90, 5, 1, -3);
Check(string.Join(",", jittered.ToArray()) == "25,23,9,21,22");
for (int roll = 0; roll < 100; roll++)
for (int metal = -5; metal <= 5; metal++)
for (int rare = -1; rare <= 1; rare++)
for (int residue = -3; residue <= 3; residue++)
{
    var parts = CrusherBatch.Parts(CrusherFeed.Junk, roll, metal, rare, residue);
    int total = 0;
    foreach (int points in parts) { Check(points > 0); total += points; }
    Check(total == 100);
}

var feed = new[] { 80, 77, 11, 28, 104 };
var sorted = Array.ConvertAll(feed, points => PowderSieveRules.Recovered(points, PowderSieveRules.RecoveryRate(0)));
Check(string.Join(",", sorted) == "40,38,5,14,52");
int tailings = 0;
for (int i = 0; i < feed.Length; i++) tailings += feed[i] - sorted[i];
Check(tailings == 151);

Check(SinteringRecipes.All.Count == 5);
Check(SinteringRecipes.IndexOf("common_electronic") == 0);
Check(SinteringRecipes.All[1].Ingredients[0] == new SinteringIngredient(PowderComponent.Silica, 30));
Check(SinteringRecipes.All[2].Product == "wire");
Check(SinteringRecipes.All[2].Ingredients[0] == new SinteringIngredient(PowderComponent.Conductive, 12));
Check(SinteringRecipes.All[3].Ingredients[0] == new SinteringIngredient(PowderComponent.RareMetal, 4));
var bottle = SinteringRecipes.All[SinteringRecipes.IndexOf("wine_bottle")];
Check(bottle.Id == "wine_bottle" && bottle.Product == "wine_bottle" && !bottle.GradeIngot);
Check(bottle.Ingredients.Length == 1 && bottle.Ingredients[0] == new SinteringIngredient(PowderComponent.Silica, 20));
int[] SplitSilica(int[] parts, int amount)
{
    int total = parts[1] + parts[4];
    int silica = amount * parts[1] / total;
    return new[] { 0, silica, 0, 0, amount - silica };
}
var bottleBatch = SinteringBatch.Plan(new[] { new[] { 0, 81, 0, 0, 19 } }, bottle.Ingredients, SplitSilica);
Check(bottleBatch != null && bottleBatch.Remaining[0][1] == 61);
Check(SinteringBatch.Plan(new[] { new[] { 0, 80, 0, 0, 20 } }, bottle.Ingredients, SplitSilica) == null);
Console.WriteLine("Randomized junk composition and electronics supply checks passed.");

Check(MineralGrowthBatch.Ore.Residue == 60 && MineralGrowthBatch.Ore.Secondary == 15 && MineralGrowthBatch.Ore.Count == 1);
Check(MineralGrowthBatch.LightSalt.Residue == 40 && MineralGrowthBatch.LightSalt.Secondary == 10 && MineralGrowthBatch.LightSalt.Count == 2);
Check(MineralGrowthBatch.SecondaryCost(15, 100) == 12 && MineralGrowthBatch.SecondaryCost(10, 25) == 8);
Check(MineralGrowthBatch.SecondaryCost(15, -10) == 15 && MineralGrowthBatch.WaterCost(25) == 15);
Check(MineralGrowthBatch.WaterCost(0) == 20 && MineralGrowthBatch.WaterCost(100) == 1);
Check(MineralGrowthBatch.Advance(0, 100) == 100 && MineralGrowthBatch.Advance(100, 100) == 200);
Check(MineralGrowthBatch.Advance(150, 100) == 200 && MineralGrowthBatch.Advance(100, 0) == 100);
var mineralFeed = new[] { 19, 0, 0, 0, 81 };
var metalFeed = new[] { 81, 19, 0, 0, 0 };
Check(MineralGrowthBatch.TryPlan(MineralGrowthBatch.Ore, mineralFeed, metalFeed, 0));
Check(string.Join(",", mineralFeed) == "19,0,0,0,21" && string.Join(",", metalFeed) == "66,19,0,0,0");
var unchanged = new[] { 19, 0, 0, 0, 81 };
Check(!MineralGrowthBatch.TryPlan(MineralGrowthBatch.Ore, unchanged, new[] { 80, 20, 0, 0, 0 }, 0));
Check(unchanged[4] == 81);
Check(!MineralGrowthBatch.TryPlan(MineralGrowthBatch.Ore, unchanged, new[] { 14, 0, 0, 0, 0 }, 0));
Check(!MineralGrowthBatch.TryPlan(MineralGrowthBatch.LightSalt, unchanged, new[] { 100, 0, 0, 0, 0 }, 0));
Check(!MineralGrowthBatch.TryPlan(MineralGrowthBatch.LightSalt, unchanged, new[] { 0, 8, 0, 0, 0 }, 25));
Check(MineralGrowthBatch.TryPlan(MineralGrowthBatch.LightSalt, unchanged, new[] { 0, 0, 8, 0, 0 }, 25));
Check(MineralGrowthBatch.Appearance(null, 200) == null);
Check(MineralGrowthBatch.Appearance("unknown", 200) == null);
Check(MineralGrowthBatch.Appearance(MineralGrowthBatch.OreBlueprint, 100) == "_ore-growing");
Check(MineralGrowthBatch.Appearance(MineralGrowthBatch.OreBlueprint, 200) == "_ore-mature");
Check(MineralGrowthBatch.Appearance(MineralGrowthBatch.LightSaltBlueprint, 199) == "_light-salt-core-growing");
Check(MineralGrowthBatch.Appearance(MineralGrowthBatch.LightSaltBlueprint, 200) == "_light-salt-core-mature");
Console.WriteLine("Mineral growth recipe, purity, cost and progress checks passed.");
