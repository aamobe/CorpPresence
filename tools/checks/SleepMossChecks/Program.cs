using System;
using CorpPresence;

static void Equal(int expected, int actual)
{
    if (expected != actual) throw new Exception($"Expected {expected}, got {actual}");
}
Equal(2828, SleepMossRules.Absorb(1000000, 10000, 4));
Equal(1414, SleepMossRules.Absorb(1000000, 5000, 6));
Equal(400, SleepMossRules.Absorb(1000000, 800, 4));
Equal(200, SleepMossRules.Absorb(1000000, 800, 6));
Equal(0, SleepMossRules.Absorb(1000000, 400, 4));
Equal(0, SleepMossRules.Absorb(0, 10000, 6));
foreach (var (progress, stage) in new[] { (0, 0), (5, 0), (6, 1), (11, 1), (12, 2), (18, 2) })
    Equal(stage, SleepMossRules.Stage(progress, 18));
Console.WriteLine("12 sleep moss checks passed.");
