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
    Check(string.Join(",", CrusherBatch.Parts(false, roll).ToArray()) == expected);
Check(string.Join(",", CrusherBatch.Parts(true, 0).ToArray()) == "75,10,5,5,5");
Check(CrusherBatch.Fits(400) && !CrusherBatch.Fits(401) && !CrusherBatch.Fits(500));
Check(PowderColor.Mix(CrusherBatch.Parts(true, 0)) != PowderColor.Mix(CrusherBatch.Parts(false, 65)));
Console.WriteLine("Crusher recipes, capacity and tint checks passed.");

Check(CrusherBatch.NightlyLimit(300) == 3);
Check(CrusherBatch.NightlyLimit(400) == 4);
Check(CrusherBatch.NightlyLimit(600) == 5);
Check(CrusherBatch.Residue(45, 20) == 36);
Check(CrusherBatch.Residue(45, 150) == 0);
Check(CrusherBatch.Residue(45, -10) == 45);
Check(CrusherBatch.Fits(409, 91) && !CrusherBatch.Fits(410, 91));
Check(CrusherBatch.CanFitFeed(422, false, 100) && !CrusherBatch.CanFitFeed(423, false, 100));
Check(CrusherBatch.CanFitFeed(405, true, 100) && !CrusherBatch.CanFitFeed(406, true, 100));
Console.WriteLine("9 overnight crusher checks passed.");

int normalQuality = CrusherBatch.EffectiveQuality(100, preserveResidue: true);
int highQuality = CrusherBatch.EffectiveQuality(100, preserveResidue: false);
Check(CrusherBatch.Residue(45, normalQuality) == 45 && CrusherBatch.Residue(45, highQuality) == 0);
Check(!CrusherBatch.CanFitFeed(422, false, normalQuality) && CrusherBatch.CanFitFeed(422, false, highQuality));
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

var jittered = CrusherBatch.Parts(false, 90, 5, 1, -3);
Check(string.Join(",", jittered.ToArray()) == "25,23,9,21,22");
for (int roll = 0; roll < 100; roll++)
for (int metal = -5; metal <= 5; metal++)
for (int rare = -1; rare <= 1; rare++)
for (int residue = -3; residue <= 3; residue++)
{
    var parts = CrusherBatch.Parts(false, roll, metal, rare, residue);
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

Check(SinteringRecipes.All.Count == 4);
Check(SinteringRecipes.IndexOf("common_electronic") == 0);
Check(SinteringRecipes.All[1].Ingredients[0] == new SinteringIngredient(PowderComponent.Silica, 30));
Check(SinteringRecipes.All[2].Product == "wire");
Check(SinteringRecipes.All[2].Ingredients[0] == new SinteringIngredient(PowderComponent.Conductive, 12));
Check(SinteringRecipes.All[3].Ingredients[0] == new SinteringIngredient(PowderComponent.RareMetal, 4));
Console.WriteLine("Randomized junk composition and electronics supply checks passed.");
