using System;
using System.Collections.Generic;
using CorpPresence;

static void Eq(int want, int got) {
    if (want != got) throw new Exception($"Expected {want}, got {got}");
}
static int Nights(int baseWork, int score, int speed) {
    var seed = new GameItem { BaseWork = baseWork, Score = score };
    var machine = new GameItem();
    int nights = 0, progress = 0;
    do {
        nights++;
        progress += speed;
        CropHealth.UpdateMaturityTarget(seed, machine);
        Eq(seed.Tags["REQUIRED_PROGRESS"], seed.Tags["PROGRESS_ITEM_TARGET_TAG"]);
        Eq(seed.Tags["REQUIRED_PROGRESS"], machine.Tags["MACHINE_PROGRESS_TARGET_TAG"]);
    } while (progress < machine.Tags["MACHINE_PROGRESS_TARGET_TAG"]);
    return nights;
}
Eq(4, Nights(28, 100, 8)); Eq(5, Nights(28, 50, 8));
Eq(6, Nights(42, 100, 8)); Eq(7, Nights(42, 50, 8));
Eq(4, Nights(21, -1, 8)); Eq(2, Nights(14, 110, 8));
foreach (var (baseWork, health, expected) in new[] {
    (28, 100, 28), (28, 90, 31), (28, 50, 33), (28, 0, 33),
    (28, -26, 33), (14, 0, 16), (1, 0, 1), (42, 0, 50)
}) {
    var seed = new GameItem { BaseWork = baseWork, Score = health };
    var machine = new GameItem();
    seed.Tags["REQUIRED_PROGRESS"] = 56;
    CropHealth.UpdateMaturityTarget(seed, machine);
    Eq(expected, seed.Tags["REQUIRED_PROGRESS"]);
    Eq(expected, seed.Tags["PROGRESS_ITEM_TARGET_TAG"]);
    Eq(expected, machine.Tags["MACHINE_PROGRESS_TARGET_TAG"]);
}
foreach (var (score, flowers, leaves) in new[] {(100,4,10),(71,4,10),(70,3,7),(31,3,7),(30,2,5),(0,2,5)}) {
    Eq(flowers, CropHealth.HarvestCount(4, score));
    Eq(leaves, CropHealth.HarvestCount(10, score));
}
Eq(1, CropHealth.HarvestCount(1, 0));
Console.WriteLine("Crop checks passed: maturity timing, target sync, refruit base work, harvest thresholds and minimum yield.");

foreach (string plant in new[] { Tobacco.SeedId, Perfume.SeedId }) {
    var seed = new GameItem { PlantId = plant };
    var machine = new GameItem { Input = seed, Progress = 35, Target = 42 };
    CropHealth.AfterContinueGrowing(seed, machine, true);
    Eq(35, seed.Tags["GROWING_PROGRESS"]);
    machine.Progress = 0;
    CropHealth.AfterContinueGrowing(seed, machine, true);
    Eq(0, seed.Tags["GROWING_PROGRESS"]);
    machine.Progress = 100;
    CropHealth.AfterContinueGrowing(seed, machine, true);
    Eq(42, seed.Tags["GROWING_PROGRESS"]);
    machine.Input = null;
    machine.Progress = 5;
    CropHealth.AfterContinueGrowing(seed, machine, true);
    Eq(42, seed.Tags["GROWING_PROGRESS"]);
    machine.Input = seed;
    CropHealth.AfterContinueGrowing(seed, machine, false);
    Eq(42, seed.Tags["GROWING_PROGRESS"]);
    seed.Destroyed = true;
    CropHealth.AfterContinueGrowing(seed, machine, true);
    Eq(42, seed.Tags["GROWING_PROGRESS"]);
}
var nativeSeed = new GameItem { PlantId = "native" };
CropHealth.AfterContinueGrowing(nativeSeed, new GameItem { Input = nativeSeed }, true);
Eq(0, nativeSeed.Tags.Count);
Console.WriteLine("PASS progress sync, perennial reset, removed/destroyed seeds, skipped original and native crop exclusion");
class GameItem {
    public int BaseWork, Score; public string PlantId; public GameItem Input; public bool Destroyed; public int Progress, Target;
    public Dictionary<string, int> Tags = new();
    public void ModifyTag(string name, Action<TagState> update) => update(new TagState(this, name));
}
class TagState(GameItem item, string name) {
    public TagState Enable() => this;
    public void SetInt(int value) => item.Tags[name] = value;
}
static class HydroponicHelper {
    public static string GetPlantId(GameItem seed) => seed.PlantId;
    public static int GetRequiredProgress(GameItem seed) => seed.BaseWork;
    public static int GetHealthPercent(GameItem seed) => seed.Score;
}

static class StorePlacement { public static bool Destroyed(GameItem item) => item.Destroyed; }
class Slot { public GameItem childItem; }
static class MachineHydroponic { public static Slot GetInputSlot(GameItem machine) => new Slot { childItem = machine.Input }; }
static class MachineProgressHelper { public static (int,int) GetProgress(GameItem machine) => (machine.Progress,machine.Target); }
static class Tobacco { public const string SeedId = "tobacco"; }
static class Perfume { public const string SeedId = "flower"; }
