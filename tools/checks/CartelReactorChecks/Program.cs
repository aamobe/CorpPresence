using System;
using CorpPresence;

static void Check(bool passed) { if (!passed) throw new Exception("Batch state assertion failed"); }
Check(CartelBatch.Advance(0) == 0);
Check(CartelBatch.Advance(1) == 2);
Check(CartelBatch.Advance(2) == 2);
Check(CartelBatch.AfterOutput(2, false) == 2);
Check(CartelBatch.AfterOutput(2, true) == 0);
Console.WriteLine("5 cartel batch checks passed.");

Check(MineralGrowth.Next(0, false, true) == 0);
Check(MineralGrowth.Next(0, true, false) == 0);
Check(MineralGrowth.Next(0, true, true) == 1);
Check(MineralGrowth.Next(1, false, false) == 1);
Check(MineralGrowth.Next(1, false, true) == 2);
Check(MineralGrowth.Next(2, false, true) == 3);
Check(MineralGrowth.Next(3, true, true) == 3);
Console.WriteLine("7 mineral growth checks passed.");
